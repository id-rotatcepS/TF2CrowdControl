namespace Effects
{
    abstract public class InstantEffect : PausableEffect
    {
        //TODO technically this hierarchy is wrong, just convenient.  They should share a common parent.

        public InstantEffect(string id)
            : base(id, TimeSpan.Zero)
        {
        }
    }

}