using System.Globalization; // for NumberStyles.HexNumber

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

        private Dictionary<string, string> VariableSettings { get; }

        private readonly object originalValuesLock = new object();

        private Dictionary<string, string> OriginalValues { get; set; }
            = new Dictionary<string, string>();

        private bool isStarted = false;

        /// <summary>
        /// IsAvailable and not every variableSetting is already set.  
        /// Override this if effect has more to it than just the variableSettings.
        /// </summary>
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

                    return OriginalValues.Count > 0;
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
                OriginalValues = new Dictionary<string, string>();

                foreach (string variable in VariableSettings.Keys)
                {
                    string activeValue = VariableSettings[variable];

                    string? startValue = TF2Effects.Instance.GetValue(variable);
                    if (startValue != null && startValue != activeValue)
                    {
                        OriginalValues[variable] = startValue;
                    }
                }
            }
        }

        /// <summary>
        /// Loads <see cref="OriginalValues"/>, then <see cref="SetRequiredVariableSettings"/>
        /// </summary>
        public override void StartEffect()
        {
            lock (originalValuesLock)
            {
                isStarted = true;
                LoadOriginalValues();
            }

            SetRequiredVariableSettings();
        }

        /// <summary>
        /// By default, sets every single variablesetting (unless it is empty) or throws an exception.
        /// </summary>
        protected virtual void SetRequiredVariableSettings()
        {
            foreach (string variable in VariableSettings.Keys)
            {
                string activeValue = VariableSettings[variable];
                if (!string.IsNullOrEmpty(activeValue))
                    TF2Effects.Instance.SetRequiredValue(variable, activeValue);
            }
        }

        public override void StopEffect()
        {
            lock (originalValuesLock)
            {
                SetValuesFromOriginal();

                isStarted = false;
            }
        }

        private void SetValuesFromOriginal()
        {
            // some effects change the initial values further during updates,
            // so we restore originals or requested values that didn't differ from originals
            foreach (string variable in VariableSettings.Keys)
            {
                string restoreValue;
                if (OriginalValues.ContainsKey(variable))
                    restoreValue = OriginalValues[variable];
                else
                    restoreValue = VariableSettings[variable];

                if (!string.IsNullOrEmpty(restoreValue))
                    TF2Effects.Instance.SetValue(variable, restoreValue);
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


    public class TankModeEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "tank_mode";

        public TankModeEffect()
            : this(EFFECT_ID, TimeSpan.FromSeconds(30))
        {
        }
        protected TankModeEffect(string id, TimeSpan duration)
            : base(id, duration, new()
            {
                ["lookstrafe"] = "1",
            })
        {
            Availability = new AliveInMap();
        }

        public override bool IsSelectableGameState => base.IsSelectableGameState
            && IsRightAndLeftAvailable();

        private bool IsRightAndLeftAvailable()
        {
            CommandBinding? right = GetRightCommand();
            if (right == null)
                return false;
            if (right.IsChanged)
                return false;

            CommandBinding? left = GetLeftCommand();
            if (left == null)
                return false;
            if (left.IsChanged)
                return false;

            return true;
        }

        private CommandBinding? GetRightCommand()
        {
            return TF2Effects.Instance.TF2Proxy?.GetCommandBinding("+moveright");
        }

        private CommandBinding? GetLeftCommand()
        {
            return TF2Effects.Instance.TF2Proxy?.GetCommandBinding("+moveleft");
        }

        public override void StartEffect()
        {
            base.StartEffect();

            CommandBinding right = GetRightCommand()
                ?? throw new EffectNotAppliedException("right bind not found");
            right.ChangeCommand("+right");

            CommandBinding left = GetLeftCommand()
                ?? throw new EffectNotAppliedException("left bind not found");
            left.ChangeCommand("+left");
        }

        public override void StopEffect()
        {
            CommandBinding? right = GetRightCommand();
            right?.RestoreCommand();

            CommandBinding? left = GetLeftCommand();
            left?.RestoreCommand();

            base.StopEffect();
        }
    }

    /// <summary>
    /// like Melee Only but constantly rotating weapons
    /// </summary>
    public class WeaponShuffleEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "weapon_shuffle";

        public WeaponShuffleEffect()
            : this(EFFECT_ID, TimeSpan.FromSeconds(5))
        {
        }
        protected WeaponShuffleEffect(string id, TimeSpan duration)
            : base(id, duration, new()
            {
                ["hud_fastswitch"] = "1" // required for it to actually shuffle.
            })
        {
            Mutex.Add(TF2Effects.MUTEX_WEAPONSLOT);
            Availability = new AliveInMap();
        }

        // animate this more quickly.
        public override bool IsUpdateAnimation => true;

        // don't go disabled when effects aren't set yet.
        public override bool IsSelectableGameState => IsAvailable;

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            _ = TF2Effects.Instance.RunCommand("invnext");
        }

        //StopEffect:
        // do nothing - whatever it happened to switch to last is what we stop on
        // ... because we shuffled the weapons.
    }

    /// <summary>
    /// Crazy settings on Motion Blur to create a weird effect
    /// </summary>
    public class VertigoTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "vertigo";
        public VertigoTimedEffect()
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }

        // test
        //mat_motion_blur_enabled 1; mat_motion_blur_falling_min 100; mat_motion_blur_forward_enabled 1; mat_motion_blur_percent_of_screen_max 20; mat_motion_blur_rotation_intensity 0; mat_motion_blur_strength 30;
        // reset
        //mat_motion_blur_enabled 0; mat_motion_blur_falling_min 10; mat_motion_blur_forward_enabled 0; mat_motion_blur_percent_of_screen_max 4; mat_motion_blur_rotation_intensity 1; mat_motion_blur_strength 1;

        protected VertigoTimedEffect(string id, TimeSpan duration)
            : base(id, duration, new Dictionary<string, string>
            {
                ["mat_motion_blur_enabled"] = "1", // default 0 (archived)
                ["mat_motion_blur_falling_intensity"] = "1",// (default 1)
                ["mat_motion_blur_falling_max"] = "20", // (default 20)
                ["mat_motion_blur_falling_min"] = "100", // default 10 - the inverted max/min is a key feature of this effect as I recall

                ["mat_motion_blur_forward_enabled"] = "1", // default 0
                ["mat_motion_blur_percent_of_screen_max"] = "20", // default 4 - larger percent grows the step size, sort of
                ["mat_motion_blur_rotation_intensity"] = "0", // default 1 - larger numbers cause weird flickering
                ["mat_motion_blur_strength"] = "30", // default 1 - larger strength shrinks the normal zone.
                                                     // 1000 is unplayable, 0 (even with all above) looks normal but 1 does not.
            })
        {
            Mutex.Add(TF2Effects.MUTEX_MOTION_BLUR);
            Availability = new AliveInMap();
        }
    }

    public class DeathAddsVertigoTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "death_adds_vertigo";
        public DeathAddsVertigoTimedEffect()
            : base(EFFECT_ID, TimeSpan.FromMinutes(5), new()
            {
                ["mat_motion_blur_enabled"] = "1", // default 0 (archived)
                ["mat_motion_blur_falling_intensity"] = "1",// (default 1)
                ["mat_motion_blur_falling_max"] = "20", // (default 20)
                ["mat_motion_blur_falling_min"] = "100", // default 10 - the inverted max/min is a key feature of this effect as I recall

                ["mat_motion_blur_forward_enabled"] = "1", // default 0
                ["mat_motion_blur_percent_of_screen_max"] = "20", // default 4 - larger percent grows the step size, sort of
                ["mat_motion_blur_rotation_intensity"] = "0", // default 1 - larger numbers cause weird flickering
                ["mat_motion_blur_strength"] = string.Empty, // default 1 - larger strength shrinks the normal zone.
                                                             // 1000 is unplayable, 0 (even with all above) looks normal but 1 does not.
            })
        {
            challenge = new DeathsChallenge(6);// 6th death 3x scale would put it close to 1000
            Mutex.Add(TF2Effects.MUTEX_MOTION_BLUR);
            Availability = new InMap();
        }

        // don't go disabled when effects aren't set yet.
        public override bool IsSelectableGameState => IsAvailable;

        private int blurStrength = 1;
        public override void StartEffect()
        {
            base.StartEffect();

            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            blurStrength = 1;
            UpdateBlur();
            TF2Effects.Instance.TF2Proxy.OnUserDied += OnDeath;
        }

        private void UpdateBlur()
        {
            TF2Effects.Instance.SetRequiredValue("mat_motion_blur_strength", blurStrength.ToString());
        }

        private void OnDeath()
        {
            blurStrength = blurStrength * 3;
            UpdateBlur();
        }

        public override void StopEffect()
        {
            if (TF2Effects.Instance.TF2Proxy != null)
                TF2Effects.Instance.TF2Proxy.OnUserDied -= OnDeath;
            blurStrength = 1;

            base.StopEffect();
        }
    }

    public class VertigoCreepAndRestoreEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "kill_restores_vertigo_creep";

        public VertigoCreepAndRestoreEffect()
            : base(EFFECT_ID, TimeSpan.FromMinutes(5), new()
            {
                ["mat_motion_blur_enabled"] = "1", // default 0 (archived)
                ["mat_motion_blur_falling_intensity"] = "1",// (default 1)
                ["mat_motion_blur_falling_max"] = "20", // (default 20)
                ["mat_motion_blur_falling_min"] = "100", // default 10 - the inverted max/min is a key feature of this effect as I recall

                ["mat_motion_blur_forward_enabled"] = "1", // default 0
                ["mat_motion_blur_percent_of_screen_max"] = "20", // default 4 - larger percent grows the step size, sort of
                ["mat_motion_blur_rotation_intensity"] = "0", // default 1 - larger numbers cause weird flickering
                ["mat_motion_blur_strength"] = string.Empty, // default 1 - larger strength shrinks the normal zone.
                                                             // 1000 is unplayable, 0 (even with all above) looks normal but 1 does not.
            })
        {
            Mutex.Add(TF2Effects.MUTEX_MOTION_BLUR);
            Availability = new InMap();
        }

        // don't go disabled when effects aren't set yet.
        public override bool IsSelectableGameState => IsAvailable;

        private double blurStrength = 1;
        public override void StartEffect()
        {
            base.StartEffect();

            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            blurStrength = 1;
            UpdateBlur();

            TF2Effects.Instance.TF2Proxy.OnUserKill += RestoreOnKill;
        }

        private void UpdateBlur()
        {
            int blurStrengthInt = (int)blurStrength;
            TF2Effects.Instance.SetValue("mat_motion_blur_strength", blurStrengthInt.ToString());
        }

        private void RestoreOnKill(string victim, string weapon, bool crit)
        {
            blurStrength = Math.Max(
                blurStrength / 2.0,
                1);
            UpdateBlur();
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            //double increasePerSecond = GetCreepRate();
            //double increase = increasePerSecond * timeSinceLastUpdate.TotalSeconds;
            //blurStrength += increase;

            double growthFactor = GetCreepFactor(timeSinceLastUpdate.TotalSeconds);
            blurStrength *= growthFactor;
            UpdateBlur();
        }

        private double GetCreepRate()
        {
            double timePercent = Elapsed.TotalSeconds / Duration.TotalSeconds;
            double increasePerSecond;
            if (timePercent > 0.90)
                // Panic mode: add 400 more in the last 10% of the time
                increasePerSecond = 400.0 / (.10 * Duration.TotalSeconds);
            else
                // normal mode: add 100 in the first 90% of the time.
                increasePerSecond = 100.0 / (.90 * Duration.TotalSeconds);
            return increasePerSecond;
        }

        //TODO: starts too slow IMO, and panic is too fast (should be POSSIBLE to keep a decent view!)

        // precalc
        private double logMaxValueNormal = Math.Log(500);
        private double logMaxValuePanic = Math.Log(500);
        private double GetCreepFactor(double fractionalSecondsPassed)
        {
            double timePercent = Elapsed.TotalSeconds / Duration.TotalSeconds;
            double rate;
            if (timePercent > 0.90)
                // Panic mode: add 400 more in the last 10% of the time
                rate = logMaxValuePanic / (.10 * Duration.TotalSeconds);
            else
                // normal mode: add 100 in the first 90% of the time.
                rate = logMaxValueNormal / (.90 * Duration.TotalSeconds);

            double growthFactor = Math.Exp(rate * fractionalSecondsPassed);
            return growthFactor;
        }

        public override void StopEffect()
        {
            blurStrength = 1;

            base.StopEffect();
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
            Mutex.Add(TF2Effects.MUTEX_AUDIO);
        }

        protected SilentMovieTimedEffect(string id, TimeSpan duration)
            : base(id, duration, new Dictionary<string, string>
            {
                ["mat_color_projection"] = "4", // black & white
                ["volume"] = "0", // mute

                // bloom - note, not setting all the other stuff from Dream Mode... if this subtle bloom doesn't work, no big deal.
                ["mat_bloom_scalefactor_scalar"] = "8", // much more subtle than Dream
                // HDR disabled maps use this value.
                ["mat_non_hdr_bloom_scalefactor"] = "8",
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
            return string.Format(format,
                TF2Effects.Instance.GetValue("name"),
                TF2Effects.Instance.TF2Proxy?.Map ?? "this map");
        }
        /// <summary>
        /// string formats to display as intertitles on death.
        /// Width limited.  "rotatcepS ⚙ became an Insurance age" is an example cutoff (it adds ... afterwards)
        /// Can't contain quotes, and semicolon is probably not safe either.
        /// arg 0: username.
        /// arg 1: map name (e.g. pl_upward - default: "this map")
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
            "A Fine Day on {1}",
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
                // HDR disabled maps use this value.
                ["mat_non_hdr_bloom_scalefactor"] = "40",
                // HDR enabled maps use this value.
                ["mat_bloom_scalefactor_scalar"] = "40",

                // ensure none of these settings are changed - they could cancel out or reduce the bloom
                // 1 disables bloom, duh.
                ["mat_disable_bloom"] = "0",
                // 0 disables bloom, 1 is LDR+bloom, 2 is HDR
                ["mat_hdr_level"] = "2",
                // still not sure what this does, but let's set it in case it prevents success.
                ["mat_bloomscale"] = "1",
                // tint color and exponent affect scale as well - unlikely somebody set them but they could disable the effect, too.
                ["r_bloomtintexponent"] = "2.2",
                ["r_bloomtintr"] = ".3",
                ["r_bloomtintg"] = ".59",
                ["r_bloomtintb"] = ".11",

                // turns out this is a cheat, and we don't need it anyhow.
                //["mat_force_bloom"] = "1",
            })
        {
            Mutex.Add(nameof(DreamTimedEffect)); //hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_BLOOM);
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }

    public class DeathAddsDreamTimedEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "death_adds_dream";
        public DeathAddsDreamTimedEffect()
            : base(EFFECT_ID, TimeSpan.FromMinutes(5), new()
            {
                // HDR enabled maps use this value.
                ["mat_bloom_scalefactor_scalar"] = string.Empty,
                // HDR disabled maps use this value.
                ["mat_non_hdr_bloom_scalefactor"] = string.Empty,

                // ensure none of these settings are changed - they could cancel out or reduce the bloom
                // 1 disables bloom, duh.
                ["mat_disable_bloom"] = "0",
                // 0 disables bloom, 1 is LDR+bloom, 2 is HDR
                ["mat_hdr_level"] = "2",
                // still not sure what this does, but let's set it in case it prevents success.
                ["mat_bloomscale"] = "1",
                // tint color and exponent affect scale as well - unlikely somebody set them but they could disable the effect, too.
                ["r_bloomtintexponent"] = "2.2",
                ["r_bloomtintr"] = ".3",
                ["r_bloomtintg"] = ".59",
                ["r_bloomtintb"] = ".11",

                // turns out this is a cheat, and we don't need it anyhow.
                //["mat_force_bloom"] = "1",
            })
        {
            challenge = new DeathsChallenge(6);// 6th death 2.25x scale would put it well past 100
            Mutex.Add(nameof(DreamTimedEffect)); //hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_BLOOM);
            Availability = new InMap();
        }

        // don't go disabled when effects aren't set yet.
        public override bool IsSelectableGameState => IsAvailable;

        private double bloomFactor = 1.0;
        public override void StartEffect()
        {
            base.StartEffect();

            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            bloomFactor = 1.0;
            UpdateBloom();
            TF2Effects.Instance.TF2Proxy.OnUserDied += OnDeath;
        }

        private void UpdateBloom()
        {
            TF2Effects.Instance.SetRequiredValue("mat_bloom_scalefactor_scalar", bloomFactor.ToString());
            TF2Effects.Instance.SetRequiredValue("mat_non_hdr_bloom_scalefactor", bloomFactor.ToString());
        }

        private void OnDeath()
        {
            bloomFactor *= 2.25;
            UpdateBloom();
        }

        public override void StopEffect()
        {
            if (TF2Effects.Instance.TF2Proxy != null)
                TF2Effects.Instance.TF2Proxy.OnUserDied -= OnDeath;
            bloomFactor = 1.0;

            base.StopEffect();
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
            //base: Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR); // can't color the broken image
            Availability = new AliveInMap();
        }
        public override bool IsSelectableGameState => base.IsSelectableGameState
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");
    }

    // Could do this as a TimedToggleEffect,
    // but if they have crosshair off they probably have a HUD replacing it, turning it on will probably not work well.
    public class NoCrosshairEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_none";

        public NoCrosshairEffect()
            : base(EFFECT_ID, TimeSpan.FromSeconds(40), new()
            {
                ["crosshair"] = "0"
                // related: "tf_hud_no_crosshair_on_scope_zoom" defaults to 0
            })
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE); // can't shape the missing crosshair
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR); // can't color the missing crosshair
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE); // can't size the missing crosshair
            Availability = new AliveInMap();
        }
    }

    public abstract class MouseSensitivityEffect : TimedEffect
    {
        private const string variable = "sensitivity";
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

        protected override double Factor => 4.5;
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

    public class HackerHUDEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "hacker_hud";

        public HackerHUDEffect()
            : base(EFFECT_ID, DefaultTimeSpan, new()
            {
                ["mat_drawTitleSafe"] = "1", // frame rectangles
                ["cl_showfps"] = "1", // green text top right corner
                ["vprof_graph"] = "1",
                ["snd_showmixer"] = "1", // flickery side text and green/yellow/red sound graph
                ["cl_showpos"] = "1", // position and velocity in white top right corner
                //;(cl_showbattery 1 overlaps - could alternate between this and showpos?) // usually "Battery: On AC" top right corner
                ["r_drawdetailprops"] = "2", // bonus that sometimes doesn't work - wallhacks for grass
            })
        {
            Availability = new InMap();
        }
        public override void StartEffect()
        {
            base.StartEffect();
            _ = TF2Effects.Instance.RunCommand("+graph");
        }
        public override void StopEffect()
        {
            _ = TF2Effects.Instance.RunCommand("-graph");
            base.StopEffect();
        }
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

        // animate this more quickly.
        public override bool IsUpdateAnimation => true;

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
            // rounds down to 0 sometimes, always do SOMETHING
            // FUTURE change both to use double?
            if (incrementFOV == 0) incrementFOV = 1;

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
            // rounds down to 0 sometimes, always do SOMETHING
            if (incrementVM == 0) incrementVM = 1;

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

    public class RainbowCombatTextEffect : RainbowTimedSetEffect
    {
        public static readonly string EFFECT_ID = "combattext_rainbow";
        public RainbowCombatTextEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 4, 0), new()
            {
                // technically don't need to watch these fields at all as I don't verify them.
                ["hud_combattext_blue"] = string.Empty,
                ["hud_combattext_green"] = string.Empty,
                ["hud_combattext_red"] = string.Empty
            })
        {
            Availability = new AliveInMap();
        }
        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("hud_combattext");

        override protected void ApplyColor()
        {
            //if "hud_combattext" 1; "hud_combattext_blue" = "1.000000"  "hud_combattext_red" = "255.000000" "hud_combattext_green" = "1.000000"
            _ = TF2Effects.Instance.RunCommand(
                $"hud_combattext_red {r};" +
                $"hud_combattext_green {g};" +
                $"hud_combattext_blue {b};");
        }
    }

    public class RainbowCrosshairEffect : RainbowTimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_rainbow";
        public RainbowCrosshairEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 2, 0), new()
            {
                // technically don't need to watch these fields at all as I don't verify them.
                ["cl_crosshair_blue"] = string.Empty,
                ["cl_crosshair_green"] = string.Empty,
                ["cl_crosshair_red"] = string.Empty
            })
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Availability = new AliveInMap();
        }

        // animate this more quickly.
        public override bool IsUpdateAnimation => true;

        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        override protected void ApplyColor()
        {
            _ = TF2Effects.Instance.RunCommand(
                $"cl_crosshair_red {r};" +
                $"cl_crosshair_green {g};" +
                $"cl_crosshair_blue {b};");
        }
    }

    public class CrosshairColorEffect : TimedSetEffect
    {
        public static readonly string EFFECT_ID = "crosshair_color";
        public CrosshairColorEffect()
            : base(EFFECT_ID, TimeSpan.FromMinutes(1), new()
            {
                ["cl_crosshair_blue"] = string.Empty,
                ["cl_crosshair_green"] = string.Empty,
                ["cl_crosshair_red"] = string.Empty
            })
        {
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_COLOR);
            Availability = new AliveInMap();
        }

        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        private string hexColor = string.Empty;
        protected override void StartEffect(EffectDispatchRequest request)
        {
            // need to pull request parameter as part of the command

            // color in #xxxxxx format
            hexColor = request.Parameter.ToLower();

            base.StartEffect(request);
        }

        public override void StartEffect()
        {
            base.StartEffect();

            (byte r, byte g, byte b) = ParseHexRGB(hexColor);

            _ = TF2Effects.Instance.RunRequiredCommand(
                $"cl_crosshair_red {r};" +
                $"cl_crosshair_green {g};" +
                $"cl_crosshair_blue {b};");
        }

        private static (byte r, byte g, byte b) ParseHexRGB(string hexColor)
        {
            // Remove '#' if present
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);

            // Ensure valid length for RGB
            if (hexColor.Length != 6)
                throw new ArgumentException("Hex color string must be 6 characters long for RGB.");

            // Parse individual color components
            byte r = ParseHexPair(hexColor, startIndex: 0);
            byte g = ParseHexPair(hexColor, startIndex: 2);
            byte b = ParseHexPair(hexColor, startIndex: 4);

            return (r, g, b);
        }

        private static byte ParseHexPair(string hexString, int startIndex)
        {
            if (hexString.Length < (startIndex + 2))
                throw new ArgumentException("Hex string must have 2 characters per byte.");
            return byte.Parse(hexString.Substring(startIndex, length: 2), NumberStyles.HexNumber);
        }
    }

    public abstract class RainbowTimedSetEffect : TimedSetEffect
    {
        private enum ColorTransition { PurpleToRed, RedToYellow, YellowToGreen, GreenToBlue, BlueToPurple }
        private ColorTransition transition;
        protected byte r, g, b;

        public RainbowTimedSetEffect(string id, TimeSpan span, Dictionary<string, string> variableSettings)
            : base(id, span, variableSettings)
        {
            transition = ColorTransition.PurpleToRed;
            // purple (magenta)
            r = 255;
            g = 0;
            b = 255;
        }

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

            ApplyColor();
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

        /// <summary>
        /// use the latest r/g/b values to update the game
        /// </summary>
        protected abstract void ApplyColor();
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
            //base: Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Availability = new AliveInMap();
        }


        // animate this more quickly.
        public override bool IsUpdateAnimation => true;

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


    public class CataractsCreepAndRestoreEffect : CrosshairShapeTimedSetEffect
    {
        public static readonly string EFFECT_ID = "kill_restores_cataracts_creep";

        public CataractsCreepAndRestoreEffect()
            : base(EFFECT_ID, TimeSpan.FromMinutes(5), new()
            {
                ["cl_crosshair_scale"] = "32",
            })
        {
            Mutex.Add(nameof(CataractsCrosshairEffect));//hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SIZE);
            //base: Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
            Availability = new InMap();
        }

        // doesn't disable just because starting state matches current state.
        public override bool IsSelectableGameState => IsAvailable
            // crosshair enabled.
            && "1" == TF2Effects.Instance.GetValue("crosshair");

        double scale;
        public override void StartEffect()
        {
            base.StartEffect();

            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            // "dot" crosshair, will grow
            TF2Effects.Instance.SetRequiredValue("cl_crosshair_file", "crosshair5");

            scale = 32;
            UpdateSize();

            TF2Effects.Instance.TF2Proxy.OnUserKill += RestoreOnKill;
        }

        private void UpdateSize()
        {
            TF2Effects.Instance.SetValue("cl_crosshair_scale", ((int)scale).ToString());
        }

        private void RestoreOnKill(string victim, string weapon, bool crit)
        {
            scale = Math.Max(
                scale / 2,
                1);
            UpdateSize();
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            double growthFactor = GetCreepFactor(timeSinceLastUpdate.TotalSeconds);
            scale += Math.Max(1, growthFactor);

            UpdateSize();
        }

        private double GetCreepFactor(double fractionalSecondsPassed)
        {
            double timePercent = Elapsed.TotalSeconds / Duration.TotalSeconds;
            double rate;
            if (timePercent > 0.90)
                // Panic mode: add 4000 more in the last 10% of the time
                rate = 6000 / (.10 * Duration.TotalSeconds);
            else
                // normal mode: add 1000 in the first 90% of the time.
                rate = 3000 / (.90 * Duration.TotalSeconds);

            double growthFactor = rate * fractionalSecondsPassed;
            return growthFactor;
        }

        public override void StopEffect()
        {
            scale = 32;

            base.StopEffect();
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
            SaveCrosshair();

            base.StartEffect();
        }

        private void SaveCrosshair()
        {
            crosshair = TF2Effects.Instance.GetValue("cl_crosshair_file")
                ?? CROSSHAIR_DEFAULT;
            if (string.IsNullOrWhiteSpace(crosshair)
                || IsInvalidCrosshair(crosshair))
                crosshair = CROSSHAIR_DEFAULT;
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
            RestoreCrosshair();
        }

        private void RestoreCrosshair()
        {
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
                // nothing more than shapes
            })
        {
            Mutex.Add(nameof(AlienCrosshairEffect));
            //base: Mutex.Add(TF2Effects.MUTEX_CROSSHAIR_SHAPE);
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
    }
}