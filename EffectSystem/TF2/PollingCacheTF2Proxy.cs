using ASPEN;

using System.Text.RegularExpressions;

using TF2FrameworkInterface;

namespace EffectSystem.TF2
{
    /// <summary>
    /// "Safely" polls the tf2 instance for regular status information and retains the most recent good responses for queries.
    /// Backs up & restores main config.cfg file to protect against commands run this session.
    /// </summary>
    public class PollingCacheTF2Proxy : TF2Proxy
    {
        private readonly TF2Instance tf2;

        private readonly Timer timer;
        private readonly Timer fasttimer;
        private readonly TF2LogOutput log;
        private readonly Dictionary<TF2Command, Action<string?>> commands;
        private readonly Dictionary<string, string?> Values;
        private readonly object valuesLock = new object();
        /// <summary>
        /// add a variable/command to this list if you don't want its GetValue to ever be reset by a null/blank value.  
        /// Sometimes they just don't load properly when polled, and sometimes that's hazardous to logic.
        /// </summary>
        public List<string> ClearValues { get; } = new();
        /// <summary>
        /// add a variable/command to this list if you don't want its GetValue to be restricted to only numeric result. 
        /// Sometimes they just don't load properly when polled.
        /// </summary>
        public List<string> StringValues { get; } = new();



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


        public PollingCacheTF2Proxy(TF2Instance tf2, string tf2Path)
        {
            this.tf2 = tf2;
            this.ConfigFilepath = Path.Combine(tf2Path, "tf/cfg/config.cfg");
            this.BackupConfigFilepath = Path.Combine(tf2Path, "tf/cfg/config.cfg-tf2spectatorbackup");

            BackupTF2ConfigFile();

            // Generally, add single variables and commands to be polled as "Values"
            Values = new()
            {
                //using getpos instead: ["net_channels"] = null, // | Shows net channel info
                ["getpos"] = null,

                ["name"] = null, // archive userinfo printonly svcanexec | Current user name

                ["info_class"] = null, // setinfo set by each class's cfg.
            };
            // list just status values that must be assumed changed to blank if not available.
            ClearValues = new()
            {
                //"net_channels",//TODO not sure about this
                "cl_crosshair_file",//"" is a valid value and is the true default (vs. "default")
            };
            // values are assumed to be numeric for any key not in this list
            StringValues = new()
            {
                "getpos",
                "name",
                "info_class",
                "net_channels",
                "cl_crosshair_file",
            };
            // If you have a custom command to run and want to cache its result, then use this.
            // May have to use this for SetInfo calls as well.
            commands = new()
            {
                // lightweight sanity test.
                [new StringCommand("echo _polling_")] = (s) => { },

                //[new TF2FrameworkInterface.StringCommand("net_channels")] = (s) => Values["net_channels"] = s,
            };

            timer = new Timer(PollTick, state: null,
                // start in 10 seconds, manual repeat
                dueTime: 1000 * 10, period: Timeout.Infinite);

            fasttimer = new Timer(FastPollTick, state: null,
                // start in 30 seconds, manual repeat
                dueTime: 1000 * 10, period: Timeout.Infinite);

            log = new TF2LogOutput(tf2Path);
            log.OnPlayerDied += PlayerDied;
            log.OnUserChangedClass += UserChangedClass;
            log.OnUserSelectedClass += UserSelectedClass;
            log.OnMapNameChanged += MapNameChanged;
            log.OnPlayerStatus += PlayerStatus;

            motionTracker = new MotionTracker(this);

            bindTracker = new BindTracker(this);
        }

        private MotionTracker motionTracker;
        private BindTracker bindTracker;

        private string ConfigFilepath { get; }
        private string BackupConfigFilepath { get; }
        private void BackupTF2ConfigFile()
        {
            //TODO maybe if backup file still exists, prompt user to quit TF2 and restore settings using it
            // if it exists, we failed to restore it on shutdown - restore it now.
            RestoreTF2ConfigFileFromBackup();
            // if it restored, it doesn't exist anymore, so we need to write it (again)

            try
            {
                File.Copy(ConfigFilepath, BackupConfigFilepath, overwrite: true);
            }
            catch (Exception ex)
            {
                Aspen.Log.ErrorException(ex, "Failure backing up tf2 config.cfg file - this is fine, unless something crashes without restoring settings.");
            }
        }

