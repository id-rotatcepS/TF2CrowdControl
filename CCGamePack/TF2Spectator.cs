using ConnectorLib.SimpleTCP;

using CrowdControl.Common;
// also uses CrowdControl.Games.dll for SimpleTCPPack<?>
// also uses CrowdControl.Extensions.dll for e.g. SITimeSpan

namespace CrowdControl.Games.Packs.TF2Spectator;

/*
    Lexi [Developer] — 3/19/2025 7:29 PM
    FYI some extra clarification:
    - A *Game* is a JSON file describing an entity like `Super Mario 64`.
    - Every *Game* has at least one *Game Pack* that defines a unique version of the game; for example, SM64 has 2\*: `Super Mario 64` and `Super Mario 64 Randomizer`
    - Every *Game Pack* has a JSON file that defines metadata for the game version such as how Crowd Control connects to the game(/mod) and what effects are available
    - Every\*\* *Game Pack* has a C# file which is used to generate the aforementioned *Game Pack* JSON file. They also implement the logic for connecting to the game(/mod), although this usually just entails extending a pre-defined connector class.
    - The *Game Pack* C# file gets compiled to a DLL and (down)loaded into the Crowd Control desktop app's *Native Client*, which manages effect queueing & retrying & timing & communication with our PubSub server & etc
    - The *Game Mod* is coded in any language and generally connects to the *Game Pack* loaded into the *Native Client* over a local TCP or WebSocket connection and can send responses to effect requests, effect variables, and reports on what effects should be visible & purchasable
    -# \*It actually has 4; BizHawk, Project64, Randomizer v0.5, & Randomizer v1.2
    -# \*\*Standalone/Unity games generally do not have an accompany C# file or game mod and instead connect directly to our PubSub backend service
 */

/// <summary>
/// Crowd Control Game Pack class.
/// 
/// Used to generate the Game Pack JSON file (that defines metadata for the game version such as
/// how Crowd Control connects to the game(/mod) and what effects are available).
/// Implements the logic for connecting to the game(/mod) (mostly by extending a pre-defined connector class).
/// This file gets compiled to a DLL and (down)loaded into the Crowd Control desktop app's Native Client,
/// which manages effect queueing & retrying & timing & communication with the PubSub server, etc.
/// </summary>
public class TF2Spectator : SimpleTCPPack<SimpleTCPServerConnector>
{
    public TF2Spectator(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler)
        : base(player, responseHandler, statusUpdateHandler) { }

    public static readonly string C_CHALLENGES = "Challenges";
    public static readonly string C_CAMERA = "Camera";
    public static readonly string C_CROSSHAIR = "Crosshair";
    public static readonly string C_GAMEPLAY = "Gameplay";
    public static readonly string C_HUD = "HUD";
    public static readonly string C_MOVEMENT = "Movement";

    public static readonly string CROWD_CONTROL_HOST = "127.0.0.1";//TODO share with my SimpleTCPClient instance
    public static readonly ushort APP_CROWD_CONTROL_PORT = 58430;//TODO share with my SimpleTCPClient instance

    /// <summary>
    /// Group: only requires loading the app/game
    /// </summary>
    public static readonly string G_APP = "app";
    /// <summary>
    /// Group: requires being loaded into a map
    /// </summary>
    public static readonly string G_MAP = "map_loaded";
    /// <summary>
    /// Group: requires being alive
    /// </summary>
    public static readonly string G_ALIVE = "alive";
    public static readonly string G_SCOUT = "scout";
    public static readonly string G_SOLLY = "soldier";
    public static readonly string G_PYRO = "pyro";
    public static readonly string G_DEMO = "demoman";
    public static readonly string G_ENGY = "engineer";
    public static readonly string G_HEAVY = "heavyweapons";
    public static readonly string G_MEDIC = "medic";
    public static readonly string G_SNIPER = "sniper";
    public static readonly string G_SPY = "spy";

