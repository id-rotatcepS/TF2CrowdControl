using TF2FrameworkInterface;

namespace EffectSystem.TF2
{
    /// <summary>
    /// Holder for all info tracked for players in a server - multiple info sources, some may be blank if the command hasn't run yet.
    /// </summary>
    public class TF2Player
    {
        //.Players[n].Team.UUID(.Name) (.connectedtime) (.kickuserid)

        private TF2Status status;

        public TF2Player()
        {
        }

        public string? KickUserID => status?.GameUserID;

        internal void Refresh(TF2Status status)
        {
            this.status = status;
            this.lastUpdated = DateTime.Now;
        }

        private DateTime lastUpdated;
        internal DateTime LastUpdated => lastUpdated;

        public string? Name => status?.UserName;
    }
}