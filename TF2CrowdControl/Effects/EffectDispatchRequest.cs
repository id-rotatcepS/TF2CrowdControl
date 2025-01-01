namespace Effects
{
    public interface EffectDispatchRequest
    {
        string EffectID { get; }
        TimeSpan RequestedDuration { get; }
        string Parameter { get; }
    }

}