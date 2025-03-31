using ASPEN;

namespace EffectSystem
{
    abstract public class EffectBase : Effect
    {
        private bool IsElapsing = false;
        private DateTime LastUpdate;

        public EffectBase(string id)
        {
            ID = id;
        }

        public string ID { get; }

        /// <summary>
        /// How long this effect will be performed - Zero if it is instant. 
        /// (clock time could be longer if the effect is paused during updates)
        /// </summary>
        abstract public TimeSpan Duration { get; }

        /// <summary>
        /// List of mutually exclusive groups this effect belongs to
        /// - only one effect in any group may be active at any moment.
        /// </summary>
        public List<string> Mutex { get; set; } = new List<string>();

        /// <summary>
        /// How much time has been credited to this effect.
        /// </summary>
        public TimeSpan Elapsed { get; private set; }

        /// <summary>
        /// set on Start, cleared after Start or Update ends with IsClosed
        /// </summary>
        public EffectDispatchRequest? CurrentRequest { get; private set; }

        /// <summary>
        /// Whether the effect has been completed (request cleared or (Elapsed >= Duration))
        /// </summary>
        public bool IsClosed => CurrentRequest == null;

        /// <summary>
        /// Whether this Effect is instant or uses a Duration to play out.
        /// </summary>
        abstract public bool HasDuration { get; }

        /// <summary>
        /// Whether the game state is appropriate for this effect to be activated.
        /// </summary>
        abstract public bool IsSelectableGameState { get; }

        /// <summary>
        /// Whether the game mode is appropriate for this effect to be listed as a choice.
        /// Defaults to true.
        /// </summary>
        virtual public bool IsListableGameMode => true;

        public void Start(EffectDispatchRequest request)
        {
            lock (request)
            {
                LastUpdate = DateTime.Now;
                Elapsed = TimeSpan.Zero;
                IsElapsing = true;
                CurrentRequest = request;

                StartEffect(request);
            }
        }

        /// <summary>
        /// Do the actual effect.  If this is performed over a duration,
        /// Update will be called until the duration is completed.
        /// Throwing an Exception indicates the Effect did not successfully start.
        /// </summary>
        /// <param name="request"></param>
        abstract protected void StartEffect(EffectDispatchRequest request);

        public void Stop()
        {
            if (CurrentRequest == null)
                return;

            lock (CurrentRequest)
            {
                TimeSpan span = ElapsedTimeSpanIncrement();

                StopEffect(span);
                CurrentRequest = null;
            }
        }

        private TimeSpan ElapsedTimeSpanIncrement()
        {
            DateTime now = DateTime.Now;
            TimeSpan span = now.Subtract(LastUpdate);
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;
            Elapsed = Elapsed.Add(span);
            LastUpdate = now;
            return span;
        }

        /// <summary>
        /// End the actual effect, if appropriate.
        /// Won't be called if there is no duration.  Always called if there is a duration.
        /// May be called before Duration is over if an early stop is requested.
        /// </summary>
        /// <param name="request"></param>
        abstract protected void StopEffect(TimeSpan timeSinceLastUpdate);

        public void Update(Action<Effect> OnPaused, Action<Effect> OnResumed, Action<Effect> OnClosing)
        {
            if (CurrentRequest == null)
                return;
            lock (CurrentRequest)
            {
                if (!CanElapse)
                {
                    UpdatePaused(OnPaused);
                    return;
                }
                UpdateContinues(OnResumed);

                UpdateOrStop(OnClosing);
            }
        }

        /// <summary>
        /// Whether the effect duration should not be paused.  Defaults to true.
        /// </summary>
        abstract protected bool CanElapse { get; }

        private void UpdatePaused(Action<Effect> OnPaused)
        {
            if (!IsElapsing)
            {
                // After we pause we will no longer give credit for Elapsed time,
                // but we need to provide an update span basis that won't be zero when we resume
                // - so we still record time of the last failed check for update elapse permission.
                LastUpdate = DateTime.Now;
                return;
            }

            IsElapsing = false;

            _ = ElapsedTimeSpanIncrement();
            // don't close effect while paused or it won't get its final Update
            // - arbitrary "time left" guarantee.
            if (Elapsed >= Duration)
                Elapsed = Duration.Subtract(TimeSpan.FromMilliseconds(100));

            OnPaused?.Invoke(this);
        }

        private void UpdateContinues(Action<Effect> OnResumed)
        {
            Aspen.Log.Trace($"Updating effect [{ID}].");
            if (IsElapsing)
                return;

            IsElapsing = true;
            OnResumed?.Invoke(this);
        }

        private void UpdateOrStop(Action<Effect> onClosing)
        {
            TimeSpan span = ElapsedTimeSpanIncrement();
            if (Elapsed < Duration)
            {
                try
                {
                    Update(span);
                    return;
                }
                catch (EffectFinishedEarlyException)
                {
                    // fall through to "stop" code
                }
            }

            StopEffect(span);
            // to keep OnClosing consistent with other events, we don't null the request until after invocation, but that means calling IsClosed is false.  That's why we call it OnClosing, not OnClosed.
            onClosing?.Invoke(this);
            CurrentRequest = null;
        }

        /// <summary>
        /// Perform effect updates since last update (or start).
        /// Throw <see cref="EffectFinishedEarlyException"/> if the Update determines the effect is ending prior to the full Duration.
        /// Assume CanElapse already passed.
        /// If Duration has completed, <see cref="StopEffect(TimeSpan)"/> is called instead as the final update. 
        /// </summary>
        abstract protected void Update(TimeSpan span);
    }
}