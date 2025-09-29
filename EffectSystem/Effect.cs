namespace EffectSystem
{
    public interface Effect
    {
        /// <summary>
        /// Unique ID for this effect
        /// </summary>
        string ID { get; }

        /// <summary>
        /// List of mutual-exclusion (mutex) ids.  This effect cannot activate if any other effect with one of these is active, and vice versa.
        /// </summary>
        List<string> Mutex { get; }

        /// <summary>
        /// <see cref="CurrentRequest"/>'s Duration or default Duration or TimeSpan.Zero
        /// </summary>
        TimeSpan Duration { get; }
        /// <summary>
        /// <see cref="Duration"/> is not TimeSpan.Zero
        /// </summary>
        bool HasDuration { get; }

        /// <summary>
        /// Whether the game's mode is such that this effect should even be listed as a possibility
        /// </summary>
        bool IsListableGameMode { get; }

        /// <summary>
        /// Whether the game's state is such that a user can select this effect to start.
        /// </summary>
        bool IsSelectableGameState { get; }

        void Start(EffectDispatchRequest request);

        /// <summary>
        /// Set by <see cref="Start(EffectDispatchRequest)"/>
        /// </summary>
        EffectDispatchRequest? CurrentRequest { get; }

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
        /// How much of the Duration that the effect has been active since it started
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Called to stop a started effect that <see cref="HasDuration"/> that might not have completed yet.
        /// Sets <see cref="CurrentRequest"/> null again.
        /// </summary>
        void Stop();

        /// <summary>
        /// if CurrentRequest has been Stopped (is null).
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Whether this effect's updates perform an animation, so it may need faster updating.
        /// </summary>
        bool IsUpdateAnimation { get; }
    }

}