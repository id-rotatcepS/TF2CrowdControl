using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EffectSystem.TF2
{

    // change class causes class.cfg to execute.  Doesn't execute until spawn.
    // death can be detected by log, but respawn can't.
    //
    // on death: change class and set an alias to change back on spawn
    //   alias respawnclass "alias respawnclass ; joinclass {currentclass}"; joinclass {diffclass};
    // all class config files - fire alias to set proper class and unset alias.
    //   setinfo currentclass "diffclass";respawnclass;
    // HOWEVER Engineer is not an option for that hack - it would destroy his buildings.
    // THEREFORE we have to rely on backup timer of perhaps 30 seconds.


    //TODO we also have "*You will spawn as Demoman"
    //TODO "Unhandled GameEvent in ClientModeShared::FireGameEvent - player_death"
    //TODO "Unhandled GameEvent in ClientModeShared::FireGameEvent - localplayer_changeclass"
    /*
Sending request to abandon current match
Sending request to exit matchmaking, marking assigned match as ended
Disconnecting from abandoned match server
Unhandled GameEvent in ClientModeShared::FireGameEvent - client_disconnect

Connecting to 169.254.208.58:37176...
Unhandled GameEvent in ClientModeShared::FireGameEvent - client_beginconnect
Unhandled GameEvent in ClientModeShared::FireGameEvent - server_spawn

Unhandled GameEvent in ClientModeShared::FireGameEvent - player_teleported

Unhandled GameEvent in ClientModeShared::FireGameEvent - teamplay_win_panel

Unhandled GameEvent in ClientModeShared::FireGameEvent - scorestats_accumulated_update
     */


    public abstract class PlayerKiller
    {
        public PlayerKiller(string victimName)
        {
            VictimName = victimName;
        }
        public bool IsSuicide { get; protected set; } = false;
        public bool IsAccident { get; protected set; } = false;
        public bool IsPlayer { get; protected set; } = true;
        public string VictimName { get; private set; }
        public string KillerName { get; protected set; } = string.Empty;
        public string KillerWeapon { get; protected set; } = string.Empty;
        public bool IsCrit { get; protected set; } = false;
    }
    public class Suicided : PlayerKiller
    {
        public Suicided(string victimName)
            : base(victimName)
        {
            IsSuicide = true;
            IsPlayer = false;
        }
    }
    public class Died : PlayerKiller
    {
        public Died(string victimName)
            : base(victimName)
        {
            IsAccident = true;
            IsPlayer = false;
        }
    }
    public class Killed : PlayerKiller
    {
        public Killed(string victimName, string killer, string weapon, bool crit)
            : base(victimName)
        {
            KillerName = killer;
            KillerWeapon = weapon;
            IsCrit = crit;
        }
    }

    public class TF2LogOutput
    {
        public TF2LogOutput(string tf2Path)
        {
            TF2Path = tf2Path;

            LineMatchers = new()
            {
                // needs tf2bd compatibility or prevention, and has no value right now
                // new ChatMatcher(this),

                new KilledMatcher(this),
                new SuicidedMatcher(this),
                new DiedMatcher(this),

                new CustomClassChangeMatcher(this),
                new NextClassChangeMatcher(this),

                new DefendedMatcher(this),
                new CapturedMatcher(this),

                new AutobalancedMatcher(this),
                new IdleMatcher(this),
                new ConnectedMatcher(this),

                new LoadMapMatcher(this),

                // currently disabled/not needed:
                //// output of the "status" command
                //new StatusHostnameMatcher(this),
                //new StatusUDPMatcher(this),
                //new StatusVersionMatcher(this),
                //new StatusSteamidMatcher(this),
                //new StatusAccountMatcher(this),
                // LoadMapMatcher is a one-shot deal that might fail or we might already be connected, so also do it this way.
                new StatusMapMatcher(this),
                //new StatusTagsMatcher(this),
                //new StatusPlayersMatcher(this),
                //new StatusEdictsMatcher(this),
                //new StatusHeaderMatcher(this),
                //new StatusPlayerMatcher(this),
            };
        }

        public string LogFileName { get; } = "TF2SpectatorCommandLogOutput.txt".ToLower(); // apparently it ignores case when you set log file name?

        protected string LogFilePath
            => Path.Combine(TF2Path, "tf", LogFileName);

        public string SetupCommand => $"con_logfile \"{LogFileName}\"; echo TF2 Spectator log started";
        // con_timestamp 1 ; Prefix console.log entries with timestamps

        // currently disabled/not needed:
        public string ActiveLoggingCommand => $"status;";

        /// <summary>
        /// Called OnPlayerDied
        /// </summary>
        /// <param name="circumstances">Details of the victim and cause of death</param>
        public delegate void PlayerDied(PlayerKiller circumstances);
        /// <summary>
        /// Fires when any player is recorded as having been killed by another player with a weapon (or the world), suicided, or otherwise died.
        /// </summary>
        public event PlayerDied OnPlayerDied;
        internal void NotifyPlayerDied(PlayerKiller killer)
        {
            OnPlayerDied.Invoke(killer);
        }

        /// <summary>
        /// Called OnUserChangedClass
        /// </summary>
        /// <param name="playerClass">scout, soldier, pyro, demoman, heavyweapons, engineer, medic, sniper, or spy</param>
        public delegate void UserChangedClass(string playerClass);
        /// <summary>
        /// Fires when the main player spawns as a different class than before (or their first spawn in the new game).
        /// This means the player has respawned with a class change. Respawns without a class change do not fire this.
        /// </summary>
        public event UserChangedClass OnUserChangedClass;
        internal void NotifyUserClassChanged(string playerClass)
        {
            OnUserChangedClass.Invoke(playerClass);
        }
        /// <summary>
        /// Fires when the main player selects a different class than before.
        /// The user likely has not spawned yet as this class.
        /// </summary>
        public event UserChangedClass OnUserSelectedClass;
        internal void NotifyUserClassSelected(string playerClass)
        {
            OnUserSelectedClass.Invoke(playerClass);
        }

        /// <summary>
        /// Called OnMapNameChanged
        /// </summary>
        /// <param name="mapName">pl_upward, etc.</param>
        public delegate void MapNameChanged(string mapName);
        /// <summary>
        /// Fires when the map name changes.
        /// </summary>
        public event MapNameChanged OnMapNameChanged;
        internal void NotifyMapNameChanged(string mapName)
        {
            OnMapNameChanged.Invoke(mapName);
        }

        //public delegate void StatusEvent(StatusCommandLogOutput source);
        //public event StatusEvent StatusUpdated;

        private string TF2Path { get; }

        private Task? monitor;
        public void StartMonitor()
        {
            monitor = Task.Run(
                () => ReadLiveLogFileWithRetries()
                );
        }

        private void ReadLiveLogFileWithRetries()
        {
            int tries = 0;
            while (tries < 60 * 5) // 5 minutes worth of retries.
            {
                try
                {
                    ReadLiveLogFile(LogFilePath);
                    return;
                }
                catch (Exception ex)
                {
                    ++tries;
                    Thread.Sleep(1000);
                    ASPEN.Aspen.Log.WarningException(ex, "TF2's Log File");
                }
            }
            ASPEN.Aspen.Log.Error("TF2's Log File - giving up. MOST STATUS CHECKS WILL NOT WORK. Recommend restarting this app and TF2.");
        }

        //public void StopMonitor() { 
        //}

        private bool notQuitting = true;
        private void ReadLiveLogFile(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs, Encoding.Default))
            {
                ASPEN.Aspen.Log.Info("TF2's Log File - starting.");
                try
                {
                    FlushOldLogLines(sr);

                    while (notQuitting)
                    {
                        for (string? line = sr.ReadLine(); line != null; line = sr.ReadLine())
                            SelectHandler(line)
                                .Handle();

                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    ASPEN.Aspen.Log.ErrorException(ex, "TF2's Log File - error.");
                }
                finally
                {
                    ASPEN.Aspen.Log.Info("TF2's Log File - ending.");
                }
            }
        }

        private static void FlushOldLogLines(StreamReader sr)
        {
            while (sr.ReadLine() != null)
                ;
        }

        public void Dispose()
        {
            notQuitting = false;
            //throws exception if not rantocompetion/faulted/cancelled... also shouldn't matter since above line should end it.
            //monitor?.Dispose();
        }

        public List<LineMatcher> LineMatchers { get; }

        private LineHandler SelectHandler(string line)
        {
            foreach (LineMatcher h in LineMatchers)
            {
                Match match = h.Match(line);
                if (match.Success)
                    return h.GetHandler(match, line);
            }
            return new NoMatchHandler(line);
        }
    }

    public class ChatMatcher : LineMatcher
    {
        // tf2bd compatibility: read "tf\custom\aaaaaaaaaa_loadfirst_tf2_bot_detector\__tf2bd_chat_msg_wrappers.json"
        // has wrappers array (full/message/name) for types all/all_dead/team/team_dead/spec/spec_team/coach
        // those match up with \resources\closecaption_english.txt (and other languages)


        // this chat is using the last-used chat wrapper of the bot detector
        /*
‏​‎‏‌‌​⁠‬﻿*DEAD* ‌﻿‌⁠‏‍‌‏‬​​Random Guy‎‍‌‬‏‌‬‬﻿‍​ :  ⁠﻿⁠﻿‏​‏​‎​​well shit⁠‬‍‏⁠﻿‏‌‎⁠‌⁠​‬​​​‌‎‏‎‬
‏​‎‏‌‌​⁠‬﻿*DEAD* ‌﻿‌⁠‏‍‌‏‬​​Random Guy‎‍‌‬‏‌‬‬﻿‍​ :  ⁠﻿⁠﻿‏​‏​‎​​you got me⁠‬‍‏⁠﻿‏‌‎⁠‌⁠​‬​​​‌‎‏‎‬
‏​‎‏‌‌​⁠‬﻿*DEAD* ‌﻿‌⁠‏‍‌‏‬​​Vixian‎‍‌‬‏‌‬‬﻿‍​ :  ⁠﻿⁠﻿‏​‏​‎​​Rare High Moments⁠‬‍‏⁠﻿‏‌‎⁠‌⁠​‬​​​‌‎‏‎‬

‬﻿⁠﻿‌​‏‌⁠﻿﻿(TEAM) ‎‎‎⁠‌‏⁠‍⁠‏⁠El Muchacho‬‏‬‬‌‏⁠‎‍‏⁠ :  ‬‏‌‬‏‬⁠‍‍‌‬good job​‎‬​‬‎‎‬‬‍‏​‍‌‎⁠‏‌‎​‍‬
‬﻿⁠﻿‌​‏‌⁠﻿﻿(TEAM) ‎‎‎⁠‌‏⁠‍⁠‏⁠Maliniday (melon day?)‬‏‬‬‌‏⁠‎‍‏⁠ :  ‬‏‌‬‏‬⁠‍‍‌‬like, target the sentry gun​‎‬​‬‎‎‬‬‍‏​‍‌‎⁠‏‌‎​‍‬
"‬﻿⁠﻿‌​‏‌⁠﻿﻿(TEAM) ‎‎‎⁠‌‏⁠‍⁠‏⁠%s1‬‏‬‬‌‏⁠‎‍‏⁠ :  ‬‏‌‬‏‬⁠‍‍‌‬%s2​‎‬​‬‎‎‬‬‍‏​‍‌‎⁠‏‌‎​‍‬"

         */

        private static readonly Regex chatRegex = new Regex(@"^.*(\*(?<state>[A-Z]+)\* )?.*(?<name>" + PlayerNamePattern + @").*﻿‍​ :  .*(?<text>.+).*$");

        public ChatMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, chatRegex)
        {

        }

        //public override void Handle(Match match, string line)
        //{
        //    //TODO
        //}
    }

    public class KilledMatcher : LineMatcher
    {
        //TODO risky - could match chat.
        private static readonly Regex killedRegex = new Regex(@"^(?<killername>" + PlayerNamePattern + @") killed (?<victimname>" + PlayerNamePattern + @") with (?<weaponname>[a-z_\d]+)\.(?<crit> \(crit\))?$");

        public KilledMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, killedRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            base.Handle(match, line);

            string victimName = match.Groups["victimname"].Value;
            string killer = match.Groups["killername"].Value;
            string weapon = match.Groups["weaponname"].Value;
            bool crit = !string.IsNullOrWhiteSpace(match.Groups["crit"].Value);
            TF2LogOutput.NotifyPlayerDied(new Killed(victimName,
                killer, weapon, crit));
        }
    }
    public class SuicidedMatcher : LineMatcher
    {
        //TODO risky - could match chat.
        private static readonly Regex suicidedRegex = new Regex(@"^(?<victimname>" + PlayerNamePattern + @") suicided.$");

        public SuicidedMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, suicidedRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            base.Handle(match, line);

            string victimName = match.Groups["victimname"].Value;
            TF2LogOutput.NotifyPlayerDied(new Suicided(victimName));
        }
    }
    public class DiedMatcher : LineMatcher
    {
        // El Muchacho died.

        //TODO risky - could match chat.
        private static readonly Regex diedRegex = new Regex(@"^(?<victimname>" + PlayerNamePattern + @") died.$");

        public DiedMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, diedRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            base.Handle(match, line);

            string victimName = match.Groups["victimname"].Value;
            TF2LogOutput.NotifyPlayerDied(new Died(victimName));
        }
    }

    /// <summary>
    /// Fires when the player spawns as the different class.
    /// Depends on custom installations in the differnet class cfg files that output this info.
    /// </summary>
    public class CustomClassChangeMatcher : LineMatcher
    {
        //TODO tie the configure and regex strings together explicitly
        // e.g. __class-engineer__ 
        private static readonly Regex defendedRegex = new Regex(@"^\s*__class-(?<class>scout|soldier|pyro|demoman|engineer|heavyweapons|medic|sniper|spy)__\s*$");

        public CustomClassChangeMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, defendedRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            base.Handle(match, line);

            string playerClass = match.Groups["class"].Value;
            TF2LogOutput.NotifyUserClassChanged(playerClass);
        }
    }
    /// <summary>
    /// Fires when the player has selected a different class (including while dead).
    /// </summary>
    public class NextClassChangeMatcher : LineMatcher
    {
        private static readonly Regex defendedRegex = new Regex(@"^\s*\*You will respawn as (?<class>Scout|Soldier|Pyro|Demoman|Engineer|Heavy|Medic|Sniper|Spy)\s*$");

        public NextClassChangeMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, defendedRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            base.Handle(match, line);

            string playerClass = match.Groups["class"].Value;
            // translate
            playerClass = playerClass.ToLower();
            if (playerClass == "heavy")
                playerClass = "heavyweapons";
            // this is not the actual class change (spawn)
            TF2LogOutput.NotifyUserClassSelected(playerClass);
        }
    }

    public class DefendedMatcher : LineMatcher
    {
        //name defended place for team #n

        //TODO risky - could match chat.
        private static readonly Regex defendedRegex = new Regex(@"^(?<name>" + PlayerNamePattern + @") defended (?<place>.+) for (?<team>team #\d)$");

        public DefendedMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, defendedRegex)
        {
        }
    }
    public class CapturedMatcher : LineMatcher
    {
        //namelist captured place for team #n

        //TODO risky - could match chat.
        private static readonly Regex capturedRegex = new Regex(@"^(?<namelist>.+) captured (?<place>.+) for (?<team>team #\d)$");

        public CapturedMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, capturedRegex)
        {
        }
    }
    public class AutobalancedMatcher : LineMatcher
    {
        // LuKaZ was moved to the other team for game balance

        //TODO risky - could match chat.
        private static readonly Regex autobalancedRegex = new Regex(@"^(?<name>" + PlayerNamePattern + @") was moved to the other team for game balance$");

        public AutobalancedMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, autobalancedRegex)
        {
        }
    }
    public class IdleMatcher : LineMatcher
    {
        // Avodroc has been idle for too long and has been kicked

        //TODO risky - could match chat.
        private static readonly Regex idleRegex = new Regex(@"^(?<name>" + PlayerNamePattern + @") has been idle for too long and has been kicked$");

        public IdleMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, idleRegex)
        {
        }
    }
    public class ConnectedMatcher : LineMatcher
    {
        // groovy connected
        //TODO risky - could match chat.
        private static readonly Regex connectedRegex = new Regex(@"^(?<name>" + PlayerNamePattern + @") connected$");

        public ConnectedMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, connectedRegex)
        {
        }
    }

    public class LoadMapMatcher : LineMatcher
    {
        // Team Fortress
        // Map: pl_pier
        // Players: 19 / 32
        // Build: 9433646
        // Server Number: 18
        private static readonly Regex mapConnectRegex = new Regex(@"^Map: (?<value>\S+)$");

        public LoadMapMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, mapConnectRegex)
        {
        }
        private string lastValue = string.Empty;
        public override void Handle(Match match, string line)
        {
            string value = match.Groups["value"].Value;
            if (value == lastValue)
                return;
            lastValue = value;
            //base.Handle(match, line);
            TF2LogOutput.NotifyMapNameChanged(value);
        }
    }

    public class StatusHostnameMatcher : LineMatcher
    {
        //hostname: Valve Matchmaking Server (Washington srcds1003-eat1 #41)

        private static readonly Regex hostnameRegex = new Regex(@"^hostname: (?<value>.+)$");

        public StatusHostnameMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, hostnameRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it for now.
        }
    }
    public class StatusUDPMatcher : LineMatcher
    {
        //udp/ip  : 169.254.98.15:55784
        private static readonly Regex udpRegex = new Regex(@"^udp/ip  : (?<value>.+)$");

        public StatusUDPMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, udpRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it for now.
        }
    }
    public class StatusVersionMatcher : LineMatcher
    {
        //version : 8835751/24 8835751 secure
        private static readonly Regex versionRegex = new Regex(@"^version : (?<value>.+)$");

        public StatusVersionMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, versionRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it for now.
        }
    }
    public class StatusSteamidMatcher : LineMatcher
    {
        //steamid : [A:1:1926463490:29216] (90197476238393346)
        private static readonly Regex steamidRegex = new Regex(@"^steamid : (?<value>.+)$");

        public StatusSteamidMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, steamidRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it for now.
        }
    }
    public class StatusAccountMatcher : LineMatcher
    {
        //account : not logged in  (No account specified)
        private static readonly Regex accountRegex = new Regex(@"^account : (?<value>.+)$");

        public StatusAccountMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, accountRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it for now.
        }
    }
    public class StatusMapMatcher : LineMatcher
    {
        //map     : pl_upward at: 0 x, 0 y, 0 z
        private static readonly Regex mapRegex = new Regex(@"^map     : (?<value>\S+).*$");

        public StatusMapMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, mapRegex)
        {
        }
        private string lastValue = string.Empty;
        public override void Handle(Match match, string line)
        {
            string value = match.Groups["value"].Value;
            if (value == lastValue)
                return;
            lastValue = value;
            //base.Handle(match, line);
            TF2LogOutput.NotifyMapNameChanged(value);
        }
    }

    public class StatusTagsMatcher : LineMatcher
    {
        //tags    : hidden,increased_maxplayers,payload,valve
        private static readonly Regex tagsRegex = new Regex(@"^tags    : (?<value>.+)$");

        public StatusTagsMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, tagsRegex)
        {
        }
        private string lastValue = string.Empty;
        public override void Handle(Match match, string line)
        {
            string value = match.Groups["value"].Value;
            if (value == lastValue)
                return;
            lastValue = value;
            base.Handle(match, line);
        }
    }
    public class StatusPlayersMatcher : LineMatcher
    {
        //players : 24 humans, 0 bots (32 max)
        private static readonly Regex playersRegex = new Regex(@"^players : (?<value>.+)$");

        public StatusPlayersMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, playersRegex)
        {
        }
        private string lastValue = string.Empty;
        public override void Handle(Match match, string line)
        {
            string value = match.Groups["value"].Value;
            if (value == lastValue)
                return;
            lastValue = value;
            base.Handle(match, line);
        }
    }

    public class StatusEdictsMatcher : LineMatcher
    {
        //edicts  : 927 used of 2048 max
        private static readonly Regex edictsRegex = new Regex(@"^edicts  : (?<value>.+)$");

        public StatusEdictsMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, edictsRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it for now.
        }
    }

    public class StatusHeaderMatcher : LineMatcher
    {
        //# userid name                uniqueid            connected ping loss state
        private static readonly Regex headerRegex = new Regex(@"^# userid name                uniqueid            connected ping loss state$");

        public StatusHeaderMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, headerRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it
        }
    }

    public class StatusPlayerMatcher : LineMatcher
    {
        //#    777 "NameName"          [U:1:1111111111]    23:09       89    0 active
        private static readonly Regex playerRegex = new Regex(
            @"^# +(?<userid>\d+) +""(?<name>" + PlayerNamePattern + @")"" +(?<uniqueid>\[.*\]) +(?<connected>[\d:]+) +(?<ping>\d+) +(?<loss>\d+) +(?<state>\w+)$"
        // more detailed version:
        // turns out connected could have an hour part... just in case allowing for empty minute part
        //@"^#\s+(?<userid>\d+)\s+\""(?<name>.*)\""\s+(?<uniqueid>\[U:\d+:\d+\])\s+(?:(?:(?<connected_hr>\d+):)?(?<connected_min>\d+):)?(?<connected_sec>\d?\d)\s+(?<ping>\d+)\s+(?<loss>\d+)\s+(?<state>.*)"
        );

        public StatusPlayerMatcher(TF2LogOutput tF2LogOutput)
            : base(tF2LogOutput, playerRegex)
        {
        }
        public override void Handle(Match match, string line)
        {
            // just eat it for now.
        }
    }

    public class LineMatcher
    {
        /// <summary>
        /// Technically limit is 31 characters, but that's a known bug that could get fixed someday.
        /// </summary>
        protected static readonly string PlayerNamePattern = ".{1,32}";

        private readonly Regex regex;

        public LineMatcher(TF2LogOutput tF2LogOutput, Regex regex)
        {
            TF2LogOutput = tF2LogOutput;
            this.regex = regex;
        }

        public TF2LogOutput TF2LogOutput { get; }

        public virtual void Handle(Match match, string line)
        {
            if (!match.Success)
                ASPEN.Aspen.Log.Warning("Match failed? " + line);
            else
                ASPEN.Aspen.Log.Trace(
                    GetType().Name + ":" +
                    string.Join(
                        " ; ",
                        match.Groups.Values
                        .Skip(1) // not "group 0"'s full string.
                        .Select(g => g.Name + ":" + g.Value)
                        ));
        }

        internal LineHandler GetHandler(Match match, string line)
        {
            return new LineHandler(match, line, Handle);
        }

        internal Match Match(string line)
        {
            return regex.Match(line);
        }
    }

    public class LineHandler
    {
        private readonly Match match;
        private readonly string line;

        public LineHandler(Match match, string line, Action<Match, string> handler)
        {
            this.match = match;
            this.line = line;
            Handler = handler;
        }

        private Action<Match, string> Handler { get; }

        internal void Handle()
        {
            Handler.Invoke(match, line);
        }
    }

    internal class NoMatchHandler : LineHandler
    {
        private static readonly Match DummyRegex = new Regex(string.Empty).Match("nomatch");
        public NoMatchHandler(string line)
            : base(DummyRegex, line, (m, s) =>
            {
                ASPEN.Aspen.Log.Trace("no match:" + s);
            })
        {
        }
    }

}

