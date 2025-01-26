namespace EffectSystem
{
    /// <summary>
    /// Effect was applied, but it appears it didn't work, so don't count it.
    /// </summary>
    [Serializable]
    public class EffectNotVerifiedException : Exception
    {
        public EffectNotVerifiedException()
        {
        }

        public EffectNotVerifiedException(string? message) : base(message)
        {
        }

        public EffectNotVerifiedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

}