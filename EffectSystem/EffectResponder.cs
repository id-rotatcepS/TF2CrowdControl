namespace EffectSystem
{
    /// <summary>
    /// Interface for communicating to an Effect UI the status of the effect and it's selectability.
    /// Designed with Crowd Control and Twitch APIs in mind.
    /// </summary>
    public interface EffectResponder
    {
        void AppliedInstant(EffectDispatchRequest request);
        void AppliedFor(EffectDispatchRequest request, TimeSpan duration);

        void NotAppliedUnavailable(EffectDispatchRequest request);
        void NotAppliedFailed(EffectDispatchRequest request, string message);
        void NotAppliedRetry(EffectDispatchRequest request);
        void NotAppliedWait(EffectDispatchRequest request, TimeSpan waitTime);

        void DurationPaused(EffectDispatchRequest request, TimeSpan remainingTime);
        void DurationResumed(EffectDispatchRequest request, TimeSpan remainingTime);
        void DurationFinished(EffectDispatchRequest request);

        void SetListed(string effectID, bool listed);
        void SetSelectable(string effectID, bool selectable);
    }

}