        private void RestoreTF2ConfigFileFromBackup()
        {
            try
            {
                if (!File.Exists(BackupConfigFilepath))
                    return;

                File.Copy(BackupConfigFilepath, ConfigFilepath, overwrite: true);
                try
                {
                    File.Delete(BackupConfigFilepath);
                }
                catch (Exception ex)
                {
                    Aspen.Log.ErrorException(ex, string.Format("Failure deleting old backup of tf2 config.cfg file - this may lead to issues with backing them up and/or restoring them later.  Consider quitting and manually deleting {0}", BackupConfigFilepath));
                }
            }
            catch (Exception ex)
            {
                Aspen.Log.ErrorException(ex, "Failure restoring tf2 config.cfg file - your settings might not be what you expect.");
            }
        }

        public void ShutDown()
        {
            // TF2 still running will probably overwrite any restored config - leave it for next time we start.
            // TODO This is not foolproof - IsOpen just refers to the RCON connection.
            if (!IsOpen)
                RestoreTF2ConfigFileFromBackup();

            Dispose();
        }

        private void Dispose()
        {
            timer.Dispose();
            fasttimer.Dispose();
            OnUserDied = null;
            OnUserKill = null;
            OnUserSpawned = null;
            log.Dispose();
            tf2.Dispose();
        }

        private void MapNameChanged(string mapName)
        {
            mapChanged = DateTime.Now;
            Map = mapName;

            // we start over.
            RecordUserDeathWithoutNotifying();

            SetInfo("info_class", string.Empty);
            lock (valuesLock)
            {
                Values["info_class"] = string.Empty;
            }
            ClassSelection = string.Empty;
        }

        // not a standard death - don't cause "OnDeath" events to fire just because map changed.
        private void RecordUserDeathWithoutNotifying()
        {
            UserLastDeath = DateTime.Now;
            _IsUserAlive = false;
        }

        private void PlayerStatus(TF2Status status)
        {
            //TODO somehow know when we're out of a server
            Server ??= new TF2Server();

            Server.RefreshPlayer(status);
        }

        private DateTime mapChanged = DateTime.MaxValue;
        public TimeSpan TimeInMap => mapChanged == DateTime.MaxValue ? new TimeSpan(0)
            : DateTime.Now - mapChanged;

        /// <summary>
        /// Last known map name (empty string if never loaded)
        /// </summary>
        public string Map { get; private set; } = string.Empty;

        private void PlayerDied(PlayerKiller circumstances)
        {
            if (circumstances.VictimName == UserName)
                UserGotKilled(circumstances);
            else if (circumstances.IsPlayer && circumstances.KillerName == UserName)
                UserGotAKill(circumstances);
        }

        private void UserGotKilled(PlayerKiller circumstances)
        {
            RecordUserDeath();
            if (circumstances.IsPlayer)
                Aspen.Log.Info($"User Died to {circumstances.KillerName} with {circumstances.KillerWeapon}. crit? {circumstances.IsCrit}");
            else
                Aspen.Log.Info($"User Died - nobody's fault");
        }

        private void RecordUserDeath()
        {
            UserLastDeath = DateTime.Now;
            IsUserAlive = false;
        }

        private DateTime UserLastDeath { get; set; } = DateTime.Now.AddMinutes(5);

        private void UserGotAKill(PlayerKiller circumstances)
        {
            // a kill more than 10s after death (afterburn kill) indicates we're alive.
            if (!IsUserAlive && DateTime.Now.Subtract(UserLastDeath).TotalSeconds > 10)
                IsUserAlive = true;

            Aspen.Log.Info($"User Killed {circumstances.VictimName} with {circumstances.KillerWeapon}. crit? {circumstances.IsCrit}");
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
                {
                    RecordUserDeathWithoutNotifying();
                    return false;
                }
                if (_IsUserAlive)
                    return true;

                if (string.IsNullOrEmpty(ClassSelection))
                    return false;

                InferIsUserAlive();

                if (_IsUserAlive)
                    OnUserSpawned?.Invoke();

                return _IsUserAlive;
            }
            set
            {
                if (_IsUserAlive == value)
                    return;

                _IsUserAlive = value;
                if (value)
                {
                    UserSpawnTime = DateTime.Now;
                    OnUserSpawned?.Invoke();
                }
                else
                    OnUserDied?.Invoke();
            }
        }

