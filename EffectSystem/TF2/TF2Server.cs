using TF2FrameworkInterface;

namespace EffectSystem.TF2
{
    /// <summary>
    /// Holder for info about the current server connection, including players on that server.
    /// </summary>
    public class TF2Server
    {
        public List<TF2Player> Players { get; private set; } = new List<TF2Player>();

        public void RefreshPlayer(TF2Status status)
        {
            if (Players.Any(PlayerStatusMatch(status)))
                Players.First(PlayerStatusMatch(status)).Refresh(status);
            else
                Players.Add(CreatePlayer(status));

            PurgeStalePlayers();
        }

        private static Func<TF2Player, bool> PlayerStatusMatch(TF2Status status)
        {
            return p => p.KickUserID == status.GameUserID;
        }

        private static TF2Player CreatePlayer(TF2Status status)
        {
            TF2Player player = new TF2Player();
            player.Refresh(status);
            return player;
        }

        private void PurgeStalePlayers()
        {
            DateTime now = DateTime.Now;
            Players.RemoveAll(p => now > p.LastUpdated + TimeSpan.FromSeconds(3));
        }
    }
}