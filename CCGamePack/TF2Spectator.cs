//using JetBrains.Annotations;

using ConnectorLib.SimpleTCP;

using CrowdControl.Common;

namespace CrowdControl.Games.Packs.TF2Spectator;

//[UsedImplicitly]
public class TF2Spectator : SimpleTCPPack<SimpleTCPServerConnector> // example was SimpleWebsocketServerConnector
{
    //TODO need some education here.
    public TF2Spectator(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler)
        : base(player, responseHandler, statusUpdateHandler) { }

    public static readonly string CROWD_CONTROL_HOST = "127.0.0.1";//TODO share with my SimpleTCPClient instance
    public static readonly ushort APP_CROWD_CONTROL_PORT = 58430;//TODO share with my SimpleTCPClient instance

    //new("Give Lives", "lives") { Quantity = 9 },

    /// <summary>
    /// mat_color_projection 4 (vs 0)
    /// Affects entire game including menus
    /// </summary>
    public static readonly Effect blackandwhite = new("Black & White", "blackandwhite")
    {
        //string ID (constructor);
        //string Name (constructor);
        //string? SortName = "Black and White",

        //QuantityRange Quantity = 1;
        //uint DefaultQuantity;

        //SITimeSpan? Duration;
        Duration = TimeSpan.FromSeconds(60),
        //bool IsDurationEditable = true;
        //? IsDurationEditable = true,
        IsDurationEditable = true,

        //string? Description;
        Description = "TF2 in the 50s",
        //string? Note;
        //string? StartMessage;
        //string? EndMessage;

        // user-facing collections (can be used to disable/hide a set with an Effect Report)
        //EffectGrouping? Category;
        //        Category = new EffectGrouping("cat1", "cat2"),

        // internal collections (can be used to disable/hide a set with an Effect Report)
        //EffectGrouping? Group;
        Group = new EffectGrouping("g1", "g2"),
        //List<string>? Tags;

        //List<string>? Metadata;

        //List<Connector>? IncompatibleConnectors;

        //string? Image;
        // = "https://resources.crowdcontrol.live/images/Minecraft/Minecraft/icons/freeze.png"

        //uint? QueueWarning;
        //uint? QueueMax;

        //SITimeSpan? ViewerCooldown;
        //SITimeSpan? SessionCooldown;

        //uint Price;
        Price = 100,

        //ItemKind Kind (constructor, Effect or Bidwar);
        //ParameterList? Parameters;

        //SITimeSpan? ResponseTimeout;
        //uint? RetryMaxAttempts;
        //SITimeSpan? RetryMaxTime;
        //NonNumericRange<SITimeSpan>? RetryInterval;

        //string? ErrorOverride;

        //bool Inactive;
        //bool Disabled;

        //Alignment? Alignment;
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),

        //bool? NoPooling;

        //EffectGrouping? ScaleGroups;

        // float ScaleFactor property
        //ScaleFactor = 2.5f,
        // SITimeSpan ScaleDecayTime property
        //ScaleDecayTime = TimeSpan.FromSeconds(1),
    };
    /// <summary>
    /// mat_viewportscale 0.1 (vs 1)
    /// Affects game generation - relative to primary resolution.
    /// </summary>
    public static readonly Effect pixelated = new("Pixelated", "pixelated")
    {
        Description = "TF2 in the 80s",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };
    /// <summary>
    /// mat_bloom_scalefactor_scalar 50 (vs 1) and mat_force_bloom 1
    /// Affects game generation.
    /// </summary>
    public static readonly Effect dream = new("Dream Mode", "dream")
    {
        Description = "TF2 in a dream",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.Harmful),
    };

    /// <summary>
    /// tf_use_min_viewmodels 0 (vs 1) and r_drawviewmodel 1
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect big_guns = new("Big Guns", "big_guns")
    {
        Description = "Force my usual weapon viewmodels to the default big ones.",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
    };
    /// <summary>
    /// tf_use_min_viewmodels 0 (vs 1) and r_drawviewmodel 1
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect small_guns = new("Small Guns", "small_guns")
    {
        Description = "Force my usual weapon viewmodels to small ones.",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
    };
    /// <summary>
    /// r_drawviewmodel 0/1 toggle
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect no_guns = new("No Guns or Show Guns", "no_guns")
    {
        Description = "Force my weapon viewmodels to hide (or show) - opposite of my usual.",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
    };
    /// <summary>
    /// viewmodel_fov 160
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect long_arms = new("Long Arms", "long_arms")
    {
        Description = "Long arms",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
    };
    /// <summary>
    /// turn on cl_first_person_uses_world_model and tf_taunt_first_person
    /// Affects game viewmodel (First-person arms/weapons)
    /// </summary>
    public static readonly Effect vr_mode = new("VR Mode", "vr_mode")
    {
        Description = "My arms, weapons, and body are visible exactly as other players see them in game.",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
    };

    /// <summary>
    /// animate a rainbow effect on the crosshair's color.
    /// Affects crosshair color
    /// </summary>
    public static readonly Effect crosshair_rainbow = new("Rainbow Crosshair", "crosshair_rainbow")
    {
        Description = "A rainbow of colors for my crosshair",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
    };

    /// <summary>
    /// animate dot crosshair scale until it fills up most of the screen.
    /// Affects crosshair shape and scale.
    /// </summary>
    public static readonly Effect crosshair_cataracts = new("Cataracts", "crosshair_cataracts")
    {
        Description = "My vision is gradually obscured through advancing cataracts",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };

    /// <summary>
    /// Every the user gets a kill, immediately triggers a taunt.
    /// </summary>
    public static readonly Effect taunt_after_kill = new("Taunt after every kill", "taunt_after_kill")
    {
        Description = "Forced to act like a complete jerk to everybody I kill",
        Duration = TimeSpan.FromSeconds(60),
        IsDurationEditable = true,
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };

    public static readonly Effect explode = new("Explode", "explode")
    {
        Description = "Instant and dramatic death",
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };

    public static readonly Effect destroybuildings = new("Destroy My Buildings", "destroybuildings")
    {
        Description = "Instantly destroy all of Engy's buildings",
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.ExtremelyHarmful),
    };

    public static readonly Effect removedisguise = new("Remove My Disguise", "removedisguise")
    {
        Description = "Undisguise Spy",
        Alignment = new Alignment(/*Orderliness.Chaotic, */Morality.VeryHarmful),
    };

    public override EffectList Effects
        => new Effect[]{
                blackandwhite,
                pixelated,
                dream,

                big_guns,
                small_guns,
                no_guns,
                long_arms,
                vr_mode,

                crosshair_rainbow,
                crosshair_cataracts,

                explode,
                destroybuildings,
                removedisguise,

                taunt_after_kill,
        };

    public override Game Game { get; } = new(
        name: "Team Fortress 2",
        id: nameof(TF2Spectator), // must match with class name and package name.
        path: "PC",
        connector: ConnectorType.SimpleTCPServerConnector
        );

    //TODO what is this supposed to mean? do I have to hook my entire app up to this for any accuracy??
    protected override bool IsReady(EffectRequest? request)
        //TODO
        => true;

    //value to setting this?
    //protected override string ProcessName => "tf2";

    public override string Host => CROWD_CONTROL_HOST;

    public override ushort Port => APP_CROWD_CONTROL_PORT;

}