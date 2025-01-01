using System.Text.RegularExpressions;

namespace Effects.TF2
{
    /// <summary>
    /// Provides the latest known TF2 Status
    /// </summary>
    public interface TF2Proxy
    {
        public bool IsOpen { get; }

        public bool IsMapLoaded { get; }

        string? GetValue(string variable);
        //public string Map { get; }

        public string ClassSelection { get; }
        public bool IsUserAlive { get; }

        public delegate void UserKill(string victim, string weapon, bool crit);
        public event UserKill? OnUserKill;

        public delegate void UserSpawn();
        public event UserSpawn? OnUserSpawned;
        public delegate void UserDeath();
        public event UserDeath? OnUserDied;
    }

    /// <summary>
    /// "safely" polls the tf2 instance for regular status information and retains the most recent good responses for queries.
    /// </summary>
    public class TF2Poller : TF2Proxy
    {
        private readonly TF2FrameworkInterface.TF2Instance tf2;
        private readonly Timer timer;
        private readonly TF2LogOutput log;
        private readonly Dictionary<TF2FrameworkInterface.TF2Command, Action<string?>> commands;
        private readonly Dictionary<string, string?> Values;
        /// <summary>
        /// add a variable/command to this list if you don't want its GetValue to ever be reset by a null/blank value.  
        /// Sometimes they just don't load properly when polled, and sometimes that's hazardous to logic.
        /// </summary>
        public List<string> NeverClearValues { get; } = new();



        /*
         * App.Server.Players[n].Team .UUID (.Name) (.connectedtime) (.kickuserid) (.ping/loss/state)
         * App.Server(.map(.name .at) .hostname .tags .playercounts .udpip .hostname)
         * App.Server.User.position/angle .latestclass
         * App.Party
         * App.

        ViewModel.IsVROnly
        ViewModel.IsVRTaunts
        ViewModel.IsOn
        ViewModel.IsSmall
        ViewModel.FieldOfView
         * .cl_first_person_uses_world_model
         * .tf_taunt_first_person 1;wait 20000;cl_first_person_uses_world_model 0;tf_taunt_first_person 0	
         No Guns	
         .r_drawviewmodel {r_drawviewmodel|1:on|0:off} 
         Big Guns	
         .tf_use_min_viewmodels {tf_use_min_viewmodels|1:small|0:big}
         Long Arms
         .viewmodel_fov 160 / 54

        Screen.ColorMap
        Screen.DetailPercent
        Screen.Bloom
         * .mat_color_projection 4 (black and white) / 0
         * .mat_viewportscale 0.1 /  1
         * .mat_bloom_scalefactor_scalar 50 / 1

        //hud_classautokill 1 Automatically kill player after choosing a new playerclass

        HUD.UsePlayerModel
         boring HUD	
         .cl_hud_playerclass_use_playermodel 3d player class model is {cl_hud_playerclass_use_playermodel|1:on|0:off}
        //cl_hud_minmode 0 client archive Set to 1 to turn on the advanced minimalist HUD mode
        //hud_draw_active_reticle 0
        //hud_draw_fixed_reticle 0
        //hud_magnetism 0.3 (no doc)

        medic radar
        //hud_medicautocallers 0
        //hud_medicautocallersthreshold 75

        not sure:
        //hud_reticle_alpha_speed 700
        //hud_reticle_maxalpha 255
        //hud_reticle_minalpha 125
        //hud_reticle_scale 1.0

        (tf_contract_progress_show 1	Settings for the contract HUD element: 0 show nothing, 1 show everything, 2 show only active contracts.)

        Crosshair.File
         cl_crosshair_file crosshair1 / ""
        Crosshair.Color.Red/Green/Blue
         cl_crosshair_red {1}
         cl_crosshair_green {1}
         cl_crosshair_blue {1}
        Crosshair.Scale
         cl_crosshair_scale {1|default:32|normal:32|giant:3000|big:100}
         cl_crosshair_file "";cl_crosshair_blue 200;cl_crosshair_green 200;cl_crosshair_red 200;cl_crosshair_scale 32;cl_crosshairalpha 200

        Sound.Kill
        Sound.Hit
         (SHOULD be possible to insert custom sounds here from a list of files and reload scheme or something.)
         kill Default	tf_dingalingaling_last_effect "0"		
         hit Default	tf_dingalingaling_effect "0"		
         tf_dingalingaling_effect	{1|0:0 (Default)|1:1 (Electro)|2:2 (Notes)|3:3 (Percussion)|4:4 (Retro)|5:5 (Space)|6:6 (Beepo)|7:7 (Vortex)|8:8 (Squasher)}


         * 
         * 
         * TF2 is running (RCON is running)
         * - connected to a server (getpos not 000, net_channels, tf_party_debug)
         * -- player list (tf_lobby_debug team/counts) non-valve or not connected: "Failed to find lobby shared object"
         * 
         * --- names & time in server (status)
         * --- map (status, path)
         * -- getpos/spec_pos - infer dead or spawned (but spectating may look the same)
         * - last selected class (set variable(s) in per-class config) (MAYBE fires every spawn??)
         * -- dead/alive 
         * 
         * 
         * setpos 2061.664551 -5343.968750 -948.968689;setang 14.071210 59.451595 0.000000
         * 
        dead:
    setang 9.200377 165.286209 0.000000
    setpos 361.759491 -559.780396 231.349213;
    getpos
    setpos 590.913391 -574.194519 231.349213;setang 9.200377 165.286209 0.000000
        respawned:
    getpos
    setpos -323.000000 912.000000 203.031311;setang 0.000000 -93.004761 0.000000
    getpos
    setpos -323.000000 912.000000 203.031311;setang 0.000000 -93.004761 0.000000
        * 
        * so maybe if decimal value is all 0's for pos and ang ...well.. respawn happend :/
         *
         * path has "map\blksadflkj.bsp" for connected map I guess... test when not connected
         * net_channels has connection time & remote IP/port
         * tf_party_debug has party and matched game info
         *
         * spec_goto 2061.7 -5344.0 -949.0 14.1 59.5
         * 
         * status -
         * hostname :
         * version  : num/num num secure
         * udp/ip   : ip:port
         * steamid  : [G:1:...] (numnumnum)    <-- maybe this is "me"? or is it the server's steamid?
         * account  : not logged in   (No account specified)
         * map      : ctf_turbine_winter at: 0 x, 0 y, 0 z
         * tags     : alltalk,ctf,...
         * sourcetv : ip:port, delay 0.0s
         * players  : 97 humans, 1 bots (101 max)
         * edicts   : 1244 used of 2048 max
         * # userid name         uniqueid             connected ping loss state
         * #     nn "name"       [U:1:...]            1:14:03     77    0 active
         * 
    CTFLobbyShared: ID:00024dda327e2c74  24 member(s), 0 pending
    Member[0] [U:1:449867098]  team = TF_GC_TEAM_INVADERS  type = MATCH_PLAYER
    Member[7] [U:1:1100925799]  team = TF_GC_TEAM_DEFENDERS  type = MATCH_PLAYER
         */