    #region Camera
    /// <summary>
    /// mat_color_projection 4 (vs 0)
    /// Affects entire game including menus
    /// </summary>
    public static readonly Effect blackandwhite = new("Black & White", "blackandwhite")
    {
        #region user facing

        //string Name (constructor);
        /// ...used to distinguish identically-named effects
        //string? Note; // "subtitle" next to Name
        Description = "TF2 in the 50s.",
        SortName = "Camera: Black and White",
        //string? Image;
        // = "https://resources.crowdcontrol.live/images/Minecraft/Minecraft/icons/freeze.png"

        // Default Price
        Price = 30,
        //bool? NoPooling;

        // Default Duration (timespan seconds) Max: 600s (preferred max 180s)
        /// The duration field is used for effects which last for a period of time. 
        /// They specify a default length in seconds from 1s-180s and can be overridden
        /// by the streamer.
        Duration = TimeSpan.FromSeconds(60),

        //ParameterList? Parameters;
        //Parameters = new ParameterList(new ParameterDef[] {
        //    new ParameterDef("param definition", "def1",
        //        new Parameter("param option one", "p1", 20),
        //        new Parameter("param option two", "p2", 30))
        //}),

        /// The quantity field accepts an object with a min integer gte 1 and a 
        /// max integer gte 2. It allows viewers to purchase multiple of the effect
        /// in bulk, for example to give a stack of items or other collectibles.
        //QuantityRange Quantity = 1;
        //uint DefaultQuantity;

        // user-facing collections (can be used to disable/hide a set with an Effect Report)
        Category = new EffectGrouping(C_CAMERA),

        //bool Disabled;

        #endregion user facing

        #region streamer facing
        // Editable Duration for CC Pro streamers?
        IsDurationEditable = true,
        // Default price increase percent per purchase (float)
        ScaleFactor = 0.5f,
        // Default price decrease interval (timespan minutes)
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        // Default user cooldown (timespan minutes nullable) Max: 120s (preferred: price-based)
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        // Default stream cooldown (timespan minutes nullable)
        //SessionCooldown = TimeSpan.FromMinutes(0),

        ///The optional inactive boolean field is used to indicate whether an effect 
        ///should be visible on a streamer's effect list by default. This is used to 
        ///reduce clutter in packs with a lot of repetitive effects (such as spawns 
        ///for every enemy in a game), but which certain streamers may still wish to enable.
        //bool Inactive;
        #endregion streamer facing

        //string ID (constructor);
        //ItemKind Kind (constructor, Effect or Bidwar);

        //SITimeSpan? ResponseTimeout;
        //uint? RetryMaxAttempts;
        //SITimeSpan? RetryMaxTime;
        //NonNumericRange<SITimeSpan>? RetryInterval;

        //EffectGrouping? ScaleGroups;
        //ScaleGroups = new EffectGrouping(G_MAP),

        // internal collections (can be used to disable/hide a set with an Effect Report)
        Group = new EffectGrouping(G_APP),

        //uint? QueueWarning;
        //uint? QueueMax;

        //string? ErrorOverride;
        //string? StartMessage;
        //string? EndMessage;

        //List<string>? Tags;
        //List<string>? Metadata;
        //List<Connector>? IncompatibleConnectors;

        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };
    public static readonly Effect blackandwhite_challenge_5ks = new("5 Killstreak Challenge", "blackandwhite_challenge_5ks")
    {
        Note = "Black & White",
        SortName = "Challenge: Killstreak Black and White",
        Duration = TimeSpan.FromMinutes(10),
        Description = "Stuck with TF2 in the 50s until I get a 5 kill streak.",
        Category = new EffectGrouping(C_CAMERA, C_CHALLENGES),
        Price = 250,
        #region streamer facing
        IsDurationEditable = false,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_APP),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };
    /// <summary>
    /// mat_viewportscale 0.1 (vs 1)
    /// Affects game generation - relative to primary resolution.
    /// </summary>
    public static readonly Effect pixelated = new("Pixelated", "pixelated")
    {
        SortName = "Camera: Pixelated",
        Description = "TF2 in the 80s.",
        Duration = TimeSpan.FromSeconds(30),
        Category = new EffectGrouping(C_CAMERA),
        Price = 40,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };
    /// <summary>
    /// mat_bloom_scalefactor_scalar 50 (vs 1) and mat_force_bloom 1
    /// Affects game generation.
    /// </summary>
    public static readonly Effect dream = new("Dream Mode", "dream")
    {
        SortName = "Camera: Dream Mode",
        Description = "The radiant glow of TF2 in a dream.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping(C_CAMERA),
        Price = 40,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };

    //public static readonly Effect wallhacks_grass = new("Wallhacks for Grass", "wallhacks_grass")
    //{
    //    SortName = "Camera: Wallhacks for Grass",
    //    Description = "At least it doesn't count as a cheat. (No effect unless player is near grass on the map)",
    //    Duration = TimeSpan.FromSeconds(60),
    //    IsDurationEditable = true,
    //    //Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    //    Category = new EffectGrouping(C_CAMERA),
    //    Group = new EffectGrouping(G_MAP),
    //    Price = 5
    //};
    #endregion Camera

    #region View Model
    /// <summary>
    /// tf_use_min_viewmodels 0 (vs 1) and r_drawviewmodel 1
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect big_guns = new("Big Guns", "big_guns")
    {
        SortName = "View Model: Big Guns",
        Description = "Force my usual weapon viewmodels to the default big ones.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping("View Model"),
        Price = 5,
        #region streamer facing
        IsDurationEditable = true,
        //ScaleFactor = 0.5f,
        //ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
    };
    /// <summary>
    /// tf_use_min_viewmodels 0 (vs 1) and r_drawviewmodel 1
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect small_guns = new("Small Guns", "small_guns")
    {
        SortName = "View Model: Small Guns",
        Description = "Force my usual weapon viewmodels to small ones.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping("View Model"),
        Price = 5,
        #region streamer facing
        IsDurationEditable = true,
        //ScaleFactor = 0.5f,
        //ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
    };
    /// <summary>
    /// r_drawviewmodel 0/1 toggle
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect no_guns = new("No Guns?", "no_guns")
    {
        SortName = "View Model: No Guns",
        Description = "Force a change to whether my weapon viewmodels are visible.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping("View Model"),
        Price = 10,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
    };
    /// <summary>
    /// viewmodel_fov 160
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect long_arms = new("Long Arms", "long_arms")
    {
        SortName = "View Model: Long Arms",
        Description = "Force my viewmodels to have very long arms.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping("View Model"),
        Price = 10,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
    };
    /// <summary>
    /// turn on cl_first_person_uses_world_model and tf_taunt_first_person
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect vr_mode = new("VR Mode", "vr_mode")
    {
        SortName = "View Model: VR Mode",
        Description = "My arms, weapons, and body are visible exactly as other players see them in game.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping("View Model"),
        Price = 5,
        #region streamer facing
        IsDurationEditable = true,
        //ScaleFactor = 0.5f,
        //ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
    };
    #endregion View Model

    #region Crosshair
    /// <summary>
    /// animate a rainbow effect on the crosshair's color.
    /// Affects crosshair color
    /// </summary>
    public static readonly Effect crosshair_rainbow = new("Rainbow Crosshair", "crosshair_rainbow")
    {
        Description = "A rainbow of colors for my crosshair.",
        Duration = TimeSpan.FromSeconds(120),// half max duration - the effect is subtle.
        Category = new EffectGrouping(C_CROSSHAIR),
        Price = 1,
        #region streamer facing
        IsDurationEditable = true,
        //ScaleFactor = 0.5f,
        //ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
    };

    /// <summary>
    /// large crosshair scale
    /// Affects crosshair size
    /// </summary>
    public static readonly Effect crosshair_giant = new("Giant Crosshair", "crosshair_giant")
    {
        Description = "Make my crosshair gigantic.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping(C_CROSSHAIR),
        Price = 7,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
    };

    /// <summary>
    /// animate dot crosshair scale until it fills up most of the screen.
    /// Affects crosshair shape and scale.
    /// </summary>
    public static readonly Effect crosshair_cataracts = new("Cataracts", "crosshair_cataracts")
    {
        Description = "My vision is gradually obscured through advancing cataracts.",
        Duration = TimeSpan.FromSeconds(40),
        Category = new EffectGrouping(C_CROSSHAIR, C_CAMERA),
        Price = 30,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };
    /// <summary>
    /// animate dot crosshair scale until it fills up most of the screen.
    /// Affects crosshair shape and scale.
    /// </summary>
    public static readonly Effect cataracts_challenge = new("3 Kill Challenge", "crosshair_cataracts_challenge_3k")
    {
        Note = "Cataracts",
        SortName = "Challenge: 3 Kill Cataracts",
        Description = "My vision is gradually obscured through advancing cataracts until I get 3 kills.",
        Duration = TimeSpan.FromMinutes(10),
        Category = new EffectGrouping(C_CROSSHAIR, C_CAMERA, C_CHALLENGES),
        Price = 100,
        #region streamer facing
        IsDurationEditable = false,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };
    #endregion Crosshair

    #region Game Play
    ///// <summary>
    ///// Every the user gets a kill, immediately triggers a taunt.
    ///// </summary>
    //public static readonly Effect taunt_after_kill = new("Taunt after every kill", "taunt_after_kill")
    //{
    //    Description = "Forced to act like a complete jerk to everybody I kill.",
    //    Duration = TimeSpan.FromSeconds(120), // long duration in case they're not good at getting kills
    //    Category = new EffectGrouping(C_GAMEPLAY),
    //    Price = 50, // it'll probably get you killed
    //    #region streamer facing
    //    IsDurationEditable = true,
    //    //ScaleFactor = 0.5f,
    //    //ScaleDecayTime = TimeSpan.FromMinutes(1),
    //    //ViewerCooldown = TimeSpan.FromMinutes(0),
    //    SessionCooldown = TimeSpan.FromMinutes(2),
    //    //bool Inactive;
    //    #endregion streamer facing
    //    Group = new EffectGrouping(G_ALIVE),
    //    Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    //};

    public static readonly Effect explode = new("Explode", "explode")
    {
        SortName = "Die: Explode",
        Description = "Instant and dramatic death.",
        Category = new EffectGrouping(C_GAMEPLAY),
        Price = 60,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };

    public static readonly Effect kill = new("Die", "kill")
    {
        SortName = "Die: Die",
        Description = "Instant death.",
        Category = new EffectGrouping(C_GAMEPLAY),
        Price = 60,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };

    private static readonly string CAUTION = "\nTAKE CARE that you know when this actually will do something.";

    public static readonly Effect destroybuildings = new("Destroy All My Buildings", "destroybuildings")
    {
        Description = "Instantly destroy all of Engy's buildings. " + CAUTION,
        Category = new EffectGrouping(C_GAMEPLAY, "Engineer"),
        Price = 60, // sort of equivalent to dying
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE, G_ENGY),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };
    public static readonly Effect destroysentry = new("Destroy My Sentry", "destroysentry")
    {
        Description = "Instantly destroy Engy's sentry. " + CAUTION,
        Category = new EffectGrouping(C_GAMEPLAY, "Engineer"),
        Price = 35,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE, G_ENGY),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };
    public static readonly Effect destroydispenser = new("Destroy My Dispenser", "destroydispenser")
    {
        Description = "Instantly destroy Engy's dispenser. " + CAUTION,
        Category = new EffectGrouping(C_GAMEPLAY, "Engineer"),
        Price = 15,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE, G_ENGY),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };
    public static readonly Effect destroyteleporters = new("Destroy My Teleporters", "destroyteleporters")
    {
        Description = "Instantly destroy both of Engy's teleporters. " + CAUTION,
        Category = new EffectGrouping(C_GAMEPLAY, "Engineer"),
        Price = 25,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        ViewerCooldown = TimeSpan.FromMinutes(2),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE, G_ENGY),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };

    public static readonly Effect removedisguise = new("Remove My Disguise", "removedisguise")
    {
        Description = "Undisguise Spy. " + CAUTION,
        Category = new EffectGrouping(C_GAMEPLAY, "Spy"),
        Price = 10,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(3),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE, G_SPY),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };

    public static readonly Effect ubernow = new("Use My Über Now", "ubernow")
    {
        Description = "Right-click Medi Gun. " + CAUTION,
        Category = new EffectGrouping(C_GAMEPLAY, "Medic"),
        Price = 30,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        SessionCooldown = TimeSpan.FromMinutes(1),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE, G_MEDIC),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.SlightlyHarmful),
    };

    public static readonly Effect medicradar = new("Medic Radar", "medicradar")
    {
        Description = "All teammates call medic to show icons over their heads.",
        Duration = TimeSpan.FromSeconds(2),
        Category = new EffectGrouping(C_GAMEPLAY, "Medic"),
        Price = 1,
        #region streamer facing
        IsDurationEditable = false,
        //ScaleFactor = 0.5f,
        //ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE, G_MEDIC),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.SlightlyHelpful),
    };

    public static readonly Effect melee_only = new("Melee Only", "melee_only")
    {
        Description = "Force slot 3 (melee) weapon.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping(C_GAMEPLAY),
        Price = 50, // it'll probably get you killed
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(10),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };
    public static readonly Effect melee_only_challenge = new("3 Kill Challenge", "melee_only_challenge_3k")
    {
        Note = "Melee Only",
        SortName = "Challenge: 3 Kill Melee Only",
        Description = "Forced to use slot 3 (melee) weapon until I get 3 kills.",
        Duration = TimeSpan.FromMinutes(10),
        Category = new EffectGrouping(C_GAMEPLAY, C_CHALLENGES),
        Price = 100, // it'll probably get you killed
        #region streamer facing
        IsDurationEditable = false,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };

    //public static readonly Effect taunt_after_crit_kill = new("Taunt after every Crit Kill", "taunt_after_crit_kill")
    //{
    //    Description = "Forced to act like a jerk to players I kill with crits (including headshots).",
    //    Duration = TimeSpan.FromSeconds(120), // long duration in case they're not good at getting kills
    //    Category = new EffectGrouping(C_GAMEPLAY),
    //    Price = 30, // it'll probably get you killed... if it triggers.
    //    #region streamer facing
    //    IsDurationEditable = true,
    //    //ScaleFactor = 0.5f,
    //    //ScaleDecayTime = TimeSpan.FromMinutes(1),
    //    //ViewerCooldown = TimeSpan.FromMinutes(0),
    //    SessionCooldown = TimeSpan.FromMinutes(2),
    //    //bool Inactive;
    //    #endregion streamer facing
    //    Group = new EffectGrouping(G_ALIVE),
    //    Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    //};
    public static readonly Effect taunt_after_single_kill = new("Taunt after next Kill", "taunt_after_kill_challenge_1k")
    {
        Note = "until 1 kill",
        Description = "Forced to act like a jerk to the next player I kill.",
        Duration = TimeSpan.FromMinutes(10), // long duration is cancelled after 1 kill
        Category = new EffectGrouping(C_GAMEPLAY),
        Price = 10,
        #region streamer facing
        IsDurationEditable = false,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.SlightlyHarmful),
    };
    public static readonly Effect taunt_after_single_crit_kill = new("Taunt after next Crit Kill", "taunt_after_crit_kill_challenge_1k")
    {
        Note = "until 1 kill",
        Description = "Forced to act like a jerk to the next player I kill with a crit (including headshots).",
        Duration = TimeSpan.FromMinutes(10), // long duration is cancelled after 1 kill
        Category = new EffectGrouping(C_GAMEPLAY),
        Price = 5,
        #region streamer facing
        IsDurationEditable = false,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.SlightlyHarmful),
    };
    #endregion Game Play

    #region HUD and Movement
    public static readonly Effect hide_hud = new("Hide HUD", "hide_hud")
    {
        Description = "Hides the Heads-up Display (HUD) until I check the scoreboard or otherwise reload it",
        //Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping(C_HUD),
        Price = 3,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(2),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_MAP),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Neutral),
    };
    public static readonly Effect show_score = new("Show Scoreboard", "show_score")
    {
        Description = "Shows the scoreboard for a few seconds",
        Duration = TimeSpan.FromSeconds(6),
        Category = new EffectGrouping(C_HUD),
        Price = 1,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(1),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(0),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_MAP),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Neutral),
    };
    public static readonly Effect mouse_sensitivity_high = new("High Sensitivity", "mouse_sensitivity_high")
    {
        SortName = "Mouse: High Sensitivity",
        Description = "My teammates will start looking at me funny.",
        Duration = TimeSpan.FromSeconds(45),
        Category = new EffectGrouping(C_MOVEMENT),
        Price = 50, // really annoying
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_APP),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };
    public static readonly Effect mouse_sensitivity_low = new("Low Sensitivity", "mouse_sensitivity_low")
    {
        SortName = "Mouse: Low Sensitivity",
        Description = "I need a bigger mousepad for this.",
        Duration = TimeSpan.FromSeconds(45),
        Category = new EffectGrouping(C_MOVEMENT),
        Price = 50, // really annoying
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_APP),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };
    public static readonly Effect retry = new("Zero-out My Score", "retry")
    {
        Description = "Reload the ongoing match, zeroing out my score - also preventing any autobalance in progress. ",
        Category = new EffectGrouping(C_HUD, C_GAMEPLAY),
        Price = 100, // worse than getting killed
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(10),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_MAP),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };
    public static readonly Effect spin_left = new("Spinnnnnn", "spin_left")
    {
        SortName = "Mouse: Spin Left",
        Description = "I swear I'm not a spinbot.",
        Duration = TimeSpan.FromSeconds(30),
        Category = new EffectGrouping(C_MOVEMENT, C_GAMEPLAY),
        Price = 40, // might get us kicked
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        //Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };
    public static readonly Effect wm1 = new("W+M1", "wm1")
    {
        Description = "Best strategy in the game.",
        Duration = TimeSpan.FromSeconds(60),
        Category = new EffectGrouping(C_MOVEMENT, C_GAMEPLAY),
        Price = 50,
        #region streamer facing
        IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(5),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Neutral),
    };
    #endregion HUD and Movement

    public static readonly Effect quit = new("'Quit Smoking'", "quit")
    {
        Description = "This is what happens if you type 'quit smoking' in the console. ",
        Category = new EffectGrouping(C_GAMEPLAY),
        Price = 2000,
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 0.5f,
        ScaleDecayTime = TimeSpan.FromMinutes(10),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        //SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_APP),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };

    public static readonly Effect join_class_autokill = new("Death by Class Change", "join_class_autokill")
    {
        Description = "Change to this class NOW, kills me if I'm out of spawn.",
        Parameters = new ParameterList(new[] {
            new ParameterDef(name:"Class", id:"class",
                new Parameter("Scout", "scout"),
                new Parameter("Soldier", "soldier"),
                new Parameter("Pyro", "pyro"),
                new Parameter("Demo", "demoman"),
                new Parameter("Heavy", "heavyweapons"),
                new Parameter("Engy", "engineer"),
                new Parameter("Medic", "medic"),
                new Parameter("Sniper", "sniper"),
                new Parameter("Spy", "spy"),
                new Parameter("?Random?", "random")
                )
        }),
        Category = new EffectGrouping(C_GAMEPLAY),
        Price = 75, // worse than getting killed
        #region streamer facing
        //IsDurationEditable = true,
        ScaleFactor = 1.0f,
        ScaleDecayTime = TimeSpan.FromMinutes(10),
        //ViewerCooldown = TimeSpan.FromMinutes(0),
        SessionCooldown = TimeSpan.FromMinutes(2),
        //bool Inactive;
        #endregion streamer facing
        Group = new EffectGrouping(G_ALIVE),
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };

    public override EffectList Effects
        => new Effect[]{
            spin_left,
            wm1,
            mouse_sensitivity_high,
            mouse_sensitivity_low,

            quit,
            retry,

            join_class_autokill,

            blackandwhite,
            pixelated,
            dream,
            //wallhacks_grass,

            big_guns,
            small_guns,
            no_guns,
            long_arms,
            vr_mode,

            hide_hud,
            show_score,

            crosshair_rainbow,
            crosshair_giant,
            crosshair_cataracts,

            kill,
            explode,
            melee_only,
            //taunt_after_kill,
            //taunt_after_crit_kill,
            destroybuildings,
            destroysentry,
            destroydispenser,
            destroyteleporters,
            removedisguise,
            medicradar,
            ubernow,

            cataracts_challenge,
            melee_only_challenge,
            taunt_after_single_kill,
            taunt_after_single_crit_kill,
            blackandwhite_challenge_5ks,
        };

    public override Game Game { get; } = new(
        name: "Team Fortress 2",
        id: nameof(TF2Spectator), // must match with class name and package name.
        path: "PC",
        connector: ConnectorType.SimpleTCPServerConnector
        );

    //protected override bool IsReady(EffectRequest? request)

    //value in setting this?
    //protected override string ProcessName => "tf2";

    public override string Host => CROWD_CONTROL_HOST;

    public override ushort Port => APP_CROWD_CONTROL_PORT;

}