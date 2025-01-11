namespace EffectSystem
{
    public interface EffectDispatchRequest
    {
        string EffectID { get; }
        TimeSpan RequestedDuration { get; }
        string Parameter { get; }
    }

}