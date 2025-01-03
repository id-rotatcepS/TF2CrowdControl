using ASPEN;

namespace Effects.TF2
{
    public class TF2Config
    {
        public int ConfigVersion = 0;
        public string TF2Path;
        public ushort RCONPort;
        public string RCONPassword;

        internal const string DefaultTF2Path = @"C:\Program Files (x86)\Steam\steamapps\common\Team Fortress 2";
        public void UpgradeVersionWithDefaults()
        {
            if (ConfigVersion < 1)
            {
                ConfigVersion = 1;
                TF2Path = DefaultTF2Path;
                RCONPort = 48000;
                RCONPassword = "test";
            }

            // additional version defaults start here
        }
    }


    #region base classes

    public class TF2Effects
    {
        private static TF2FrameworkInterface.TF2Instance? _tf2Instance;
        public static TF2FrameworkInterface.TF2Instance? TF2Instance
        {
            get => _tf2Instance;
            set
            {
                _tf2Instance = value;

                if (TF2Proxy != null)
                    (TF2Proxy as TF2Poller).Dispose();

                if (_tf2Instance == null)
                    TF2Proxy = null;
                else
                    TF2Proxy = NewTF2Poller();
            }
        }

        public static TF2Config TF2Config => Aspen.Option.Get<TF2Config>(nameof(TF2Config));

        private static TF2Proxy NewTF2Poller()
        {
            TF2Poller tf2 = new TF2Poller(_tf2Instance, TF2Config.TF2Path);

            //// subtle indicator of "app thinks you're dead/alive"
            ////TODO delete this or get a better indicator.
            //string DeadCommand =
            //    "cl_hud_playerclass_use_playermodel 0";
            //string AliveCommand =
            //    "cl_hud_playerclass_use_playermodel 1";
            //tf2.OnUserDied += () => _tf2Instance?.SendCommand(new TF2FrameworkInterface.StringCommand(
            //    DeadCommand), (r) => { });
            //tf2.OnUserSpawned += () => _tf2Instance?.SendCommand(new TF2FrameworkInterface.StringCommand(
            //    AliveCommand), (r) => { });

            return tf2;
        }

        public static TF2Proxy? TF2Proxy { get; private set; }

        public static readonly string MUTEX_VIEWMODEL = "viewmodel";
        public static readonly string MUTEX_WEAPONSLOT = "weaponslot";
        public static readonly string MUTEX_CROSSHAIR_COLOR = "crosshair_color";
        public static readonly string MUTEX_CROSSHAIR_SIZE = "crosshair_size";
        public static readonly string MUTEX_CROSSHAIR_SHAPE = "crosshair_shape";

        // override ParameterTypes if you want to use Parameters

        ///// <summary>
        ///// TF2Effect.TF2Instance must be non-null, and any Availability must pass.
        ///// </summary>
        ///// <returns></returns>
        //public override bool IsReady()
        //{
        //    return base.IsReady() && TF2Instance != null && IsAvailable();
        //}

        public string RunCommand(string command)
        {
            if (TF2Instance == null)
            {
                return "";
                //TODO do something.
            }
            string result = string.Empty;

            Aspen.Log.Info($"Run> {command}");

            TF2Instance.SendCommand(new TF2FrameworkInterface.StringCommand(command),
                (r) => result = r
                ).Wait();
            return result;
        }
        public void SetInfo(string variable, string value)
        {
            _ = RunCommand("setinfo " + variable + " " + value);
        }
        public void SetValue(string variable, string value)
        {
            _ = RunCommand(variable + " " + value);
        }
        public string GetValue(string variable)
        {
            return TF2Proxy?.GetValue(variable);
            ////TODO this is wrong (wait... why? I think it's correct.  Returned value is cleaned up by underlying library)
            //return RunCommand(variable);
        }
    }

    abstract public class TimedEffect : PausableEffect
    {
        public TimedEffect(string id, TimeSpan span) : base(id,
            defaultDuration: span)
        {
        }

        protected TF2Effects tf2 = new TF2Effects();

        public TF2Availability? Availability { get; set; }

        public string Command { get; }


        //public override bool IsSelectableGameState => throw new NotImplementedException();

        //public override bool IsListableGameMode => base.IsListableGameMode;

        //protected override bool CanElapse => base.CanElapse;

        public abstract void StartEffect();
        public abstract void StopEffect();

        protected override void StartEffect(EffectDispatchRequest request)
        {
            StartEffect();
        }

        override protected bool CanElapse => IsAvailable;
        protected bool IsAvailable => Availability?.IsAvailable(TF2Effects.TF2Proxy) ?? true;

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            // does nothing for this type of effect - we just start and stop.
        }

