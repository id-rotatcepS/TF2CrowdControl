namespace EffectSystem
{
    /// <summary>
    /// an effect with an expected (and a default) Duration
    /// </summary>
    abstract public class PausableEffect : EffectBase
    {
        public static readonly TimeSpan DefaultTimeSpan = new TimeSpan(0, minutes: 1, 0);

        public PausableEffect(string id, TimeSpan defaultDuration)
            : base(id)
        {
            DefaultDuration = defaultDuration;
        }

        public TimeSpan DefaultDuration { get; }

        override public TimeSpan Duration
            => CurrentRequest?.RequestedDuration ?? DefaultDuration;

        override public bool HasDuration
            => Duration > TimeSpan.Zero;
    }
}