        private void InferIsUserAlive()
        {
            // We think we have a class defined since joining a map
            // if they didn't change class after death, we can't detect respawn,
            // so just assume they respawn a few seconds later.

            // about 5s for deathcam
            // typically 10s for respawn wave, but might require 2 waves.  We'll split the difference as 1.5 waves.
            double deathcamSeconds = 5;
            double maxSpawnwaveSeconds = 10; // not technically max, but typical max.
            double spawnSeconds = deathcamSeconds + 1.5 * maxSpawnwaveSeconds;
            // FUTURE if mp_disable_respawn_times is set to 1, just deathcam respawn

            DateTime now = DateTime.Now;
            TimeSpan timeSinceDeath = now.Subtract(UserLastDeath);
            bool diedTooLongAgo = timeSinceDeath > TimeSpan.FromSeconds(spawnSeconds);
            IsUserAlive = diedTooLongAgo;

            if (_IsUserAlive)
                UserSpawnTime = now;
        }

        public DateTime UserSpawnTime { get; private set; }
        public event TF2Proxy.UserSpawn? OnUserSpawned;
        public event TF2Proxy.UserDeath? OnUserDied;

        private void UserChangedClass(string playerClass)
        {
            if (ClassSelection == playerClass)
                return;

            Aspen.Log.Info($"User Spawned as new class {playerClass}");
            IsUserAlive = true;

            ClassSelection = playerClass;
        }
        /// <summary>
        /// Last known class spawn (empty string if never spawned)
        /// </summary>
        public string ClassSelection { get; private set; } = string.Empty;

        private void UserSelectedClass(string playerClass)
        {
            if (NextClassSelection == playerClass)
                return;

            Aspen.Log.Info($"User selected a new class {playerClass}");
            // can't assume user is alive yet.

            NextClassSelection = playerClass;
        }
        /// <summary>
        /// Last known class selection (expected next spawn)
        /// </summary>
        public string NextClassSelection { get; private set; } = string.Empty;

        public static readonly TimeSpan PollPeriod = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan PollPauseTime = TimeSpan.FromSeconds(15);
        private string tickRepeatedExceptionMessage = string.Empty;
        private void PollTick(object? state)
        {
            Aspen.Log.Trace(DateTime.Now.Ticks + " PollTick");

            PollOrPause(timer, PollPeriod, PollTick);

            Aspen.Log.Trace(DateTime.Now.Ticks + " PollTick after");
        }

        private void PollOrPause(Timer timer, TimeSpan period, Action action)
        {
            try
            {
                action?.Invoke();

                tickRepeatedExceptionMessage = string.Empty;
                // standard update period, manual repeat
                _ = timer.Change(period, Timeout.InfiniteTimeSpan);
            }
            catch (Exception pollEx)
            {
                if (tickRepeatedExceptionMessage != pollEx.Message)
                {
                    tickRepeatedExceptionMessage = pollEx.Message;
                    Aspen.Log.WarningException(pollEx, "unable to poll tf2 status - pausing for a bit");
                }
                // give us a long break to finish loading or whatever else is wrong.
                // This is to prevent game crashes we were getting during map loads.
                _ = timer.Change(PollPauseTime, Timeout.InfiniteTimeSpan);
            }
        }

        private void PollTick()
        {
            // polling "status" or anything else for additional log parsing.
            _ = tf2.SendCommand(new StringCommand(log.ActiveLoggingCommand), (s) => { })
                .Wait(MaxCommandRunTime);
            // sending a command first establishes the connection if it was not connected
            if (!tf2.IsConnected)
                return;

            InitializeLogWhenNeeded();

            PollCommandsAndVariables();

            PollForUserClassChangeNotification();
        }

        public static readonly TimeSpan FastPollPeriod = TimeSpan.FromMilliseconds(100);
        private void FastPollTick(object? state)
        {
            PollOrPause(fasttimer, FastPollPeriod, FastPollTick);
        }

        private void FastPollTick()
        {
            if (!tf2.IsConnected)
                return;

            // use getpos and time to calculate motion for other features.
            motionTracker.RecordUserMotion();
        }

        public double VerticalSpeed => motionTracker.GetVerticalSpeed();
        public double HorizontalSpeed => motionTracker.GetHorizontalSpeed();

