namespace EffectSystem.TF2
{
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

            _ = TF2Effects.Instance.RunCommand(Command);
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

    public class EngineerDestroyBuildingsEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "destroybuildings";
        public EngineerDestroyBuildingsEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 2 0;wait 200;destroy 1 0;wait 200;destroy 1 1;wait 200;destroy 0 0")
        {
            Availability = new AliveClass("engineer");
        }
    }
    public class EngineerDestroyTeleportersEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "destroyteleporters";
        public EngineerDestroyTeleportersEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 1 0;wait 200;destroy 1 1")
        {
            Availability = new AliveClass("engineer");
        }
    }
    public class EngineerDestroySentryEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "destroysentry";
        public EngineerDestroySentryEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 2 0")
        {
            Availability = new AliveClass("engineer");
        }
    }
    public class EngineerDestroyDispenserEffect : SingleCommandEffect
    {
        public static readonly string EFFECT_ID = "destroydispenser";
        public EngineerDestroyDispenserEffect()
            // needs some delay to actually work.  This is dramatically slow for my computer, but other setups it might just barely work I'm guessing.
            : base(EFFECT_ID, "destroy 0 0")
        {
            Availability = new AliveClass("engineer");
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
    }
}