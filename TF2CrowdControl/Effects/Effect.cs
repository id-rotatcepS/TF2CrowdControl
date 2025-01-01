namespace Effects
{
    public interface Effect
    {
        string ID { get; }
        // non-null
        List<string> Mutex { get; }
        /// <summary>
        /// requested Duration or default Duration or TimeSpan.Zero
        /// </summary>
        TimeSpan Duration { get; }
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Set by <see cref="Start(EffectDispatchRequest)"/>
        /// </summary>
        EffectDispatchRequest? CurrentRequest { get; }

        void Start(EffectDispatchRequest request);

        /// <summary>
        /// Called (while not <see cref="IsClosed"/>) periodically to update <see cref="Elapsed"/>
        /// and continue a started effect that <see cref="HasDuration"/>.
        /// Also performs <see cref="Stop"/> if <see cref="Elapsed"/> is >= <see cref="Duration"/>.
        /// </summary>
        /// <param name="OnPaused">Called when Elapsed won't be incrementing due to circumstances.</param>
        /// <param name="OnResumed">Called when a pause is ended and Elapsed will resume incrementing.</param>
        /// <param name="OnClosing">Called when the update causes the effect to reach its end and as been stopped. CurrentRequest has not been cleared and IsClosed is still false.</param>
        void Update(Action<Effect> OnPaused, Action<Effect> OnResumed, Action<Effect> OnClosing);

        /// <summary>
        /// Called to stop a started effect that might not have completed yet.
        /// </summary>
        void Stop();

        /// <summary>
        /// true if CurrentRequest has been Stopped
        /// </summary>
        bool IsClosed { get; }

        bool IsSelectableGameState { get; }

        bool HasDuration { get; }

        bool IsListableGameMode { get; }
    }

}