        protected override void StopEffect(TimeSpan timeSinceLastUpdate)
        {
            StopEffect();
        }
    }

    // self-disables (false IsSelectableGameState) if all values are already set.
    public class TimedSetEffect : TimedEffect
    {
        public TimedSetEffect(string id, TimeSpan span, Dictionary<string, string> variableSettings)
            : base(id, span)
        {
            VariableSettings = variableSettings;
            // "register" all these values so they store a decent value by the time this starts.
            foreach (string variable in VariableSettings.Keys)
                _ = tf2.GetValue(variable);

        }
        public TimedSetEffect(string id, TimeSpan span, string variable, string activeValue)
            : this(id, span, new Dictionary<string, string> { [variable] = activeValue })
        {
        }

        public Dictionary<string, string>? OriginalValues { get; private set; }
        public Dictionary<string, string> VariableSettings { get; }

        private bool isStarted = false;

        public override bool IsSelectableGameState
            => IsAvailable
            && IsOriginalValuesDifferentFromEffectValues();
        ///// <summary>
        ///// Original Values must not already match effect active Values
        ///// </summary>
        ///// <returns></returns>
        //public override bool IsReady()
        //{
        //    return base.IsReady() && IsOriginalValuesDifferentFromEffectValues();
        //}

        private bool IsOriginalValuesDifferentFromEffectValues()
        {
            try
            {
                if (!isStarted)
                    LoadOriginalValues();

                return OriginalValues != null && OriginalValues.Count > 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private void LoadOriginalValues()
        {
            OriginalValues = null;

            foreach (string variable in VariableSettings.Keys)
            {
                string activeValue = VariableSettings[variable];

                string startValue = tf2.GetValue(variable);
                if (startValue != activeValue)
                {
                    OriginalValues ??= new Dictionary<string, string>();
                    OriginalValues[variable] = startValue;
                }
            }
        }

        public override void StartEffect()
        {
            isStarted = true;
            LoadOriginalValues();
            foreach (string variable in VariableSettings.Keys)
            {
                string activeValue = VariableSettings[variable];

                tf2.SetValue(variable, activeValue);
            }
        }

        public override void StopEffect()
        {
            if (OriginalValues != null)
                foreach (string variable in OriginalValues.Keys)
                    tf2.SetValue(variable, OriginalValues[variable]);
            //TODO else

            isStarted = false;
        }
    }

    //TODO TimedCommandEffect

    public class SingleCommandEffect : InstantEffect
    {
        public SingleCommandEffect(string id, string command) : base(id)
        {
            Command = command;
        }

        private TF2Effects tf2 = new TF2Effects();
        public TF2Availability? Availability { get; set; }

        public string Command { get; }


        public override bool IsSelectableGameState => Availability?.IsAvailable(TF2Effects.TF2Proxy) ?? true;

        //public override bool IsListableGameMode => base.IsListableGameMode;

        //protected override bool CanElapse => base.CanElapse;

        protected override void StartEffect(EffectDispatchRequest request)
        {
            //base.StartEffect(request);

            _ = tf2.RunCommand(Command);
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

    // always enabled, regardless of current setting
    public class TimedToggleEffect : TimedEffect
    {
        public TimedToggleEffect(string id, TimeSpan span, string variable, string value1, string value2)
            : base(id, span)
        {
            Variable = variable;
            Value1 = value1;
            Value2 = value2;
        }
        public TimedToggleEffect(string id, TimeSpan span, string variable)
            : base(id, span)
        {
            Variable = variable;
            Value1 = "0";
            Value2 = "1";
        }

        public string Variable { get; }
        public string Value1 { get; }
        public string Value2 { get; }

        /// <summary>
        /// Always selectable (in a map) because you can always toggle.
        /// </summary>
        public override bool IsSelectableGameState
            => true
            && IsAvailable;

        public override void StartEffect()
        {
            _ = tf2.RunCommand(GetToggleCommand());
        }

        private string GetToggleCommand()
        {
            return string.Format("toggle {0} {1} {2}", Variable, Value1, Value2);
        }

        public override void StopEffect()
        {
            _ = tf2.RunCommand(GetToggleCommand());
        }
    }

    #endregion base classes

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

    public class MedicRadarEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "medicradar";
        public MedicRadarEffect()
            : base(EFFECT_ID, new TimeSpan(0, 0, seconds: 2), new()
            {
                ["hud_medicautocallers"] = "1",
                ["Hud_MedicAutocallersThreshold"] = "150",
            })
        {
            Availability = new AliveClass("medic");
        }
    }

    public class BlackAndWhiteTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "blackandwhite";
        public BlackAndWhiteTimedEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, "mat_color_projection", "4")
        {
            // Availability: even works in the menu
            Availability = new InApplication();
        }
    }
    public class PixelatedTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "pixelated";
        public PixelatedTimedEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, "mat_viewportscale", "0.1")
        {
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }
    public class DreamTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "dream";
        //dream requires an additional setting mat_force_bloom 1
        public DreamTimedEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, new()
            {
                ["mat_bloom_scalefactor_scalar"] = "50",
                ["mat_force_bloom"] = "1",
            })
        {
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }

    public class BigGunsTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "big_guns";
        public BigGunsTimedEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, new()
            {
                ["tf_use_min_viewmodels"] = "0",
                ["r_drawviewmodel"] = "1",
            })
        {
            Mutex.Add(TF2Effects.MUTEX_VIEWMODEL);
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }
    public class SmallGunsTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "small_guns";
        public SmallGunsTimedEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, new()
            {
                ["tf_use_min_viewmodels"] = "1",
                ["r_drawviewmodel"] = "1",
            })
        {
            Mutex.Add(TF2Effects.MUTEX_VIEWMODEL);
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }

        public override bool IsSelectableGameState
        {
            get
            {
                var x = base.OriginalValues;
                var y = base.IsSelectableGameState;
                if (y)
                    return y;
                else
                    return y;
            }
        }
    }
    public class NoGunsToggleEffect : TimedToggleEffect
    {
        public static readonly string EFFECT_ID = "no_guns";
        public NoGunsToggleEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, "r_drawviewmodel")
        {
            Mutex.Add(TF2Effects.MUTEX_VIEWMODEL);
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }
    public class LongArmsTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "long_arms";
        public LongArmsTimedEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, new()
            {
                ["viewmodel_fov"] = "160",
                ["r_drawviewmodel"] = "1",
            })
        {
            Mutex.Add(TF2Effects.MUTEX_VIEWMODEL);
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }

    public class VRModeTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "vr_mode";
        public VRModeTimedEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, new()
            {
                ["cl_first_person_uses_world_model"] = "1",
                ["tf_taunt_first_person"] = "1",
            })
        {
            Mutex.Add(TF2Effects.MUTEX_VIEWMODEL);
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }

    public class RainbowCrosshairEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "crosshair_rainbow";

        public RainbowCrosshairEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan)
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == tf2.GetValue("crosshair");

        public override void StartEffect()
        {
            // violet (but in range of the updates)
            _ = tf2.RunCommand("cl_crosshair_blue 255; cl_crosshair_green 50; cl_crosshair_red 143;");
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
            _ = tf2.RunCommand(
                $"incrementvar cl_crosshair_red 50 255 {redincrement};" +
                $"incrementvar cl_crosshair_green 50 255 {grnincrement};" +
                $"incrementvar cl_crosshair_blue 50 255 {bluincrement};");
        }

        public override void StopEffect()
        {
            //reset to default
            _ = tf2.RunCommand("cl_crosshair_blue 200;cl_crosshair_green 200;cl_crosshair_red 200;");
        }
    }
    public class CataractsCrosshairEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "crosshair_cataracts";

        private string crosshair;
        public CataractsCrosshairEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan)
        {
            //Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE);
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Availability = new AliveInMap();

            crosshair = "\"\"";
        }
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == tf2.GetValue("crosshair");

        public override void StartEffect()
        {
            crosshair = tf2.GetValue("cl_crosshair_file");
            if (string.IsNullOrWhiteSpace(crosshair))
                crosshair = "\"\"";

            // "dot" crosshair, will grow
            _ = tf2.RunCommand(
                "cl_crosshair_file crosshair5;" +
                "cl_crosshair_scale 32");
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            // reaches max in 30 seconds, and just stays there until effect ends.
            TimeSpan growtime = TimeSpan.FromSeconds(30);
            double percent;
            if (this.Elapsed >= growtime)
                percent = 1.0;
            else
                percent = this.Elapsed.TotalMilliseconds / growtime.TotalMilliseconds;

            int scale = (int)(3000 * percent) + 32;

            _ = tf2.RunCommand("cl_crosshair_scale " + scale);
        }

        public override void StopEffect()
        {
            //reset to default
            _ = tf2.RunCommand("cl_crosshair_blue 200;cl_crosshair_green 200;cl_crosshair_red 200;" +
                "cl_crosshair_scale 32;" +
                "cl_crosshair_file " + crosshair);
        }
    }
    public class GiantCrosshairEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_giant";

        public GiantCrosshairEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan, new()
            {
                ["cl_crosshair_scale"] = "1000"
            })
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == tf2.GetValue("crosshair");
    }

    public class TauntAfterKillEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "taunt_after_kill";

        public TauntAfterKillEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan)
        {
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable
            && null != TF2Effects.TF2Proxy;

        public override void StartEffect()
        {
            if (TF2Effects.TF2Proxy != null)
                TF2Effects.TF2Proxy.OnUserKill += TauntAfterKillEffect_OnUserKill;
        }

        private void TauntAfterKillEffect_OnUserKill(string victim, string weapon, bool crit)
        {
            _ = tf2.RunCommand("taunt");
            //TODO tempting to add "say ha ha I killed you, {victim}"
        }

        public override void StopEffect()
        {
            if (TF2Effects.TF2Proxy != null)
                TF2Effects.TF2Proxy.OnUserKill -= TauntAfterKillEffect_OnUserKill;
        }
    }

    public class MeleeOnlyEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "melee_only";

        public MeleeOnlyEffect()
            : base(EFFECT_ID, PausableEffect.DefaultTimeSpan)
        {
            Mutex.Add(TF2Effects.MUTEX_WEAPONSLOT);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable;

        public override void StartEffect()
        {
            _ = tf2.RunCommand("slot3");
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            _ = tf2.RunCommand("slot3");
        }

        public override void StopEffect()
        {
            //switch to primary
            _ = tf2.RunCommand("slot1");
        }
    }
}