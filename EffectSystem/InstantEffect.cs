
namespace EffectSystem
{
    /// <summary>
    /// Effects that don't use Duration.
    /// Especially important for StartEffect to throw <see cref="EffectNotVerifiedException"/>
    /// when the instant effect likely didn't work.
    /// </summary>
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