using TF2FrameworkInterface;

namespace EffectSystem.TF2
{
    /// <summary>
    /// Holder for info about the current server connection, including players on that server.
    /// </summary>
    public class TF2Server
    {
        private List<TF2Player> Players { get; set; } = new List<TF2Player>();

        public void RefreshPlayer(TF2Status status)
        {
            lock (Players)
            {
                if (Players.Any(PlayerStatusMatch(status)))
                    Players.First(PlayerStatusMatch(status)).Refresh(status);
                else
                    Players.Add(CreatePlayer(status));
            }

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
            DateTime threeSecondsAgo = DateTime.Now - TimeSpan.FromSeconds(3);
            lock (Players)
                Players.RemoveAll(p => threeSecondsAgo > p.LastUpdated);
        }

        public TF2Player? GetPlayer(string? name)
        {
            if (name == null)
                return null;

            lock (Players)
                return Players.FirstOrDefault(p => p.Name == name);
        }
    }
}