//TODO votekick message, quit, matched away, etc.

//   Member[12] [U:1:174978987]  team = TF_GC_TEAM_DEFENDERS  type = MATCH_PLAYER
//   Member[13] [U:1:467590794]  team = TF_GC_TEAM_DEFENDERS  type = MATCH_PLAYER
//   Member[14] [U:1:1019779858]  team = TF_GC_TEAM_INVADERS  type = MATCH_PLAYER
//   Member[15] [U:1:193999356]  team = TF_GC_TEAM_DEFENDERS  type = MATCH_PLAYER
//   Member[16] [U:1:103230612]  team = TF_GC_TEAM_DEFENDERS  type = MATCH_PLAYER
//   Member[17] [U:1:1842477910]  team = TF_GC_TEAM_DEFENDERS  type = MATCH_PLAYER
//   Member[18] [U:1:373567079]  team = TF_GC_TEAM_INVADERS  type = MATCH_PLAYER
//   Member[19] [U:1:123650837]  team = TF_GC_TEAM_INVADERS  type = MATCH_PLAYER
//   Member[20] [U:1:1126146545]  team = TF_GC_TEAM_INVADERS  type = MATCH_PLAYER
//   Member[21] [U:1:1128111537]  team = TF_GC_TEAM_DEFENDERS  type = MATCH_PLAYER
//   Member[22] [U:1:301790960]  team = TF_GC_TEAM_INVADERS  type = MATCH_PLAYER
//   Pending[0] [U:1:1127836230]  team = TF_GC_TEAM_INVADERS  type = MATCH_PLAYER

