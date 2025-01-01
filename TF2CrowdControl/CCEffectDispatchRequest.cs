using ConnectorLib.JSON;

using Effects;

namespace CrowdControl
{
    /// <summary>
    /// Map from EffectDispatchRequest to Crowd Control EffectRequest
    /// </summary>
    public class CCEffectDispatchRequest : EffectDispatchRequest
    {
        public EffectRequest OriginalRequest { get; }

        public CCEffectDispatchRequest(EffectRequest original)
        {
            OriginalRequest = original;
        }

        public string EffectID => OriginalRequest.code;
        public string Parameter => OriginalRequest.message;//TODO is this right?
        //Note apparently CC request duration is in milliseconds
        public TimeSpan RequestedDuration => TimeSpan.FromMilliseconds(OriginalRequest.duration ?? 0);
    }
}