        /*
         * net_channels - includes connected, ip, connection time

    NetChannel 'CLIENT':
    - online: 01:27
    - remote IP: 169.254.14.98:11872 
    - reliable: available
    - latency: 0.1, loss 0.00
    - packets: in 66.4/s, out 67.0/s
    - choke: in 0.01, out 0.01
    - flow: in 28.2, out 4.7 kB/s
    - total: in 2.0, out 0.4 MB

        * disconnected:
    No active net channels.


        * net_status
    Net status for host 0.0.0.0:
    - Ports: Client 27005, Server 41394, HLTV 0, Matchmaking 0, Systemlink 0
    - Config: Multiplayer, listen, 1 connections
    - Latency: avg out 0.08s, in 0.01s
    - Packets: net total out  66.9/s, in 67.3/s
    - Loss:    avg out 0.0, in 0.0
           per client out 66.9/s, in 67.3/s
    - Data:    net total out  5.1, in 14.5 kB/s
           per client out 5.1, in 14.5 kB/s

        * disconnected net_status:
    Net status for host 0.0.0.0:
    - Config: Multiplayer, listen, 0 connections
    - Ports: Client 27005, Server 41394, HLTV 0, Matchmaking 0, Systemlink 0


         * tf_party_debug
    TFParty: ID:00024dda934f5e35  1 member(s)  LeaderID: [U:1:123650837]
    ------
    In 0 queues:
    party_id: 648551123082805
    leader_id: 76561198083916565
    member_ids: 76561198083916565
    members {
    owns_ticket: true
    competitive_access: true
    player_criteria {
    }
    activity {
    lobby_id: 648551031634256
    lobby_match_group: k_eTFMatchGroup_Casual_12v12
    multiqueue_blocked: false
    online: true
    client_version: 9087997
    }
    }
    associated_lobby_id: 648551031634256
    group_criteria {
    casual_criteria {
    selected_maps_bits: 2147745792
    selected_maps_bits: 541197384
    selected_maps_bits: 0
    selected_maps_bits: 4194912
    selected_maps_bits: 87572496
    selected_maps_bits: 930635776
    }
    custom_ping_tolerance: 80
    }
    associated_lobby_match_group: k_eTFMatchGroup_Casual_12v12
    leader_ui_state {
    menu_step: k_eTFSyncedMMMenuStep_Configuring_Mode
    match_group: k_eTFMatchGroup_Casual_12v12
    }

        * out of a match: (associated_lobby_id: 0  AND  associated_lobby_match_group: k_eTFMatchGroup_Invalid)
    TFParty: ID:00024dda934f5e35  1 member(s)  LeaderID: [U:1:123650837]
    In 0 queues:
    ------
    party_id: 648551123082805
    leader_id: 76561198083916565
    member_ids: 76561198083916565
    members {
    owns_ticket: true
    competitive_access: true
    player_criteria {
    }
    activity {
    lobby_id: 0
    lobby_match_group: k_eTFMatchGroup_Invalid
    multiqueue_blocked: false
    online: true
    client_version: 9087997
    }
    }
    associated_lobby_id: 0
    group_criteria {
    casual_criteria {
    selected_maps_bits: 2147745792
    selected_maps_bits: 541197384
    selected_maps_bits: 0
    selected_maps_bits: 4194912
    selected_maps_bits: 87572496
    selected_maps_bits: 930635776
    }
    custom_ping_tolerance: 80
    }
    associated_lobby_match_group: k_eTFMatchGroup_Invalid
    leader_ui_state {
    menu_step: k_eTFSyncedMMMenuStep_Configuring_Mode
    match_group: k_eTFMatchGroup_Casual_12v12
    }
        * 


         * stats (huh..players and connects 0 on a working server I guess its for when you make your own)
    CPU    In_(KB/s)  Out_(KB/s)  Uptime  Map_changes  FPS      Players  Connects
    0.00   0.00       0.00        45      -1           111.08   0        0

         * star_memory has memory usage info
         * memory has more detail

         * path - includes some map-specific stuff to find map name. well at least the one that starts "maps\map_name.bsp"
         * (I think I added the (map) after the interesting one)
    path
    TF2FrameworkInterface.StringCommand: ---------------
    Paths:
    "s:\games\steamlibrary\steamapps\common\team fortress 2\" "BASE_PATH" 
    "maps\ctf_haarp.bsp" "GAME" (map)
    "s:\games\steamlibrary\steamapps\common\team fortress 2\bin\x64\" "EXECUTABLE_PATH" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\bin\" "EXECUTABLE_PATH" 
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\custom\mastercomfig-high-preset.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\platform\" "PLATFORM" 
    "mod" "mod" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\custom\mastercomfig-high-preset.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\aaaaaaaaaa_loadfirst_tf2_bot_detector\" "GAME" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\post-upgrade uninstall\" "GAME" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\aaaaaaaaaa_loadfirst_tf2_bot_detector\" "mod" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\post-upgrade uninstall\" "mod" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\aaaaaaaaaa_votefailed_eraser_v2\" "GAME" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\toonhud\" "GAME" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\aaaaaaaaaa_votefailed_eraser_v2\" "mod" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\toonhud\" "mod" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\aaaaaaaaaa_votefailed_eraser_v2\" "custom_mod" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\toonhud\" "custom_mod" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\uninstalled\" "GAME" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\uninstalled\" "mod" 
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\workshop\" "GAME" 
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\hl2\hl2_sound_vo_english.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\custom\workshop\" "mod" 
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\hl2\hl2_sound_misc.vpk
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_textures.vpk
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\hl2\hl2_misc.vpk
    "mod" "mod" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_textures.vpk
    "vgui" "vgui" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\hl2\hl2_misc.vpk
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_sound_vo_english.vpk
    "PLATFORM" "PLATFORM" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\platform\platform_misc.vpk
    "mod" "mod" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_sound_vo_english.vpk
    "vgui" "vgui" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\platform\platform_misc.vpk
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_sound_misc.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\" "mod" 
    "mod" "mod" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_sound_misc.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\" "mod_write" 
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_misc.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\" "default_write_path" 
    "mod" "mod" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_misc.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\" "GAME" 
    "vgui" "vgui" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\tf\tf2_misc.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\" "game_write" 
    "GAME" "GAME" (VPK)S:\Games\SteamLibrary\steamapps\common\Team Fortress 2\hl2\hl2_textures.vpk
    "s:\games\steamlibrary\steamapps\common\team fortress 2\tf\bin\x64\" "gamebin"


         */