        /// <summary>
        /// 300 Hu/s is normal walk speed. 400 for scout, but nobody walks straight up.
        /// Seems to work OK - pyro going up the tr_walkway ramp goes just under 200 vertically. 
        /// scout went up to 225
        /// </summary>
        public static readonly double MAX_VERTICAL_WALK_SPEED = 300;
        /// <summary>
        /// 76(.67) HU/s crouched heavy walk speed
        /// </summary>
        public static readonly double MIN_HORIZONTAL_WALK_SPEED = 76;
        public static readonly double CONGA_SPEED = 50;

        public bool IsJumping => Math.Abs(motionTracker.GetVerticalSpeed()) > MAX_VERTICAL_WALK_SPEED;
        public bool IsWalking => Math.Abs(motionTracker.GetHorizontalSpeed()) > MIN_HORIZONTAL_WALK_SPEED;

        /// <summary>
        /// One-time command has successfully run?
        /// </summary>
        private bool _PollingSetupRun = false;
        private void InitializeLogWhenNeeded()
        {
            // do any leftover initialization
            if (_PollingSetupRun)
                return;

            _PollingSetupRun =
                tf2.SendCommand(new StringCommand(log.SetupCommand), (s) => { })
                .Wait(MaxCommandRunTime);
            // log is set up, start monitoring it.
            if (_PollingSetupRun)
                log.StartMonitor();
        }

        private void PollCommandsAndVariables()
        {
            // Note: I kind of want to only lock during direct setting/querying of Values,
            // but one point here is to clear (one variable, and one dummy command) entries and fill them again
            // HOWEVER we only need a long long for those that get cleared, the rest can be handled individually.
            lock (valuesLock)
            {
                // in case we're timing out
                ResetResults();

                SetResetResults();
            }
            SetResults();
        }

        private void ResetResults()
        {
            foreach (string name in Values.Keys
                .Intersect(ClearValues))
                Values[name] = null;
            foreach ((TF2Command _, Action<string?> response) in commands)
                response?.Invoke(null);
        }

        private void SetResetResults()
        {
            foreach (string name in Values.Keys
                .Intersect(ClearValues))
                if (StringValues.Contains(name))
                    PollStringNow(name);
                else
                    PollNumberNow(name);

            foreach ((TF2Command command, Action<string?> response) in commands)
                _ = tf2.SendCommand(command, response
                    ).Wait(MaxCommandRunTime);
        }

        private void PollStringNow(string name)
        {
            Task task = tf2.SendCommand(new StringCommand(name),
                (s) =>
                {
                    // never clear "NeverClear" values.
                    if (string.IsNullOrWhiteSpace(s) && !ClearValues.Contains(name))
                        return;
                    Values[name] = s;
                }
                );
            lock (valuesLock)
            {
                bool completed = task.Wait(MaxCommandRunTime);
            }
        }

        // number format examples: 0 0. .0 0.0 -0.0 0.05f 300.f
        private readonly Regex cvarNumeric = new Regex(@"^\s*\-?(:?\d*\.?\d+|\d+\.?\d*)f?\s*$");
        private void PollNumberNow(string name)
        {
            Task task = tf2.SendCommand(new StringCommand(name),
                (s) =>
                {
                    if (cvarNumeric.IsMatch(s))
                        Values[name] = s;
                    //else
                    // just leave the last-known number.  We need good numbers.
                    //;
                }
                );
            lock (valuesLock)
            {
                bool completed = task.Wait(MaxCommandRunTime);
            }
        }

        private void SetResults()
        {
            List<string> nonClearedVariables = Values.Keys
                // these ones were polled in SetResetResults
                .Except(ClearValues).ToList();

            foreach (string name in nonClearedVariables)
                if (StringValues.Contains(name))
                    PollStringNow(name);
                else
                    PollNumberNow(name);
        }

        private void PollForUserClassChangeNotification()
        {
            string? playerClass;
            lock (valuesLock)
            {
                playerClass = Values["info_class"];
            }
            if (playerClass != null
                // before first set_info, returns 'Unknown command "info_class"'
                && !playerClass.Contains("info_class"))
                UserChangedClass(playerClass);
        }

