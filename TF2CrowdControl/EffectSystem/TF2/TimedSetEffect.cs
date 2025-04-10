namespace EffectSystem.TF2
{
    /// <summary>
    /// self-disables (false IsSelectableGameState) if all values are already set.
    /// </summary>
    public class TimedSetEffect : TimedEffect
    {
        public TimedSetEffect(string id, TimeSpan span, Dictionary<string, string> variableSettings)
            : base(id, span)
        {
            VariableSettings = variableSettings;
            // "register" all these values so they store a decent value by the time this starts.
            foreach (string variable in VariableSettings.Keys)
                _ = TF2Effects.Instance.GetValue(variable);

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

                string? startValue = TF2Effects.Instance.GetValue(variable);
                if (startValue != null && startValue != activeValue)
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

                TF2Effects.Instance.SetRequiredValue(variable, activeValue);
            }
        }

        public override void StopEffect()
        {
            if (OriginalValues != null)
                foreach (string variable in OriginalValues.Keys)
                    TF2Effects.Instance.SetValue(variable, OriginalValues[variable]);
            //TODO else

            isStarted = false;
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
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }

        protected BlackAndWhiteTimedEffect(string id, TimeSpan duration)
            : base(id, duration, "mat_color_projection", "4")
        {
            Mutex.Add(nameof(BlackAndWhiteTimedEffect));//hierarchy is all mutex
            // Availability: even works in the menu
            Availability = new InApplication();
        }
    }

    public class PixelatedTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "pixelated";
        public PixelatedTimedEffect()
            : base(EFFECT_ID, DefaultTimeSpan, "mat_viewportscale", "0.1")
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
            : base(EFFECT_ID, DefaultTimeSpan, new()
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
            : base(EFFECT_ID, DefaultTimeSpan, new()
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
            : base(EFFECT_ID, DefaultTimeSpan, new()
            {
                ["tf_use_min_viewmodels"] = "1",
                ["r_drawviewmodel"] = "1",
            })
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
            : base(EFFECT_ID, DefaultTimeSpan, new()
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
            : base(EFFECT_ID, DefaultTimeSpan, new()
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

    public class GiantCrosshairEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_giant";

        public GiantCrosshairEffect()
            : base(EFFECT_ID, DefaultTimeSpan, new()
            {
                ["cl_crosshair_scale"] = "1000"
            })
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => base.IsSelectableGameState
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");
    }

    public class MouseSensitivityHighEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "mouse_sensitivity_high";

        public MouseSensitivityHighEffect()
            : base(EFFECT_ID, DefaultTimeSpan, new()
            {
                ["sensitivity"] = "20" // default 3
            })
        {
            Mutex.Add(TF2Effects.MUTEX_MOUSE);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => base.IsSelectableGameState;
    }
    public class MouseSensitivityLowEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "mouse_sensitivity_low";

        public MouseSensitivityLowEffect()
            : base(EFFECT_ID, DefaultTimeSpan, new()
            {
                ["sensitivity"] = "1.0" // default 3
            })
        {
            Mutex.Add(TF2Effects.MUTEX_MOUSE);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => base.IsSelectableGameState;
    }

    public class WallhacksForGrassEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "wallhacks_grass";

        public WallhacksForGrassEffect()
            : base(EFFECT_ID, DefaultTimeSpan, new()
            {
                ["r_drawdetailprops"] = "2"
            })
        {
            //Mutex.Add(TF2Effects.MUTEX_DRAW_DETAIL_PROPS);
            Availability = new InMap();
        }
        public override bool IsSelectableGameState => base.IsSelectableGameState;
    }

    public class RainbowCrosshairEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_rainbow";

        public RainbowCrosshairEffect()
            : base(EFFECT_ID, DefaultTimeSpan, new()
            {
                // violet (but in range of the updates)
                ["cl_crosshair_blue"] = "255",
                ["cl_crosshair_green"] = "50",
                ["cl_crosshair_red"] = "143"
            })
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

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
    }

    public class CataractsCrosshairEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_cataracts";
        private static readonly string CROSSHAIR_DEFAULT = "\"\"";
        private string crosshair;
        public CataractsCrosshairEffect()
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }
        protected CataractsCrosshairEffect(string id, TimeSpan span)
            : base(id, span, new()
            {
                ["cl_crosshair_scale"] = "32",
            })
        {
            Mutex.Add(nameof(CataractsCrosshairEffect));//hierarchy is all mutex
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
            // custom restore code because blank crosshair file might not restore right.
            crosshair = TF2Effects.Instance.GetValue("cl_crosshair_file")
                ?? CROSSHAIR_DEFAULT;
            if (string.IsNullOrWhiteSpace(crosshair)
                || IsInvalidCrosshair(crosshair))
                crosshair = CROSSHAIR_DEFAULT;

            base.StartEffect();

            // "dot" crosshair, will grow
            TF2Effects.Instance.SetRequiredValue("cl_crosshair_file", "crosshair5");
        }

        private bool IsInvalidCrosshair(string crosshair)
        {
            if (crosshair == null)
                return true;

            switch (crosshair)
            {
                case "crosshair1":
                case "crosshair2":
                case "crosshair3":
                case "crosshair4":
                case "crosshair5":
                case "crosshair6":
                case "crosshair7":
                case "default":
                case "\"\"":
                    return false;
                default:
                    return true;
            }
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            // reaches max in 30 seconds, and just stays there until effect ends.
            TimeSpan growtime = TimeSpan.FromSeconds(30);
            double percent;
            if (Elapsed >= growtime)
                percent = 1.0;
            else
                percent = Elapsed.TotalMilliseconds / growtime.TotalMilliseconds;

            //TODO scale the 3000 by resolution.
            int scale = (int)(3000 * percent) + 32;

            TF2Effects.Instance.SetValue("cl_crosshair_scale", scale.ToString());
            // MAYBE: ensure shape doesn't get changed
            //TF2Effects.Instance.SetValue("cl_crosshair_file", "crosshair5");
        }

        public override void StopEffect()
        {
            // "seteffect" claims to set scale to 32, but then we changes it more.  If user already had 32 it won't get reset.
            // So take us back to the "we set it to this" value before restore is attempted.
            foreach (string variable in VariableSettings.Keys)
            {
                string activeValue = VariableSettings[variable];

                TF2Effects.Instance.SetValue(variable, activeValue);
            }

            base.StopEffect();

            //reset to default
            TF2Effects.Instance.SetValue("cl_crosshair_file", crosshair);
        }
    }
}