        public TF2Poller(TF2FrameworkInterface.TF2Instance tf2, string tf2Path)
        {
            this.tf2 = tf2;

            // Generally, add single variables and commands to be polled as "Values"
            Values = new()
            {
                ["net_channels"] = null, // | Shows net channel info
                ["name"] = null, // archive userinfo printonly svcanexec | Current user name
            };
            //TODO should probably invert this and list just status values that must be assumed changed to blank if not available.
            NeverClearValues = new()
            {
                "name", // we always have a name - don't clear it out just from flakey rcon calls.

                // most config values can be assumed unchanged.
                "r_drawviewmodel",
                "tf_use_min_viewmodels",
                "viewmodel_fov",
                "cl_first_person_uses_world_model",
                "tf_taunt_first_person",

                "mat_bloom_scalefactor_scalar",
                "mat_force_bloom",
                "mat_color_projection",
                "mat_viewportscale",

            };
            // If you have a custom command to run and want to cache its result, then use this.
            // May have to use this for SetInfo calls as well.
            commands = new()
            {
                // lightweight sanity test.
                [new TF2FrameworkInterface.StringCommand("echo _polling_")] = (s) => { },

                //[new TF2FrameworkInterface.StringCommand("net_channels")] = (s) => Values["net_channels"] = s,
            };

            timer = new Timer(PollTick, state: null,
                // start in 10 seconds, manual repeat
                dueTime: 1000 * 10, period: Timeout.Infinite);

            log = new TF2LogOutput(tf2, tf2Path);
            _ = tf2.SendCommand(new TF2FrameworkInterface.StringCommand(log.SetupCommand), (s) => { });
            log.OnPlayerDied += PlayerDied;
            log.OnUserChangedClass += UserChangedClass;
            log.StartMonitor();
        }

