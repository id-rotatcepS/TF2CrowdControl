using ASPEN;

using ConnectorLib.JSON;

using EffectSystem;

namespace CrowdControl
{
    /// <summary>
    /// Map from EffectResponder to Crowd Control responses via SimpleTCPClient
    /// </summary>
    public class CCEffectResponder : EffectResponder
    {
        private SimpleTCPClient _client;

        public CCEffectResponder(SimpleTCPClient client)
        {
            _client = client;
        }

        public void AppliedFor(EffectDispatchRequest request, TimeSpan duration)
            => Respond(request, EffectStatus.Success, duration).Forget();

        private async Task<bool> Respond(EffectDispatchRequest request, EffectStatus result, TimeSpan? timeRemaining, string message = "")
            //We can "safely" assume a CC request given we're wrapping a CC client
            => await Respond(((CCEffectDispatchRequest)request).OriginalRequest, result, timeRemaining, message);

        private async Task<bool> Respond(EffectRequest request, EffectStatus result, TimeSpan? timeRemaining, string message = "")
        {
            try
            {

                // Unavailable/Retry/Success/Paused/Resumed/Finished
                if (timeRemaining == null)
                    Aspen.Log.Info($"{result} effect [{request.code}]. {message}");
                else
                    Aspen.Log.Info($"{result} effect [{request.code}] {timeRemaining.Value.TotalSeconds}s. {message}");

                return await _client.Respond(new EffectResponse()
                {
                    id = request.ID,
                    status = result,
                    timeRemaining = ((long?)timeRemaining?.TotalMilliseconds) ?? 0L,
                    message = message,
                    type = ResponseType.EffectRequest,
                    //metadata = GetMetadata()
                });
            }
            catch (Exception e)
            {
                Aspen.Log.ErrorException(e, "Respond to effect request failed");
                return false;
            }
        }

        public void AppliedInstant(EffectDispatchRequest request)
            => Respond(request, EffectStatus.Success, null).Forget();

        public void DurationFinished(EffectDispatchRequest request)
            => Respond(request, EffectStatus.Finished, null).Forget();

        public void DurationPaused(EffectDispatchRequest request, TimeSpan remainingTime)
            => Respond(request, EffectStatus.Paused, remainingTime).Forget();

        public void DurationResumed(EffectDispatchRequest request, TimeSpan remainingTime)
            => Respond(request, EffectStatus.Resumed, remainingTime).Forget();

        public void NotAppliedFailed(EffectDispatchRequest request, string message)
            => Respond(request, EffectStatus.Failure, null, message).Forget();

        public void NotAppliedRetry(EffectDispatchRequest request, TimeSpan waitTime)
            //TODO the theory is sound, but the SDK doesn't appear to use the time value at all.
            => Respond(request, EffectStatus.Retry, waitTime).Forget();

        public void NotAppliedUnavailable(EffectDispatchRequest request)
            => Respond(request, EffectStatus.Unavailable, null).Forget();

        public void SetListed(string effectID, bool listed)
            => Status(effectID, listed ? EffectStatus.Visible : EffectStatus.NotVisible).Forget();

        private async Task<bool> Status(string effectID, EffectStatus result)
        {
            try
            {
                Aspen.Log.Info($"{result} effect [{effectID}].");
                return await _client.Update(new EffectUpdate()
                {
                    id = 0,//TODO uuid.v4()
                    status = result,
                    type = ResponseType.EffectStatus,
                    // list of specific effects, vs. a list of group or category names to update.
                    idType = EffectUpdate.IdentifierType.Effect,
                    ids = [effectID],
                });
            }
            catch (Exception e)
            {
                Aspen.Log.ErrorException(e, $"Status response for effect {effectID} failed");
                return false;
            }
        }

        public void SetSelectable(string effectID, bool selectable)
            => Status(effectID, selectable ? EffectStatus.Selectable : EffectStatus.NotSelectable).Forget();
    }
}