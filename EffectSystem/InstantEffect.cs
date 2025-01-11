
namespace EffectSystem
{
    abstract public class InstantEffect : EffectBase
    {
        public InstantEffect(string id)
            : base(id)
        {
        }

        public override TimeSpan Duration => TimeSpan.Zero;

        public override bool HasDuration => false;

        override protected bool CanElapse => true;

    }
}