//connecting:
// [PartyClient] Requesting queue for 12v12 Casual Match
// [ReliableMsg] PartyQueueForMatch queued for Party ID 0, Embedded Options 1
// [PartyClient] Entering queue for match group 12v12 Casual Match
// [ReliableMsg] PartyQueueForMatch started for Party ID 0, Embedded Options 1
// [PartyClient] Joining party 661129887019662
// [PartyClient] Re-joining party 661129887019662 we previously had queued messages for.  Triggering full resend of options.
// [ReliableMsg] PartySetOptions queued for Party ID 661129887019662, Overwrite 1
// [ReliableMsg] PartyQueueForMatch successfully sent for Party ID 0, Embedded Options 1
// [ReliableMsg] PartySetOptions started for Party ID 661129887019662, Overwrite 1
// [ReliableMsg] PartySetOptions successfully sent for Party ID 661129887019662, Overwrite 1
// [PartyClient] Leaving queue for match group 12v12 Casual Match
// [ReliableMsg] AcceptLobbyInvite queued for Lobby ID 661129763878603 / Abandoning Match 0000000000000000
// [ReliableMsg] AcceptLobbyInvite started for Lobby ID 661129763878603 / Abandoning Match 0000000000000000
// Lobby created
// Differing lobby received. Lobby: [A:1:4057276428:35341]/Match100094950/Lobby661129763878603 CurrentlyAssigned: [I:0:0]/Match0/Lobby0 ConnectedToMatchServer: 0 HasLobby: 1 AssignedMatchEnded: 0
// [ReliableMsg] AcceptLobbyInvite successfully sent for Lobby ID 661129763878603 / Abandoning Match 0000000000000000
// Connecting to matchmaking server 169.254.82.115:14824
// Connecting to 169.254.82.115:14824
// Connecting to matchmaking server 169.254.82.115:14824
// Connecting to 169.254.82.115:14824
// Connecting to 169.254.82.115:14824...
// Connected to 169.254.82.115:14824
// 
// Team Fortress
// Map: pl_pier
// Players: 19 / 32
// Build: 9433646
// Server Number: 18

// Disconnect: #TF_MM_Generic_Kicked.

//IdleMatcher:name:rotatcepS ⚙
// Disconnect: #TF_Idle_kicked.
// Kicked due to inactivity

//disconnecting:
// Sending request to abandon current match
// Disconnecting from abandoned match server
// Sending request to exit matchmaking, marking assigned match as ended
// CAsyncWavDataCache: 758.wavs total 0 bytes, 0.00 % of capacity
// Lobby destroyed

// Differing lobby received. Lobby: [I:0:0]/Match0/Lobby0 CurrentlyAssigned: [A:1:4057276428:35341]/Match100094950/Lobby661129763878603 ConnectedToMatchServer: 0 HasLobby: 0 AssignedMatchEnded: 1


