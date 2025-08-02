namespace EffectSystem.TF2
{
    /// <summary>
    /// Fire-and-forget command effects.
    /// Override <see cref="CheckEffectWorked"/> if effect verification is more subtle than just !available.
    /// </summary>
    public class SingleCommandEffect : InstantEffect
    {
        public SingleCommandEffect(string id, string command) : base(id)
        {
            Command = command;
        }

        public TF2Availability? Availability { get; set; }

        public string Command { get; }


        public override bool IsSelectableGameState => Availability?.IsAvailable(TF2Effects.Instance.TF2Proxy) ?? true;

        //public override bool IsListableGameMode => base.IsListableGameMode;

        //protected override bool CanElapse => base.CanElapse;

        protected override void StartEffect(EffectDispatchRequest request)
        {
            StartEffect();

            Thread.Sleep(WaitTimeToVerify);

            CheckEffectWorked();
        }

        protected virtual void StartEffect()
        {
            _ = TF2Effects.Instance.RunRequiredCommand(Command);
        }

        protected virtual TimeSpan WaitTimeToVerify { get; set; }
            // give enough time for two polls (2 seconds, last I checked)
            = PollingCacheTF2Proxy.PollPeriod * 2;

        /// <summary>
        /// Throws an exception if the Effect can't be verified <see cref="WaitTimeToVerify"/> later.
        /// Default implementation: if Availability is still available, the effect must not have applied.
        /// </summary>
        /// <exception cref="EffectNotVerifiedException"></exception>
        protected virtual void CheckEffectWorked()
        {
            if (Availability != null
                && Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Couldn't verify that the effect started.");
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            // should never be called.  TODO that means the method shouldn't exist.
        }

        protected override void StopEffect(TimeSpan timeSinceLastUpdate)
        {
            // should never be called.  TODO that means the method shouldn't exist.
        }
    }

    public class KillEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "kill";
        public KillEffect()
            : base(EFFECT_ID, "kill")
        {
            Availability = new AliveInMap();
        }
    }

    public class ExplodeEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "explode";
        public ExplodeEffect()
            : base(EFFECT_ID, "explode")
        {
            Availability = new AliveInMap();
        }
    }

    public class EngineerDestroyBuildingsEffect : EngineerDestroyEffect
    {
        public static readonly string EFFECT_ID = "destroybuildings";
        public EngineerDestroyBuildingsEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 2 0;wait 200;destroy 1 0;wait 200;destroy 1 1;wait 200;destroy 0 0")
        {
        }
    }
    public class EngineerDestroyTeleportersEffect : EngineerDestroyEffect
    {
        public static readonly string EFFECT_ID = "destroyteleporters";
        public EngineerDestroyTeleportersEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 1 0;wait 200;destroy 1 1")
        {
        }
    }
    public class EngineerDestroySentryEffect : EngineerDestroyEffect
    {
        public static readonly string EFFECT_ID = "destroysentry";
        public EngineerDestroySentryEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 2 0")
        {
        }
    }
    public class EngineerDestroyDispenserEffect : EngineerDestroyEffect
    {
        public static readonly string EFFECT_ID = "destroydispenser";
        public EngineerDestroyDispenserEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 0 0")
        {
        }
    }

    public abstract class EngineerDestroyEffect : SingleCommandEffect
    {
        public EngineerDestroyEffect(string id, string command)
            : base(id, command)
        {
            Availability = new AliveClass("engineer");
        }

        // verification: if you died, that's not really relevant to whether the effect worked.
        // if you changed class there was no value to the effect
        private TF2Availability Worked = new ClassInMap("engineer");

        protected override void CheckEffectWorked()
        {
            if (!Worked.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Destroy building didn't work - player is no longer an engineer in game.");
        }
    }

    public class SpyRemoveDisguiseEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "removedisguise";
        public SpyRemoveDisguiseEffect()
            : base(EFFECT_ID, "disguise 8 -2")
        {
            Availability = new AliveClass("spy");
        }

        // verification: if you died, that's not really relevant to whether the effect worked.
        // if you changed class there was no value to the effect
        private TF2Availability Worked = new ClassInMap("spy");

        protected override void CheckEffectWorked()
        {
            if (!Worked.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Remove Disguise didn't work - player is no longer a spy in game.");
        }
    }

    public class MedicUberNowEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "ubernow";
        public MedicUberNowEffect()
            : base(EFFECT_ID, "slot2;+attack2;wait 40;-attack2")
        {
            Mutex.Add(TF2Effects.MUTEX_WEAPONSLOT);
            // 24 seconds for vacc uber
            // 32 seconds for kritz uber
            // 40 seconds for stock uber
            Availability = new AliveClassForMinimumTime("medic", 32);
        }

        // verification: if you died, the command either didn't work (uber) or had no real value (other ubers)
        // if you changed class there was no value to the effect
        private TF2Availability Worked = new AliveClass("medic");

        protected override void CheckEffectWorked()
        {
            if (!Worked.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Über didn't work - player is no longer an alive medic.");
        }
    }

    // FUTURE: Not currently used - CC doesn't allow raw string arguments - although we could make it work with a long list of parameters I guess?
    public class PartyChatEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "say_party";
        public PartyChatEffect()
            : base(EFFECT_ID, "tf_party_chat \"{0} in stream says: '{1}'\"")
        {
            Availability = new InApplication();
        }

        protected override void StartEffect(EffectDispatchRequest request)
        {
            //base.StartEffect(request);
            //TODO escape/replace quotes in Parameter
            string formattedCommand = string.Format(Command, request.Requestor, request.Parameter);
            _ = TF2Effects.Instance.RunRequiredCommand(formattedCommand);

            // no verification.
        }
    }

    //TODO disables weapon switching... perhaps +score prevents that? other options?  Also needs to be Duration and end itself.
    ///// <summary>
    ///// shows the scoreboard and reloads the hud and doesn't release the scoreboard thus hiding everything until the next time you hit tab.
    ///// </summary>
    //public class HideHUDEffect : SingleCommandEffect
    //{
    //    public static readonly string EFFECT_ID = "hide_hud";
    //    public HideHUDEffect()
    //        : base(EFFECT_ID, "+showscores; hud_reloadscheme;")//internet used +score, which is also a valid command - I assume they're the same thing.
    //    {
    //        Availability = new InMap();
    //        Mutex.Add(TF2Effects.MUTEX_SCOREBOARD);
    //        // and it hides the crosshair, too.
    //        Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
    //        Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
    //        Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE);
    //    }

    //    protected override void CheckEffectWorked()
    //    {
    //        // can't really be verified.
    //        if (!Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
    //            throw new EffectNotVerifiedException("Left map as effect started.");
    //    }
    //}

    public class QuitEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "quit";
        public QuitEffect()
            : base(EFFECT_ID, "quit")
        {
            Availability = new InApplication();
        }
        protected override TimeSpan WaitTimeToVerify => TimeSpan.Zero;
        protected override void CheckEffectWorked()
        {
            // tf2 (going) down, can't really count on anything to verify.
        }
    }
    public class RetryEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "retry";
        public RetryEffect()
            : base(EFFECT_ID, "retry")
        {
            Availability = new InMap();
        }
    }
    /// <summary>
    /// Fades to like 50% blue then resets. 
    /// I think it's used by the game when drowning.
    /// </summary>
    public class UnderwaterFadeEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "fade";
        public UnderwaterFadeEffect()
            : base(EFFECT_ID, "fade")
        {
            Availability = new InMap();
        }
        protected override void CheckEffectWorked()
        {
            // no verification possible or necessary
        }
    }
    public class ContrackerEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "show_quest_log";
        public ContrackerEffect()
            : base(EFFECT_ID, "show_quest_log")
        {
            Availability = new InApplication();
        }
        protected override void CheckEffectWorked()
        {
            // no verification possible or necessary
        }
    }
    public class PopupUIEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "popup_ui";
        public PopupUIEffect()
            : base(EFFECT_ID, string.Empty)
        {
            Availability = new InApplication();
        }
        protected override void StartEffect()
        {
            string command = Choose(
                    //"showconsole", // anytime (kinda boring)
                    "fogui", // anytime (& doesn't go to main menu!)
                    "bug", // anytime
                    "showschemevisualizer", // anytime
                    "training_showdlg;gameui_activate", // anytime (sometimes "hidden" on main menu? so show it)
                    "itemtest", // anytime (fullscreen)
                    "itemtest_botcontrols" // anytime (& doesn't go to main menu!)
                    );
            // the minor additions here are not that interesting for the effect
            // TF2Effects.Instance.TF2Proxy?.IsUserAlive
            //        //"gameui_activate", // go to main menu - so require them to be in a map.
            //        "show_motd", // requires a map
            //        "showmapinfo" // requires a map...and alive?
            //        //,"+vgui_drawtree" // worried about not hitting - command
            //        //"opencharinfo",  // requires a map// class before loadouts
            //        //"opencharinfo_direct",  // requires a map// current loadout
            //        //"opencharinfo_backpack", // requires a map
            //        //"opencharinfo_crafting", // requires a map
            //        // the above are all common menu selections... armory is rarely clicked.
            //        //"opencharinfo_armory"  // requires a map// "mannco catalog"

            _ = TF2Effects.Instance.RunRequiredCommand(command);
        }

        private static string Choose(params string[] options)
        {
            int index = Random.Shared.Next(0, options.Length);
            return options[index];
        }

        protected override void CheckEffectWorked()
        {
            // no verification possible or necessary
        }
    }

    public class VoiceMenuEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "voicemenu";
        public VoiceMenuEffect()
            : this(EFFECT_ID)
        {
        }
        protected VoiceMenuEffect(string id)
            : base(id, "voicemenu {1}")
        {
            Availability = new AliveInMap();
            Mutex.Add(TF2Effects.MUTEX_AUDIO);
        }

        protected string selection = string.Empty;
        protected string requestor = string.Empty;
        protected override void StartEffect(EffectDispatchRequest request)
        {
            // need to pull request parameter as part of the command

            // 0: part of format, but not used currently 
            requestor = request.Requestor;
            // 1: part of format
            selection = request.Parameter.ToLower();

            base.StartEffect(request);
        }

        protected override void StartEffect()
        {
            //base.StartEffect(); // runs Command directly.
            string formattedCommand = string.Format(Command, requestor, selection);

            _ = TF2Effects.Instance.RunRequiredCommand(formattedCommand);
        }

        protected override void CheckEffectWorked()
        {
            // availability doesn't change, but if it became unavailable it probably won't take.
            if (Availability != null
                && !Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Not alive before command applied");
        }
    }

    public class ItemPreviewEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "item_preview";
        public ItemPreviewEffect()
            : this(EFFECT_ID)
        {
        }
        protected ItemPreviewEffect(string id)
            : base(id, "tf_econ_item_preview {1}")
        {
            Availability = new InApplication();
        }

        protected string selection = string.Empty;
        protected string requestor = string.Empty;
        protected override void StartEffect(EffectDispatchRequest request)
        {
            // need to pull request parameter as part of the command

            // 0: part of format, but not used currently 
            requestor = request.Requestor;
            // 1: part of format
            selection = request.Parameter;

            base.StartEffect(request);
        }

        protected override void StartEffect()
        {
            //base.StartEffect(); // runs Command directly.

            TF2Effects.Instance.Play(Choose(
                    TF2Effects.SOUND_CRATE_OPEN,
                    TF2Effects.SOUND_CRATE_RARE_MVM_OPEN));
        }

        private static T Choose<T>(params T[] options)
        {
            int index = Random.Shared.Next(0, options.Length);
            return options[index];
        }


        protected override void CheckEffectWorked()
        {
            // availability doesn't change, but if it became unavailable it probably won't take.
            if (Availability != null
                && !Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Not in application before command applied");

            string formattedCommand = string.Format(Command, requestor, selection);

            _ = TF2Effects.Instance.RunRequiredCommand(formattedCommand);
        }
    }


}