        private void PlayerDied(PlayerKiller circumstances)
        {
            if (circumstances.VictimName == UserName)
                UserGotKilled(circumstances);
            else if (circumstances.IsPlayer && circumstances.KillerName == UserName)
                UserGotAKill(circumstances);
        }

        private void UserGotKilled(PlayerKiller circumstances)
        {
            UserLastDeath = DateTime.Now;
            IsUserAlive = false;
            if (circumstances.IsPlayer)
                ASPEN.Aspen.Log.Info($"User Died to {circumstances.KillerName} with {circumstances.KillerWeapon}. crit? {circumstances.IsCrit}");
            else
                ASPEN.Aspen.Log.Info($"User Died - nobody's fault");
        }
        private DateTime UserLastDeath { get; set; } = DateTime.MaxValue;

        private void UserGotAKill(PlayerKiller circumstances)
        {
            IsUserAlive = true;
            ASPEN.Aspen.Log.Info($"User Killed {circumstances.VictimName} with {circumstances.KillerWeapon}. crit? {circumstances.IsCrit}");
            OnUserKill?.Invoke(circumstances.VictimName, circumstances.KillerWeapon, circumstances.IsCrit);
        }
        public event TF2Proxy.UserKill? OnUserKill;

        private bool _IsUserAlive = false;
        /// <summary>
        /// Whether the user is believed alive (default: map loaded and more than 30s since last death)
        /// </summary>
        public bool IsUserAlive
        {
            get
            {
                if (!IsMapLoaded)
                    return false;
                if (_IsUserAlive)
                    return true;

                // about 5s for deathcam
                // typically 10s for respawn wave, but might require 2 waves.  We'll split the difference as 1.5 waves.
                double deathcamSeconds = 5;
                double maxSpawnwaveSeconds = 10; // not technically max, but typical max.
                double spawnSeconds = deathcamSeconds + (1.5 * maxSpawnwaveSeconds);
                // FUTURE if mp_disable_respawn_times is set to 1, just deathcam respawn

                TimeSpan timeSinceDeath = DateTime.Now.Subtract(UserLastDeath);
                bool diedTooLongAgo = timeSinceDeath > TimeSpan.FromSeconds(spawnSeconds);
                IsUserAlive = diedTooLongAgo;

                return _IsUserAlive;
            }
            set
            {
                if (_IsUserAlive == value)
                    return;

                _IsUserAlive = value;
                if (value)
                    OnUserSpawned?.Invoke();
                else
                    OnUserDied?.Invoke();
            }
        }
        public event TF2Proxy.UserSpawn? OnUserSpawned;
        public event TF2Proxy.UserDeath? OnUserDied;