        private TimeSpan MaxCommandRunTime = TimeSpan.FromSeconds(10);
        public string RunCommand(string command)
        {
            return RunCommand(command, tf2.SendCommand);
        }

        private string RunCommand(string command, Func<TF2Command, Action<string>, Task> sendCommand)
        {
            string result = string.Empty;

            if (IsLogworthy(command))
            {
                Aspen.Log.Info($"Run> {command}");
            }

            bool completed =
                sendCommand.Invoke(new StringCommand(command),
                (r) => result = r
                ).Wait(MaxCommandRunTime);

            // Oddly, if we restart this app this happens once... then further commands connect to the same TF2 instance just fine.
            // So one bad command resets the RCON port or something like that.
            if (!completed)
                Aspen.Log.Warning("TF2 command took too long - If this continues, restart TF2");
            //TODO consider caching failures and retrying them before the requested command.

            return result;
        }

        private string lastCommand = string.Empty;
        private bool IsLogworthy(string command)
        {
            bool result = command.Split(" ")[0] != lastCommand.Split(" ")[0];
            if (result)
                lastCommand = command;
            return result;
        }

        internal string RunCommandRaw(string command)
        {
            return RunCommand(command, tf2.SendCommandRaw);
        }

        public void SetInfo(string variable, string value)
        {
            _ = RunCommand("setinfo " + variable + " " + value);
        }

        public void SetValue(string variable, string value)
        {
            _ = RunCommand(variable + " " + value);
        }

        public string? GetValue(string key)
        {
            lock (valuesLock)
            {
                if (!Values.ContainsKey(key))
                    Values[key] = null;

                //this froze the app somehow:
                //if (string.IsNullOrWhiteSpace(Values[key]))
                //    if (!ClearValues.Contains(key))
                //        PollValueNow(key);

                return Values[key];
            }
        }

        public bool IsOpen
            => tf2 != null
            && tf2.IsConnected;

        /// <summary>
        /// The user's in-game name. 
        /// Non-clearing "name" variable value.
        /// </summary>
        public string? UserName => GetValue("name");

        /// <summary>
        /// Data about the currently connected game server, including its player list.
        /// </summary>
        public TF2Server? Server { get; private set; }

        /// <summary>
        /// Data about the streamer's player in the game server - null if not in a game.
        /// </summary>
        public TF2Player? User => Server?.GetPlayer(name: UserName);

        // ways to infer that we're in a map
        // getpos not 000, net_channels, tf_party_debug
        // getpos - stays 000 until you're in the map (at least a camera).
        public bool IsMapLoaded => IsGetPosSomewhere();

        public string AllValues => string.Join("\n",
            Values?.Select(x => x.Key + "->" + x.Value)
            ?? []);

        // not in map (no camera): "setpos 0.0 0.0 0.0;setang 0.0 0.0 0.0" (either order or sometimes one is missing or has newline)
        private Regex setpos000 = new Regex(@".*setpos( 0(\.0+)?){3}.*", RegexOptions.Singleline);

        // getpos: in map: "setpos 2061.664551 -5343.968750 -948.968689;setang 14.071210 59.451595 0.000000"
        // "setang 9.200377 165.286209 0.000000
        // setpos 361.759491 -559.780396 231.349213;"
        private bool IsGetPosSomewhere()
        {
            //new System.Windows.Media.Media3D.Point3D(0.0, 0.0, 0.0);
            //new System.Windows.Media.Media3D.Vector3D(0.0, 0.0, 0.0);
            string? getpos = GetValue("getpos");

            if (string.IsNullOrWhiteSpace(getpos))
                return false;

            bool posSomewhereOtherThan0 = !setpos000.IsMatch(getpos);

            return posSomewhereOtherThan0;
        }

        // net_channels: "No active net channels."
        private bool IsActiveNetChannels()
        {
            string? net_channels = GetValue("net_channels");

            //NOTE net_channels=no active returns true while still loading into the map.
            if (string.IsNullOrWhiteSpace(net_channels))
                return false;

            bool activeChannels = !net_channels.Contains("No active net channels.");

            return activeChannels;
        }

        public CommandBinding? GetCommandBinding(string command) => bindTracker.GetCommandBinding(command);

        // net_status: includes "- Config: Multiplayer, listen, 0 connections"
        // tf_party_debug: includes "associated_lobby_id: 0"

    }
}