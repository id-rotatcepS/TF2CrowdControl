using ConnectorLib.JSON;

using EffectSystem;

namespace CrowdControl
{
    /// <summary>
    /// Map from EffectDispatchRequest to Crowd Control EffectRequest
    /// https://developer.crowdcontrol.live/sdk/simpletcp/structure.html#requests
    /// </summary>
    public class CCEffectDispatchRequest : EffectDispatchRequest
    {
        public EffectRequest OriginalRequest { get; }

        public CCEffectDispatchRequest(EffectRequest original)
        {
            OriginalRequest = original;
        }

        /// <summary>
        /// The identifier of the requested effect.	Only present on request types 0 (Test), 1(Start) and 2 (Stop).
        /// </summary>
        public string EffectID => OriginalRequest.code ?? string.Empty;
        /// <summary>
        /// Just the first parameter's value as a string.
        /// object?[]?		This field contains any parameters the user has selected.
        /// 
        /// parameters?.First?.ToString():
        /// "class": {
        /// "value": "heavyweapons",
        /// "title": "class",
        /// "type": "options"
        /// }
        /// </summary>
        public virtual string Parameter => OriginalRequest.parameters?.First?.First?["value"]?.ToString() ?? string.Empty;

        /// <summary>
        /// The requested duration of the effect, in milliseconds.	An option to report this value as decimal seconds (double?) will be available in a future release.
        /// </summary>
        public TimeSpan RequestedDuration => TimeSpan.FromMilliseconds(OriginalRequest.duration ?? 0);
        /// <summary>
        /// The displayable name of the viewer who requested the effect.	Returns “the crowd” if multiple viewers are present.
        /// </summary>
        public string Requestor => OriginalRequest.viewer ?? string.Empty;
    }

    public class CCHypeTrainEffectDispatchRequest : CCEffectDispatchRequest
    {
        public CCHypeTrainEffectDispatchRequest(EffectRequest original, string hype, string progress, IEnumerable<string> hypecontribs)
            : base(original)
        {
            Hype = hype;
            Progress = progress;
            HypeContribs = hypecontribs;
        }

        public string Hype { get; private set; }
        public string Progress { get; }
        public IEnumerable<string> HypeContribs { get; private set; }

        public override string Parameter =>
            Hype + " " +
            //Progress + " " + // TODO not sure if this is formatted right - it gets logged.  Also might be too long for party-chat.
            "Special thanks to " + string.Join(", ", HypeContribs);
    }
}