        private void UserChangedClass(string playerClass)
        {
            ASPEN.Aspen.Log.Info($"User Spawned as new class {playerClass}");
            IsUserAlive = true;

            ClassSelection = playerClass;
        }
        /// <summary>
        /// Last known class spawn (empty string if never spawned)
        /// </summary>
        public string ClassSelection { get; private set; } = string.Empty;

        private void PollTick(object? state)
        {
            try
            {
                // not currently polling "status" or anything else for additional log parsing.
                //_ = tf2.SendCommand(new TF2FrameworkInterface.StringCommand(log.ActiveLoggingCommand), (s) => { });

                PollCommandsAndVariables();

                // standard update period, manual repeat
                _ = timer.Change(1000, Timeout.Infinite);
            }
            catch (Exception pollEx)
            {
                ASPEN.Aspen.Log.WarningException(pollEx, "unable to poll tf2 status - pausing for a bit");
                // give us a long break to finish loading or whatever else is wrong.
                // This is to prevent game crashes we were getting during map loads.
                _ = timer.Change(1000 * 15, Timeout.Infinite);
            }
        }

        private void PollCommandsAndVariables()
        {
            lock (this)
            {
                // reset results in case we're timing out
                foreach (string name in Values.Keys
                    .Except(NeverClearValues))
                    Values[name] = null;
                foreach ((TF2FrameworkInterface.TF2Command _, Action<string?> response) in commands)
                    response?.Invoke(null);

                foreach (string name in Values.Keys)
                    tf2.SendCommand(new TF2FrameworkInterface.StringCommand(name),
                        (s) =>
                        {
                            // never clear "NeverClear" values.
                            if (string.IsNullOrEmpty(s) && NeverClearValues.Contains(name))
                                return;
                            Values[name] = s;
                        }
                        ).Wait();
                foreach ((TF2FrameworkInterface.TF2Command command, Action<string?> response) in commands)
                    tf2.SendCommand(command, response
                        ).Wait();
            }
        }

        public void Dispose()
        {
            timer.Dispose();
            OnUserDied = null;
            OnUserKill = null;
            OnUserSpawned = null;
        }

        public string? GetValue(string key)
        {
            lock (this)
            {
                if (!Values.ContainsKey(key))
                    Values[key] = null;

                return Values[key];
            }
        }

        public bool IsOpen
        {
            get
            {
                return tf2 != null;
                //TODO (taken care of elsewhere now I think?) detect disconnected, provide reconnect code that resets that detection.
            }
        }

        /// <summary>
        /// The user's in-game name. 
        /// Non-clearing "name" variable value.
        /// </summary>
        public string? UserName => GetValue("name");

        // ways to infer that we're in a map
        // getpos not 000, net_channels, tf_party_debug
        // getpos - stays 000 until you're in the map (at least a camera).
        public bool IsMapLoaded => IsGetPos000();

        // not in map (no camera): "setpos 0.0 0.0 0.0;setang 0.0 0.0 0.0" (either order or sometimes one is missing or has newline)
        private Regex setpos000 = new Regex(@".*setpos( 0(\.0+)?){3}");

        // getpos: in map: "setpos 2061.664551 -5343.968750 -948.968689;setang 14.071210 59.451595 0.000000"
        // "setang 9.200377 165.286209 0.000000
        // setpos 361.759491 -559.780396 231.349213;"
        private bool IsGetPos000()
        {
            //new System.Windows.Media.Media3D.Point3D(0.0, 0.0, 0.0);
            //new System.Windows.Media.Media3D.Vector3D(0.0, 0.0, 0.0);
            string? getpos;
            lock (this)
            {
                getpos = GetValue("getpos");
            }

            if (string.IsNullOrWhiteSpace(getpos))
                return false;

            bool activeChannels = !setpos000.IsMatch(getpos);

            return activeChannels;
        }

        // net_channels: "No active net channels."
        private bool IsNoActiveNetChannels()
        {
            string? net_channels;
            lock (this)
            {
                net_channels = GetValue("net_channels");
            }

            //NOTE net_channels=no active returns true while still loading into the map.
            if (string.IsNullOrWhiteSpace(net_channels))
                return false;

            bool activeChannels = !net_channels.Contains("No active net channels.");

            return activeChannels;
        }
        // net_status: includes "- Config: Multiplayer, listen, 0 connections"
        // tf_party_debug: includes "associated_lobby_id: 0"

    }

}