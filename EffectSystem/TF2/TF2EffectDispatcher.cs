
using ASPEN;

namespace EffectSystem.TF2
{
    /// <summary>
    /// EffectDispatcher that does safe-for-TF2 timing for updates
    /// </summary>
    public class TF2EffectDispatcher : EffectDispatcher
    {
        public TF2EffectDispatcher(EffectResponder responses) : base(responses)
        {
            // start the Update timer.
            _safeTimer = new Timer(TickSafe, null, SafeIntervalInMillis, Timeout.Infinite);
            _fastTimer = new Timer(TickFast, null, TickIntervalInMillis, Timeout.Infinite);
        }

        private Timer? _safeTimer;
        private Timer? _fastTimer;

        private readonly int TickIntervalInMillis = 50;
        private readonly int SafeIntervalInMillis = 250;

        private void TickSafe(object? state)
        {
            Aspen.Log.Trace(DateTime.Now.Ticks + " TickSAFE");
            try
            {
                //TODO merge these into one interface call on Dispatcher?
                UpdateUnclosedDurationEffects();
                RefreshEffectListings();
                //TODO make this more granular - dispatcher should invoke when there's actually a change.
                NotifyEffectStatesUpdated(this);
            }
            finally
            {
                _ = _safeTimer?.Change(SafeIntervalInMillis, Timeout.Infinite);
            }
            Aspen.Log.Trace(DateTime.Now.Ticks + " TickSAFE after");
        }

        private void TickFast(object? state)
        {
            try
            {
                UpdateUnclosedDurationFastAnimationEffects();
            }
            finally
            {
                _ = _fastTimer?.Change(TickIntervalInMillis, Timeout.Infinite);
            }
        }

        public override void StopAll()
        {
            base.StopAll();
            _safeTimer?.Dispose();
            _fastTimer?.Dispose();
        }

    }
}