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

        private object originalValuesLock = new object();

        protected Dictionary<string, string>? OriginalValues { get; private set; }
        protected Dictionary<string, string> VariableSettings { get; }

        private bool isStarted = false;

        public override bool IsSelectableGameState
            => IsAvailable
            && IsOriginalValuesDifferentFromEffectValues();

        private bool IsOriginalValuesDifferentFromEffectValues()
        {
            try
            {
                lock (originalValuesLock)
                {
                    if (!isStarted)
                        LoadOriginalValues();

                    return (OriginalValues?.Count ?? 0) > 0;
                }
            }
            catch (Exception)
            {
                return true;
            }
        }

        private void LoadOriginalValues()
        {
            lock (originalValuesLock)
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
        }

        public override void StartEffect()
        {
            lock (originalValuesLock)
            {
                isStarted = true;
                LoadOriginalValues();
            }
            foreach (string variable in VariableSettings.Keys)
            {
                string activeValue = VariableSettings[variable];

                TF2Effects.Instance.SetRequiredValue(variable, activeValue);
            }
        }

        public override void StopEffect()
        {
            lock (originalValuesLock)
            {
                // some effects change the initial values further during updates,
                // so we restore originals or requested values that didn't differ from originals
                foreach (string variable in VariableSettings.Keys)
                {
                    string restoreValue;
                    if (OriginalValues?.ContainsKey(variable) ?? false)
                        restoreValue = OriginalValues[variable];
                    else
                        restoreValue = VariableSettings[variable];

                    TF2Effects.Instance.SetValue(variable, restoreValue);
                }

                isStarted = false;
            }
        }
    }

    public class MedicRadarEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "medicradar";
        public MedicRadarEffect()
            : base(EFFECT_ID, new TimeSpan(0, 0, seconds: 6), new()
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

    /// <summary>
    /// Black & White, No Sound, and random Intertitles on death.
    /// </summary>
    public class SilentMovieTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "silent_movie";

        public SilentMovieTimedEffect()
            : this(EFFECT_ID, DefaultTimeSpan)
        {
            _ = TF2Effects.Instance.GetValue("volume"); // register for polling
        }

        protected SilentMovieTimedEffect(string id, TimeSpan duration)
            : base(id, duration, new Dictionary<string, string>
            {
                ["mat_color_projection"] = "4", // black & white
                ["volume"] = "0", // mute
                ["mat_bloom_scalefactor_scalar"] = "8", // much more subtle than Dream
                ["mat_force_bloom"] = "1", // "required" for bloom - except setting it to 0 doesn't force it off
            })
        {
            Mutex.Add(nameof(BlackAndWhiteTimedEffect));//hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_BLOOM);
            // Availability: even works in the menu
            Availability = new InApplication();
        }
        public override void StartEffect()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("TF2 Connection invalid for effect");
            base.StartEffect();

            TF2Effects.Instance.TF2Proxy.OnUserDied += ShowRandomIntertitle;
        }

        private void ShowRandomIntertitle()
        {
            string intertitle = FormatRandomIntertitle();

            _ = TF2Effects.Instance.RunCommand(string.Format(
                "showinfo {0} \"{1}\" \"{2}\"",
                0, // type (not sure of the values, but 3 doesn't show the title)
                intertitle, // title
                string.Empty // message - known bug in 2013 source makes this arg never work.
                ));
        }

        private static string FormatRandomIntertitle()
        {
            string format = intertitles[Random.Shared.Next(intertitles.Count)];
            return string.Format(format, TF2Effects.Instance.GetValue("name"));
        }
        /// <summary>
        /// string formats to display as intertitles on death.
        /// Width limited.  "rotatcepS ⚙ became an Insurance age" is an example cutoff (it adds ... afterwards)
        /// Can't contain quotes, and semicolon is probably not safe either.
        /// arg 0: username.
        /// </summary>
        private static List<string> intertitles = new List<string>()
        {
            "      T h e   E n d",
            "            'Gasp!'",
            "        (Censored.)",
            //"/Fin/",
            "    INTERMISSION...",
            "The End...Or Is It?",
            "TO BE CONTINUED...",
            "Act III: Death of {0}",
            //"{0} was killed by a drunk driver in December 1964.",
            //"{0} was reported missing in action inear An Loc in December 1965.",
            //"{0} became an Insurance agent in Modesto, California.",
            //"{0} became a writer and is living in Canada.",
        };

        public override void StopEffect()
        {
            if (TF2Effects.Instance.TF2Proxy != null)
                TF2Effects.Instance.TF2Proxy.OnUserDied -= ShowRandomIntertitle;
            base.StopEffect();
        }
    }

    public class PixelatedTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "pixelated";
        public PixelatedTimedEffect()
            : base(EFFECT_ID, TimeSpan.FromSeconds(30), "mat_viewportscale", "0.1")
        {
            Mutex.Add(nameof(PixelatedTimedEffect)); //hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_VIEWPORT);
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
            Mutex.Add(nameof(DreamTimedEffect)); //hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_BLOOM);
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

    public class BrrrCrosshairEffect : CrosshairShapeTimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_brrr";

        public BrrrCrosshairEffect()
            : base(EFFECT_ID, TimeSpan.FromSeconds(40), new()
            {
                ["cl_crosshair_file"] = "brrr"
            })
        {
            //Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR); // can't color the broken image
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => base.IsSelectableGameState
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");
    }

    public abstract class MouseSensitivityEffect : TimedEffect
    {
        private const string variable = "sensitivity";
        private int originalSensitivity;
        protected MouseSensitivityEffect(string id)
            : base(id, TimeSpan.FromSeconds(45))
        {
            Mutex.Add(TF2Effects.MUTEX_MOUSE);
            Availability = new AliveInMap();
            // "register" values so they store a decent value by the time this starts.
            // ... except the proxy probably isn't ready by the time this is constructed - so we do it once during IsSelectableGameState
            _ = TF2Effects.Instance.GetValue(variable);
        }
        public override bool IsSelectableGameState => IsAvailable && IsValueRegistered();

        private bool registered = false;
        private bool IsValueRegistered()
        {
            if (registered) return true;
            _ = TF2Effects.Instance.GetValue(variable);
            registered = true;
            return true;
        }

        private double startValue;
        private void LoadOriginalValues()
        {
            startValue = GetDoubleOr(TF2Effects.Instance.GetValue(variable),
                def: 3);
        }

        private double GetDoubleOr(string? value, double def)
        {
            if (value == null)
                return def;

            double result;
            if (double.TryParse(value, out result))
                return result;
            return def;
        }
        abstract protected double Factor { get; }

        public override void StartEffect()
        {
            LoadOriginalValues();

            double newval = Factor * startValue;
            TF2Effects.Instance.SetRequiredValue(variable, newval.ToString());
        }

        public override void StopEffect()
        {
            TF2Effects.Instance.SetValue(variable, startValue.ToString());
        }
    }
    public class MouseSensitivityHighEffect : MouseSensitivityEffect
    {
        public static readonly string EFFECT_ID = "mouse_sensitivity_high";
        public MouseSensitivityHighEffect()
            : base(EFFECT_ID)
        {
        }

        protected override double Factor => 5.0;
    }
    public class MouseSensitivityLowEffect : MouseSensitivityEffect
    {
        public static readonly string EFFECT_ID = "mouse_sensitivity_low";

        public MouseSensitivityLowEffect()
            : base(EFFECT_ID)
        {
        }

        protected override double Factor => .25;
    }

    // unreliable, not currently available.
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

    /// <summary>
    /// Animate fov and viewmodel fov back and forth at different rates
    /// </summary>
    public class DrunkEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "drunk";

        private byte fov;
        private bool fov_up;
        private byte vm;
        private bool vm_up;

        public DrunkEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 1, 0), new()
            {
                ["fov_desired"] = "75",//fov_desired 20 to 90 (def 75) // in practice only 75-90 are visible changes - probably due to default_fov 75 (cheats required)
                ["viewmodel_fov"] = "54",//viewmodel_fov .1 to 179.9 (def 54)
                ["r_drawviewmodel"] = "1"
            })
        {
            fov = 75;
            fov_up = false;
            vm = 54;
            vm_up = true;

            Mutex.Add(TF2Effects.MUTEX_VIEWMODEL);
            Mutex.Add(TF2Effects.MUTEX_FOV);
            Availability = new AliveInMap();
        }

        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable;

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            calculateFOV(timeSinceLastUpdate);

            calculateVM(timeSinceLastUpdate);

            _ = TF2Effects.Instance.RunCommand(
                $"fov_desired {fov};" +
                $"viewmodel_fov {vm};"
                );
        }

        private void calculateFOV(TimeSpan timeSinceLastUpdate)
        {
            // seconds per transition (current updates run every quarter second)
            double transitionLengthFOV = 2.0;
            // in practice only 75-90 are visible changes - probably due to default_fov 75 (cheats required)
            byte maxFOV = 90;
            byte minFOV = 75;// 20; 
            byte rangeFOV = (byte)(maxFOV - minFOV);
            byte incrementFOV = (byte)Math.Min(rangeFOV,
                (timeSinceLastUpdate.TotalSeconds / transitionLengthFOV) * rangeFOV);

            if (fov_up)
            {
                fov = (byte)Math.Min(maxFOV, fov + incrementFOV);
                if (fov == maxFOV)
                    fov_up = false;
            }
            else
            {
                fov = (byte)Math.Max(minFOV, fov - incrementFOV);
                if (fov == minFOV)
                    fov_up = true;
            }
        }

        private void calculateVM(TimeSpan timeSinceLastUpdate)
        {
            // seconds per transition (current updates run every quarter second)
            double transitionLengthVM = 3.5;
            // not using full range, we want to feel drunk, not insane.
            byte maxVM = 100;// 179;
            byte minVM = 40;// 1;
            byte rangeVM = (byte)(maxVM - minVM);
            byte incrementVM = (byte)Math.Min(rangeVM,
                (timeSinceLastUpdate.TotalSeconds / transitionLengthVM) * rangeVM);

            if (vm_up)
            {
                vm = (byte)Math.Min(maxVM, vm + incrementVM);
                if (vm == maxVM)
                    vm_up = false;
            }
            else
            {
                vm = (byte)Math.Max(minVM, vm - incrementVM);
                if (vm == minVM)
                    vm_up = true;
            }
        }
    }

    public class RainbowCrosshairEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_rainbow";
        private enum ColorTransition { PurpleToRed, RedToYellow, YellowToGreen, GreenToBlue, BlueToPurple }
        private ColorTransition transition;
        private byte r, g, b;

        public RainbowCrosshairEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 2, 0), new()
            {
                // purple
                ["cl_crosshair_blue"] = "255",
                ["cl_crosshair_green"] = "0",
                ["cl_crosshair_red"] = "255"
            })
        {
            transition = ColorTransition.PurpleToRed;
            r = 255;
            g = 0;
            b = 255;
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Availability = new AliveInMap();
        }
        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            // 1.5 seconds per transition (current updates run every quarter second)
            double transitionLength = 1.5;
            byte increment = (byte)Math.Min(255,
                (timeSinceLastUpdate.TotalSeconds / transitionLength) * 255);

            switch (transition)
            {
                case ColorTransition.PurpleToRed:
                    b = DecrementTowardsTransition(b, increment,
                        ColorTransition.RedToYellow);
                    break;
                case ColorTransition.RedToYellow:
                    g = IncrementTowardsTransition(g, increment,
                        ColorTransition.YellowToGreen);
                    break;
                case ColorTransition.YellowToGreen:
                    r = DecrementTowardsTransition(r, increment,
                        ColorTransition.GreenToBlue);
                    break;
                case ColorTransition.GreenToBlue:
                    g = DecrementColor(g, increment);
                    b = IncrementTowardsTransition(b, increment,
                        ColorTransition.BlueToPurple);
                    break;
                case ColorTransition.BlueToPurple:
                    r = IncrementTowardsTransition(r, increment,
                        ColorTransition.PurpleToRed);
                    break;
            }
            _ = TF2Effects.Instance.RunCommand(
                $"cl_crosshair_red {r};" +
                $"cl_crosshair_green {g};" +
                $"cl_crosshair_blue {b};");

        }

        private byte IncrementTowardsTransition(byte g, byte incrementFactor, ColorTransition transitionAtEndOfInc)
        {
            byte colorPart = IncrementColor(g, incrementFactor);
            if (colorPart == 255) transition = transitionAtEndOfInc;
            return colorPart;
        }

        private static byte IncrementColor(byte g, byte incrementFactor)
        {
            return (byte)Math.Min(g + incrementFactor, 255);
        }

        private byte DecrementTowardsTransition(byte b, byte incrementFactor, ColorTransition transitionAtEndOfDec)
        {
            byte colorPart = DecrementColor(b, incrementFactor);
            if (colorPart == 0) transition = transitionAtEndOfDec;
            return colorPart;
        }

        private static byte DecrementColor(byte b, byte incrementFactor)
        {
            return (byte)Math.Max(b - incrementFactor, 0);
        }
    }

    public class CataractsCrosshairEffect : CrosshairShapeTimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_cataracts";

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
            //Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Availability = new AliveInMap();
        }
        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        public override void StartEffect()
        {
            base.StartEffect();

            // "dot" crosshair, will grow
            TF2Effects.Instance.SetRequiredValue("cl_crosshair_file", "crosshair5");
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
    }

    /// <summary>
    /// special handling to restore crosshair shape at the end of the effect.
    /// Also registers mutex with shape and provides a list of available shapes.
    /// </summary>
    public abstract class CrosshairShapeTimedSetEffect : TimedSetEffect
    {
        protected static readonly string CROSSHAIR_DEFAULT = "\"\"";
        protected List<string> non_default_crosshairs = new()
        {
            "crosshair1",
            "crosshair2",
            "crosshair3",
            "crosshair4",
            "crosshair5",
            "crosshair6",
            "crosshair7",
            "default",
        };
        private string crosshair;
        protected CrosshairShapeTimedSetEffect(string id, TimeSpan span, Dictionary<string, string> variables)
            : base(id, span, variables)
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);

            crosshair = CROSSHAIR_DEFAULT;
        }
        public override void StartEffect()
        {
            // custom restore code because blank crosshair file might not restore right.
            crosshair = TF2Effects.Instance.GetValue("cl_crosshair_file")
                ?? CROSSHAIR_DEFAULT;
            if (string.IsNullOrWhiteSpace(crosshair)
                || IsInvalidCrosshair(crosshair))
                crosshair = CROSSHAIR_DEFAULT;

            base.StartEffect();

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
        public override void StopEffect()
        {
            base.StopEffect();

            //reset to default
            TF2Effects.Instance.SetValue("cl_crosshair_file", crosshair);
        }
    }

    public class AlienCrosshairEffect : CrosshairShapeTimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_alien";
        public AlienCrosshairEffect()
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }
        protected AlienCrosshairEffect(string id, TimeSpan span)
            : base(id, span, new()
            {
                // none
            })
        {
            Mutex.Add(nameof(AlienCrosshairEffect));
            //Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Availability = new AliveInMap();
        }
        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        public override void StartEffect()
        {
            base.StartEffect();

            TF2Effects.Instance.SetRequiredValue("cl_crosshair_file", randomCrosshair());
        }

        private string randomCrosshair()
        {
            int i = Random.Shared.Next(non_default_crosshairs.Count);
            return non_default_crosshairs[i];
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            TF2Effects.Instance.SetValue("cl_crosshair_file", randomCrosshair());
        }

        public override void StopEffect()
        {
            base.StopEffect();
        }
    }
}