
namespace EffectSystem
{
    [Serializable]
    public class EffectFinishedEarlyException : Exception
    {
        public EffectFinishedEarlyException()
        {
        }

        public EffectFinishedEarlyException(string? message) : base(message)
        {
        }

        public EffectFinishedEarlyException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}