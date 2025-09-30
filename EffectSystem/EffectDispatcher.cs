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
                _client.NotAppliedWait(request,
                    GetRemainingTime(effect));
                return;
            }

            IEnumerable<Effect> mutexEffects = GetBlockingMutexEffects(effect);
            if (mutexEffects.Any())
            {
                Aspen.Log.Info($"{mutexEffects.Count()} similar effect(s) must close out before request [{request.EffectID}].");
                TimeSpan retryTime = mutexEffects.Max(GetRemainingTime);
                _client.NotAppliedWait(request, retryTime);
                return;
            }

            if (!effect.IsSelectableGameState)
            {
                _client.NotAppliedRetry(request);
                return;
            }

            ApplyEffectNow(effect, request);
        }

        private static TimeSpan GetRemainingTime(Effect effect)
        {
            return effect.Duration - effect.Elapsed;
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

        /// <summary>
        /// easy-to-consume status of all the effects managed by this dispatcher.
        /// </summary>
        /// <returns></returns>
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
                        ? GetRemainingTime(openEffect)
                        : TimeSpan.Zero
                    });
            }
            return result;
        }

        protected void UpdateUnclosedDurationEffects()
        {
            foreach (Effect openEffect in Effects.Where(e
                => !e.IsClosed
                && e.HasDuration
                && !e.IsUpdateAnimation))
            {
                UpdateEffect(openEffect);
            }
        }

        private void UpdateEffect(Effect openEffect)
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
            catch (Exception e)
            {
                Aspen.Log.ErrorException(e, $"Update Effect failed [{request?.EffectID}]");
                // this is not handled "well" but rethrow would prevent other effects updating (and statuses getting updated)
            }
        }

        private void OnUpdatePausedEffect(Effect e)
        {
            _client.DurationPaused(e.CurrentRequest, GetRemainingTime(e));
        }

        private void OnUpdateResumedEffect(Effect e)
        {
            _client.DurationResumed(e.CurrentRequest, GetRemainingTime(e));
        }

        private void OnUpdateClosingEffect(Effect e)
        {
            Aspen.Log.Info($"{e.ID} Finishing.");
            _client.DurationFinished(e.CurrentRequest);
        }

        protected void UpdateUnclosedDurationFastAnimationEffects()
        {
            foreach (Effect openEffect in Effects.Where(e
                => !e.IsClosed
                && e.HasDuration
                && e.IsUpdateAnimation))
            {
                UpdateEffect(openEffect);
            }
        }

        public void StopEarly(EffectDispatchRequest req)
        {
            if (IsForAllEffects(req))
            {
                StopAll();
                Responder.DurationFinished(req);
                return;
            }
            if (IsUnregisteredEffect(req))
            {
                Aspen.Log.Error($"Effect {req.EffectID} not found. ");// Available effects: {string.Join(", ", Effects.Keys)}");
                return;
            }

            Effect effect = Effects.First(e => e.ID == req.EffectID);//Effects[request.code];

            Stop(effect);
            Responder.DurationFinished(req);
        }

        private bool IsForAllEffects(EffectDispatchRequest req)
        {
            return req.EffectID == string.Empty;
        }

        public void StopAll()
        {
            foreach (Effect openEffect in Effects.Where(e => !e.IsClosed))
            {
                Stop(openEffect);
            }
        }

        private void Stop(Effect effect)
        {
            try
            {
                effect.Stop();

                Aspen.Log.Info($"Effect {effect.ID} stopped.");
            }
            catch (Exception ex)
            {
                Aspen.Log.ErrorException(ex, $"Effect {effect.ID} Stop failed. ");
            }
        }

        private bool IsUnregisteredEffect(EffectDispatchRequest req)
        {
            return req.EffectID == null
                || !Effects.Any(e => e.ID == req.EffectID);
        }

        // TODO maybe not the best plan, but I need to cache rather than constantly sending the same status.
        private Dictionary<Effect, (bool selectable, bool listable)> EffectListings = new();
        protected void RefreshEffectListings()
        {
            foreach (Effect effect in Effects)
            {
                try
                {
                    RefreshEffectListing(effect);
                }
                catch (Exception ex)
                {
                    Aspen.Log.WarningException(ex, $"Effect status refresh failed for {effect?.ID}");
                    // continue refreshing the others.
                }
            }
        }

        private void RefreshEffectListing(Effect effect)
        {
            if (EffectListings.ContainsKey(effect))
                UpdateEffectListing(effect);
            else
                FirstEffectListing(effect);
        }

        private void UpdateEffectListing(Effect effect)
        {
            (bool wasSelectable, bool wasListable) = EffectListings[effect];

            bool selectable = IsEffectListingSelectable(effect);
            if (wasSelectable != selectable)
                _client.SetSelectable(effect.ID, selectable);

            bool listable = effect.IsListableGameMode;
            if (wasListable != listable)
                _client.SetListed(effect.ID, listable);

            EffectListings[effect] = (selectable, listable);
        }

        private bool IsEffectListingSelectable(Effect effect)
        {
            return !GetBlockingMutexEffects(effect).Any()
                && effect.IsClosed
                && effect.IsSelectableGameState;
        }

        private void FirstEffectListing(Effect effect)
        {
            bool selectable = IsEffectListingSelectable(effect);
            _client.SetSelectable(effect.ID, selectable);

            bool listable = effect.IsListableGameMode;
            _client.SetListed(effect.ID, listable);

            EffectListings[effect] = (selectable, listable);
        }

        public delegate void EffectStatesUpdated(EffectDispatcher cc);
        public event EffectStatesUpdated? OnEffectStatesUpdated;

        protected void NotifyEffectStatesUpdated(EffectDispatcher tF2EffectDispatcher)
        {
            OnEffectStatesUpdated?.Invoke(this);
        }

        /// <summary>
        /// Stops all effects and cleans up any other resources owned by the dispatcher.
        /// </summary>
        virtual public void Dispose()
        {
            StopAll();
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