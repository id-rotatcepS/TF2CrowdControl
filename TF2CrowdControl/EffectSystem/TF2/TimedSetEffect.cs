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
            : base(EFFECT_ID, DefaultTimeSpan, "mat_color_projection", "4")
        {
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

        public override bool IsSelectableGameState
        {
            get
            {
                var x = OriginalValues;
                var y = base.IsSelectableGameState;
                if (y)
                    return y;
                else
                    return y;
            }
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
}