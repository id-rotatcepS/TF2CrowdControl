
using ASPEN;

namespace EffectSystem.TF2
{
    /// <summary>
    /// EffectDispatcher that does safe-for-TF2 timing for updates.
    /// Triggering UpdateUnclosedDuration(/FastAnimation)Effects and RefreshEffectListings on timers.
    /// </summary>
    public class TF2EffectDispatcher : EffectDispatcher
    {
        public TF2EffectDispatcher(EffectResponder responses)
            : base(responses)
        {
            // start the Update timers.
            _safeTimer = new Timer(TickSafe, null, PollPeriodSafe, Timeout.InfiniteTimeSpan);
            _fastTimer = new Timer(TickFast, null, PollPeriodFast, Timeout.InfiniteTimeSpan);
        }

        private readonly Timer _safeTimer;
        private readonly Timer _fastTimer;

        private static readonly TimeSpan PollPeriodFast = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan PollPeriodSafe = TimeSpan.FromMilliseconds(250);

        private static readonly TimeSpan PollPauseTime = TimeSpan.FromSeconds(10);

        private void TickSafe(object? state)
        {
            Aspen.Log.Trace(DateTime.Now.Ticks + " TickSAFE");
            TickOrPauseOnError(_safeTimer, PollPeriodSafe, () =>
            {
                //TODO merge these into one interface call on Dispatcher?
                UpdateUnclosedDurationEffects();
                RefreshEffectListings();
                //TODO make this more granular - dispatcher should invoke when there's actually a change.
                NotifyEffectStatesUpdated(this);
            });
            Aspen.Log.Trace(DateTime.Now.Ticks + " TickSAFE after");
        }

        private string tickRepeatedExceptionMessage = string.Empty;
        private void TickOrPauseOnError(Timer timer, TimeSpan pollPeriod, Action tickAction)
        {
            try
            {
                tickAction.Invoke();

                _ = timer?.Change(pollPeriod, Timeout.InfiniteTimeSpan);
            }
            catch (Exception pollEx)
            {
                if (tickRepeatedExceptionMessage != pollEx.Message)
                {
                    tickRepeatedExceptionMessage = pollEx.Message;
                    Aspen.Log.WarningException(pollEx, "unable to update tf2 effects - pausing for a bit");
                }
                // give us a long break to finish loading or whatever else is wrong.
                // This is to prevent game crashes we were getting during map loads.
                _ = timer?.Change(PollPauseTime, Timeout.InfiniteTimeSpan);
            }
        }

        private void TickFast(object? state)
        {
            TickOrPauseOnError(_fastTimer, PollPeriodFast, () =>
                UpdateUnclosedDurationFastAnimationEffects()
            );
        }

        public override void Dispose()
        {
            _safeTimer.Dispose();
            _fastTimer.Dispose();
            base.Dispose();
        }
    }
}