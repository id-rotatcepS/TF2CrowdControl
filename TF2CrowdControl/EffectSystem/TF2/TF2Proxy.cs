namespace EffectSystem.TF2
{
    /// <summary>
    /// Provides the latest known TF2 Status
    /// </summary>
    public interface TF2Proxy
    {
        /// <summary>
        /// TF2 is running
        /// </summary>
        public bool IsOpen { get; }

        /// <summary>
        /// A game map has loaded
        /// </summary>
        public bool IsMapLoaded { get; }
        /// <summary>
        /// name of the last loaded game map
        /// </summary>
        public string Map { get; }

        /// <summary>
        /// Gets the latest known value returned for the named variable (or command)
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        string? GetValue(string variable);

        /// <summary>
        /// Run the command and wait for its result
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        string RunCommand(string command);
        /// <summary>
        /// RunCommand("setinfo {variable} {value}")
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        void SetInfo(string variable, string value);
        /// <summary>
        /// RunCommand("{variable} {value}")
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        void SetValue(string variable, string value);

        /// <summary>
        /// call this to clean up when shutting down (e.g. restoring any settings)
        /// </summary>
        void ShutDown();

        /// <summary>
        /// User has spawned and has not died.
        /// </summary>
        public bool IsUserAlive { get; }
        /// <summary>
        /// The time the user was believed to have spawned last
        /// </summary>
        public DateTime UserSpawnTime { get; }
        /// <summary>
        /// The last class the user is believed to have spawned as
        /// </summary>
        public string ClassSelection { get; }
        /// <summary>
        /// The class the user last selected for their next spawn
        /// </summary>
        public string NextClassSelection { get; }
        bool IsJumping { get; }
        bool IsWalking { get; }
        double VerticalSpeed { get; }
        double HorizontalSpeed { get; }

        /// <summary>
        /// User has killed another player
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="weapon"></param>
        /// <param name="crit"></param>
        public delegate void UserKill(string victim, string weapon, bool crit);
        /// <summary>
        /// Fires when user is credited for a kill of another player
        /// </summary>
        public event UserKill? OnUserKill;

        /// <summary>
        /// User is believed to have spawned
        /// </summary>
        public delegate void UserSpawn();
        /// <summary>
        /// Fires when user is believed to be alive after joining a map or dying.
        /// </summary>
        public event UserSpawn? OnUserSpawned;
        /// <summary>
        /// User has died
        /// </summary>
        public delegate void UserDeath();
        /// <summary>
        /// Fires when user dies in game
        /// </summary>
        public event UserDeath? OnUserDied;
    }

}