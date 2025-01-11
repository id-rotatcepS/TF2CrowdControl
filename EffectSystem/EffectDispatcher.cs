using ASPEN;

namespace EffectSystem
{
    /// <summary>
    /// Simple and generic interface to the effect list, accounting for Mutex groups.  
    /// Applies requested effect, responding on success/retry/failure via EffectResponder interface. 
    /// Updates active effects (pauses/resumes/finishes via EffectResponder interface).  
    /// Refreshes effect in UI via EffectResponder interface.
    /// </summary>
    public class EffectDispatcher
    {
        private readonly EffectResponder _client;

        public readonly List<Effect> Effects = new List<Effect>();

        public EffectDispatcher(EffectResponder responses)
        {
            _client = responses;
        }

        public EffectResponder Responder
            => _client;

        public void Apply(EffectDispatchRequest request)
        {
            if (!Effects.Any(e => e.ID == request.EffectID))
            {
                _client.NotAppliedUnavailable(request);
                return;
            }

            Effect effect = Effects.First(e => e.ID == request.EffectID);

            if (!effect.IsClosed)
            {
                _client.NotAppliedRetry(request,
                    // doesn't seem to listen to this.
                    effect.Elapsed - effect.Duration);
                return;
            }

            IEnumerable<Effect> mutexEffects = GetBlockingMutexEffects(effect);
            if (mutexEffects.Any())
            {
                Aspen.Log.Info($"{mutexEffects.Count()} similar effect(s) must close out before request [{request.EffectID}].");
                TimeSpan retryTime = mutexEffects.Max(e => e.Duration - e.Elapsed);
                _client.NotAppliedRetry(request, retryTime);
                return;
            }

            if (!effect.IsSelectableGameState)
            {
                _client.NotAppliedRetry(request,
                    //TODO arbitrary time, but doesn't seem to listen anyhow.
                    TimeSpan.FromSeconds(10));
                return;
            }

            ApplyEffectNow(effect, request);
        }

        private IEnumerable<Effect> GetBlockingMutexEffects(Effect effect)
        {
            return Effects.Where(
                e => e != effect
                && !e.IsClosed
                && e.Mutex.Any(
                    mutex => effect.Mutex.Contains(mutex)));
        }

        private void ApplyEffectNow(Effect effect, EffectDispatchRequest request)
        {
            try
            {
                effect.Start(request);

                if (effect.HasDuration)
                {
                    Aspen.Log.Info($"{effect.ID} Started: {effect.Duration}");
                    _client.AppliedFor(request, effect.Duration);
                }
                else
                {
                    Aspen.Log.Info($"{effect.ID} Started & Finished.");
                    _client.AppliedInstant(request);
                }
            }
            catch (Exception ex)
            {
                Aspen.Log.ErrorException(ex, $"Effect failed [{request.EffectID}].");
                _client.NotAppliedFailed(request, ex.Message);
            }
        }

        public IEnumerable<EffectState> GetEffectsStatus()
        {
            List<EffectState> result = new();
            foreach (Effect openEffect in Effects)
            {
                result.Add(
                    new EffectState(openEffect.ID)
                    {
                        Listed = openEffect.IsListableGameMode,
                        Selectable = openEffect.IsSelectableGameState,
                        Running = !openEffect.IsClosed,
                        Remaining = !openEffect.IsClosed
                        ? openEffect.Duration - openEffect.Elapsed
                        : TimeSpan.Zero
                    });
            }
            return result;
        }

        public void UpdateUnclosedEffects()
        {
            foreach (Effect openEffect in Effects.Where(e => !e.IsClosed))
            {
                //Aspen.Log.Trace($"Updating effect [{openEffect.ID}].");

                // save reference in case the update clears it.
                EffectDispatchRequest? request = openEffect.CurrentRequest;
                try
                {
                    openEffect.Update(
                        OnUpdatePausedEffect,
                        OnUpdateResumedEffect,
                        OnUpdateClosingEffect
                        );
                }
                catch (Exception e) { Aspen.Log.ErrorException(e, $"Update Effect failed [{request?.EffectID}]"); }
            }
        }

        private void OnUpdatePausedEffect(Effect e)
        {
            _client.DurationPaused(e.CurrentRequest, e.Duration - e.Elapsed);
        }

        private void OnUpdateResumedEffect(Effect e)
        {
            _client.DurationResumed(e.CurrentRequest, e.Duration - e.Elapsed);
        }

        private void OnUpdateClosingEffect(Effect e)
        {
            Aspen.Log.Info($"{e.ID} Finishing.");
            _client.DurationFinished(e.CurrentRequest);
        }

        public void StopEarly(EffectDispatchRequest req)
        {
            if (req.EffectID == null
                || !Effects.Any(e => e.ID == req.EffectID)
                )
            {
                Aspen.Log.Error($"Effect {req.EffectID} not found. ");// Available effects: {string.Join(", ", Effects.Keys)}");
                //could not find the effect
                return;
            }
            Effect effect = Effects.First(e => e.ID == req.EffectID);//Effects[request.code];

            try
            {
                effect.Stop();
                //if (!effect.TryStop())
                //{
                //    Aspen.Log.Trace($"Effect {request.code} failed to stop.");
                //    return;
                //}

                Aspen.Log.Info($"Effect {req.EffectID} stopped.");
                // TODO doesn't seem to register... using "Finished" didn't help, either.
                Responder.AppliedInstant(req);
            }
            catch (Exception ex)
            {
                Aspen.Log.ErrorException(ex, $"Effect {req.EffectID} Stop failed. ");
                return;
            }
        }

        public void StopAll()
        {
            foreach (Effect openEffect in Effects.Where(e => !e.IsClosed))
            {
                try
                {
                    openEffect.Stop();
                    Aspen.Log.Info($"Effect {openEffect.ID} stopped.");
                }
                catch (Exception ex)
                {
                    Aspen.Log.ErrorException(ex, $"Effect {openEffect.ID} Stop failed. ");
                }
            }
        }

        // TODO maybe not the best plan, but I need to cache rather than constantly sending hte same status.
        private Dictionary<Effect, (bool selectable, bool listable)> EffectListings = new();
        public void RefreshEffectListings()
        {
            foreach (Effect effect in Effects)
            {
                bool neversent;
                bool wasSelectable;
                bool wasListable;
                if (EffectListings.ContainsKey(effect))
                {
                    (wasSelectable, wasListable) = EffectListings[effect];
                    neversent = false;
                }
                else
                {
                    wasSelectable = true;
                    wasListable = true;
                    neversent = true;
                }

                bool selectable = !GetBlockingMutexEffects(effect).Any()
                    && effect.IsClosed
                    && effect.IsSelectableGameState;
                if (neversent || wasSelectable != selectable)
                    _client.SetSelectable(effect.ID, selectable);

                bool listable = effect.IsListableGameMode;
                if (neversent || wasListable != listable)
                    _client.SetListed(effect.ID, listable);

                EffectListings[effect] = (selectable, listable);
            }
        }

    }

    public class EffectState
    {
        public string ID { get; }

        public EffectState(string iD)
        {
            ID = iD;
        }

        public bool Listed { get; set; }
        public bool Selectable { get; set; }
        public bool Running { get; set; }
        public TimeSpan Remaining { get; set; }
    }
}