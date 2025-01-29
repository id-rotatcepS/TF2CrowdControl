namespace EffectSystem
{
    /// <summary>
    /// Effect could not be applied (in whole or in part).
    /// </summary>
    [Serializable]
    public class EffectNotAppliedException : Exception
    {
        public EffectNotAppliedException()
        {
        }

        public EffectNotAppliedException(string? message) : base(message)
        {
        }

        public EffectNotAppliedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

}