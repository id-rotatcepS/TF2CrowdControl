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
            //base.StartEffect(request);

            _ = TF2Effects.Instance.RunRequiredCommand(Command);

            Thread.Sleep(WaitTimeToVerify);
            CheckEffectWorked();
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
            : base(EFFECT_ID, "say_party {0} in stream says: '{1}'")
        {
            Availability = new InApplication();
        }

        protected override void StartEffect(EffectDispatchRequest request)
        {
            //base.StartEffect(request);
            string formattedCommand = string.Format(Command, request.Requestor, request.Parameter);
            _ = TF2Effects.Instance.RunRequiredCommand(formattedCommand);

            // no verification.
        }
    }

    /// <summary>
    /// shows the scoreboard and reloads the hud and doesn't release the scoreboard thus hiding everything until the next time you hit tab.
    /// </summary>
    public class HideHUDEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "hide_hud";
        public HideHUDEffect()
            : base(EFFECT_ID, "+showscores; hud_reloadscheme;")//internet used +score, which is also a valid command - I assume they're the same thing.
        {
            Availability = new InMap();
            Mutex.Add(TF2Effects.MUTEX_SCOREBOARD);
            // and it hides the crosshair, too.
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE);
        }

        protected override void CheckEffectWorked()
        {
            // can't really be verified.
            if (!Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Left map as effect started.");
        }
    }

    public class QuitEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "quit";
        public QuitEffect()
            : base(EFFECT_ID, "quit")
        {
            Availability = new InApplication();
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

    public class ForcedChangeClassEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "join_class_autokill";
        public ForcedChangeClassEffect()
            : this(EFFECT_ID)
        {
        }
        protected ForcedChangeClassEffect(string id)
            : base(id, "join_class {1}")
        {
            Availability = new InMap();
        }

        protected virtual string Autokill => "1";

        // we should be able to verify within 60s that the class forcibly changed.
        protected override TimeSpan WaitTimeToVerify => TimeSpan.FromSeconds(60);

        protected string classSelection = string.Empty;
        protected override void StartEffect(EffectDispatchRequest request)
        {
            // not using base - request parameter is part of the command, also smart required variable.

            classSelection = request.Parameter.ToLower(); // join_class supports "random" directly
            string formattedMainCommand = string.Format(Command, request.Requestor, classSelection);
            string formattedCommand = AddTempVariableChange(formattedMainCommand,
                variable: "hud_classautokill", val: Autokill,
                def: "0");
            _ = TF2Effects.Instance.RunRequiredCommand(formattedCommand);

            CheckEffectWorked();
        }

        private string AddTempVariableChange(string mainCommand, string variable, string val, string def)
        {
            string prev = TF2Effects.Instance.GetValue(variable)
                ?? def;
            if (prev == def)
                return mainCommand;
            // set value; run command; restore value.
            return string.Format("{0} {1};{2};{0} {3}", variable, val, mainCommand, prev);
        }

        protected override void CheckEffectWorked()
        {
            // availability doesn't change, but if it became unavailable it probably won't take.
            if (Availability != null
                && !Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
                throw new EffectNotVerifiedException("Left the map before class applied");

            // we should be able to verify within 60s that the class forcibly changed.
            if (!string.IsNullOrEmpty(classSelection)
                && classSelection != "random"
                && TF2Effects.Instance.TF2Proxy?.ClassSelection == classSelection)
                throw new EffectNotVerifiedException("Class doesn't appear to have been applied");
        }

    }
    ///// <summary>
    ///// This version can't positively verify itself - the user might not respawn before CC verification timeout.
    ///// ... even worse, in cases where we must restore the autokill value to 1 IMMEDIATELY invokes the autokill.
    ///// Could only be an option for users that have autokill set to 0.
    ///// </summary>
    //public class ChangeClassEffect : ForcedChangeClassEffect
    //{
    //    new public static readonly string EFFECT_ID = "join_class";
    //    public ChangeClassEffect()
    //        : base(EFFECT_ID)
    //    {
    //        //TODO add "autokill set to 0" to availability.
    //    }

    //    protected override string Autokill => "0";

    //    protected override void CheckEffectWorked()
    //    {
    //        // availability doesn't change, but if it became unavailable it probably won't take.
    //        if (Availability != null
    //            && !Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
    //            throw new EffectNotVerifiedException("Left the map before class applied");
    //        // but we can't guarantee a POSITIVE verification within the CC timeout - the user might not die.
    //        //    if (TF2Effects.Instance.TF2Proxy.ClassSelection == classSelection)
    //    }
    //}

}