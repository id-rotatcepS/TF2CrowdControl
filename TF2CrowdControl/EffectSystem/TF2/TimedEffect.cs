namespace EffectSystem.TF2
{
    abstract public class TimedEffect : PausableEffect
    {
        public TimedEffect(string id, TimeSpan span) : base(id,
            defaultDuration: span)
        {
        }

        override protected bool CanElapse => IsAvailable;
        protected bool IsAvailable => Availability?.IsAvailable(TF2Effects.Instance.TF2Proxy) ?? true;
        public TF2Availability? Availability { get; set; }

        protected override void StartEffect(EffectDispatchRequest request)
        {
            StartEffect();
        }
        public abstract void StartEffect();

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            // does nothing for this type of effect - we just start and stop.
        }

        protected override void StopEffect(TimeSpan timeSinceLastUpdate)
        {
            StopEffect();
        }
        public abstract void StopEffect();
    }

    public class RainbowCrosshairEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "crosshair_rainbow";

        public RainbowCrosshairEffect()
            : base(EFFECT_ID, DefaultTimeSpan)
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        public override void StartEffect()
        {
            // violet (but in range of the updates)
            // (not RunRequiredCommand - updates can pause and continue from any color)
            _ = TF2Effects.Instance.RunCommand("cl_crosshair_blue 255; cl_crosshair_green 50; cl_crosshair_red 143;");
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            // factors were based on about 3 increments per second.
            /// but that seems slow, let's try 6
            double incrementFactor = timeSinceLastUpdate.TotalSeconds * 6.0;
            // make each increment multipied by the timespan for a precise rainbow speed.
            int redincrement = (int)(2 * incrementFactor);
            int grnincrement = (int)(3 * incrementFactor);
            int bluincrement = (int)(4 * incrementFactor);
            _ = TF2Effects.Instance.RunCommand(
                $"incrementvar cl_crosshair_red 50 255 {redincrement};" +
                $"incrementvar cl_crosshair_green 50 255 {grnincrement};" +
                $"incrementvar cl_crosshair_blue 50 255 {bluincrement};");
        }

        public override void StopEffect()
        {
            //reset to default
            _ = TF2Effects.Instance.RunCommand("cl_crosshair_blue 200;cl_crosshair_green 200;cl_crosshair_red 200;");
        }
    }

    public class CataractsCrosshairEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "crosshair_cataracts";
        private static readonly string CROSSHAIR_DEFAULT = "\"\"";
        private string crosshair;
        public CataractsCrosshairEffect()
            : base(EFFECT_ID, DefaultTimeSpan)
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE);
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Availability = new AliveInMap();

            crosshair = CROSSHAIR_DEFAULT;
        }
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        public override void StartEffect()
        {
            crosshair = TF2Effects.Instance.GetValue("cl_crosshair_file")
                ?? CROSSHAIR_DEFAULT;
            if (string.IsNullOrWhiteSpace(crosshair))
                crosshair = CROSSHAIR_DEFAULT;

            // "dot" crosshair, will grow
            _ = TF2Effects.Instance.RunRequiredCommand(
                "cl_crosshair_file crosshair5;" +
                "cl_crosshair_scale 32");
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            // reaches max in 30 seconds, and just stays there until effect ends.
            TimeSpan growtime = TimeSpan.FromSeconds(30);
            double percent;
            if (Elapsed >= growtime)
                percent = 1.0;
            else
                percent = Elapsed.TotalMilliseconds / growtime.TotalMilliseconds;

            int scale = (int)(3000 * percent) + 32;

            _ = TF2Effects.Instance.RunCommand("cl_crosshair_scale " + scale);
        }

        public override void StopEffect()
        {
            //reset to default
            _ = TF2Effects.Instance.RunCommand("cl_crosshair_blue 200;cl_crosshair_green 200;cl_crosshair_red 200;" +
                "cl_crosshair_scale 32;" +
                "cl_crosshair_file " + crosshair);
        }
    }

    public class TauntAfterKillEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "taunt_after_kill";

        public TauntAfterKillEffect()
            : this(EFFECT_ID)
        {
        }
        protected TauntAfterKillEffect(string id)
            : base(id, DefaultTimeSpan)
        {
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable
            && null != TF2Effects.Instance.TF2Proxy;

        public override void StartEffect()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            TF2Effects.Instance.TF2Proxy.OnUserKill += TauntAfterKillEffect_OnUserKill;
        }

        private void TauntAfterKillEffect_OnUserKill(string victim, string weapon, bool crit)
        {
            if (ShouldTaunt(victim, weapon, crit))
                _ = TF2Effects.Instance.RunCommand("taunt");
            //FUTURE tempting to add "say ha ha I killed you, {victim}"
        }

        virtual protected bool ShouldTaunt(string victim, string weapon, bool crit)
        {
            return true;
        }

        public override void StopEffect()
        {
            if (TF2Effects.Instance.TF2Proxy != null)
                TF2Effects.Instance.TF2Proxy.OnUserKill -= TauntAfterKillEffect_OnUserKill;
        }
    }

    public class TauntAfterCritKillEffect : TauntAfterKillEffect
    {
        new public static readonly string EFFECT_ID = "taunt_after_crit_kill";

        public TauntAfterCritKillEffect()
            : base(EFFECT_ID)
        {
            Availability = new AliveInMap();
        }

        protected override bool ShouldTaunt(string victim, string weapon, bool crit)
        {
            return crit;
        }
    }

    public class MeleeOnlyEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "melee_only";

        public MeleeOnlyEffect()
            : base(EFFECT_ID, DefaultTimeSpan)
        {
            Mutex.Add(TF2Effects.MUTEX_WEAPONSLOT);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable;

        public override void StartEffect()
        {
            // (not RunRequiredCommand - updates can pause and continue same command)
            _ = TF2Effects.Instance.RunCommand("slot3");
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            _ = TF2Effects.Instance.RunCommand("slot3");
        }

        public override void StopEffect()
        {
            //switch to primary
            _ = TF2Effects.Instance.RunCommand("slot1");
        }
    }

    public class ShowScoreboardEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "show_score";
        public ShowScoreboardEffect()
            : base(EFFECT_ID, TimeSpan.FromSeconds(6))
        {
            Availability = new InMap();
            Mutex.Add(TF2Effects.MUTEX_SCOREBOARD);
        }

        public override bool IsSelectableGameState => IsAvailable;

        public override void StartEffect()
        {
            _ = TF2Effects.Instance.RunRequiredCommand("+showscores");
        }

        public override void StopEffect()
        {
            _ = TF2Effects.Instance.RunCommand("-showscores");
        }
    }

    public class SpinEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "spin_left";

        public SpinEffect()
            : base(EFFECT_ID, DefaultTimeSpan)
        {
            Mutex.Add(TF2Effects.MUTEX_FORCE_MOVE);
            Availability = new AliveInMap();
        }

        public override bool IsSelectableGameState => IsAvailable;

        public override void StartEffect()
        {
            _ = TF2Effects.Instance.RunRequiredCommand("+left");
        }

        public override void StopEffect()
        {
            _ = TF2Effects.Instance.RunCommand("-left");
        }
    }

    /// <summary>
    /// Oddly enough, pressing W or M1 does not cancel this out.
    /// </summary>
    public class WM1Effect : TimedEffect
    {
        public static readonly string EFFECT_ID = "wm1";

        public WM1Effect()
            : base(EFFECT_ID, DefaultTimeSpan)
        {
            Mutex.Add(TF2Effects.MUTEX_FORCE_MOVE);
            Availability = new AliveInMap();
        }

        public override bool IsSelectableGameState => IsAvailable;

        public override void StartEffect()
        {
            _ = TF2Effects.Instance.RunRequiredCommand("+forward;+attack");
        }

        public override void StopEffect()
        {
            _ = TF2Effects.Instance.RunCommand("-attack;-forward");
        }
    }

}