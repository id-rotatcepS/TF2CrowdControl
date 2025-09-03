
namespace EffectSystem.TF2
{
    public class TF2Effects
    {
        private static TF2Effects? _instance;
        public static TF2Effects Instance
            => _instance
            ??= new TF2Effects();

        public TF2Proxy? TF2Proxy { get; set; }

        #region Mutual Exclusion categories
        public static readonly string MUTEX_VIEWMODEL = "viewmodel";
        public static readonly string MUTEX_FOV = "fov";
        public static readonly string MUTEX_WEAPONSLOT = "weaponslot";
        public static readonly string MUTEX_CROSSHAIR_COLOR = "crosshair_color";
        public static readonly string MUTEX_CROSSHAIR_SIZE = "crosshair_size";
        public static readonly string MUTEX_CROSSHAIR_SHAPE = "crosshair_shape";
        public static readonly string MUTEX_SCOREBOARD = "scoreboard";
        public static readonly string MUTEX_MOUSE = "mouse";
        public static readonly string MUTEX_FORCE_MOVE_ROTATE = "force_move_rotate";
        public static readonly string MUTEX_FORCE_MOVE_STRAFE = "force_move_strafe";
        public static readonly string MUTEX_FORCE_MOVE_ATTACK = "force_move_attack";
        public static readonly string MUTEX_FORCE_MOVE_FORWARD = "force_move_forward";
        public static readonly string MUTEX_FORCE_MOVE_JUMP = "force_move_jump";
        public static readonly string MUTEX_VIEWPORT = "viewport";
        public static readonly string MUTEX_BLOOM = "bloom";
        public static readonly string MUTEX_CLASS_CHANGE = "join_class";
        public static readonly string MUTEX_MOTION_BLUR = "motion_blur";
        /// <summary>
        /// can't play two files at the same time, and don't overlap muting with audio effects
        /// </summary>
        public static readonly string MUTEX_AUDIO = "audio";
        #endregion Mutual Exclusion categories

        // Sounds that can be used with the "play" command (maybe will get moved into the TF2FrameworkInterface)
        // (paths generally pulled from game_sounds.txt and similar... mix of mp3/wave and back/forward slashes are from there)
        #region sounds
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_SLIP = new TF2FrameworkInterface.TF2Sound("misc/banana_slip.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_CLOCK_TICK = new TF2FrameworkInterface.TF2Sound("misc/halloween/clock_tick.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_WHEEL_OF_FATE = new TF2FrameworkInterface.TF2Sound("misc/halloween/hwn_wheel_of_fate.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_MISSILE_EXPLOSION = new TF2FrameworkInterface.TF2Sound("misc\\doomsday_missile_explosion.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_ALARM_KLAXON = new TF2FrameworkInterface.TF2Sound("ambient_mp3\\alarms\\doomsday_lift_alarm.mp3");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_ACHIEVEMENT_GUITAR_CHEER = new TF2FrameworkInterface.TF2Sound("misc/achievement_earned.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_ANNOUNCER_SUDDEN_DEATH = new TF2FrameworkInterface.TF2Sound("misc/your_team_suddendeath.mp3");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_HL_SUITCHARGED = new TF2FrameworkInterface.TF2Sound("items/suitchargeok1.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_RESUPPLY_CABINET = new TF2FrameworkInterface.TF2Sound("items/regenerate.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_BONUS_DUCKS3 = new TF2FrameworkInterface.TF2Sound("vo/halloween_merasmus/sf14_merasmus_minigame_duckhunt_bonusducks_03.mp3");
        public static readonly TF2FrameworkInterface.TF2Sound[] SOUND_QUACKS = new[]
        {
            new TF2FrameworkInterface.TF2Sound("ambient/bumper_car_quack1.wav"),
            new TF2FrameworkInterface.TF2Sound("ambient/bumper_car_quack2.wav"),
            new TF2FrameworkInterface.TF2Sound("ambient/bumper_car_quack3.wav"),
            new TF2FrameworkInterface.TF2Sound("ambient/bumper_car_quack4.wav"),
            new TF2FrameworkInterface.TF2Sound("ambient/bumper_car_quack5.wav"),
            new TF2FrameworkInterface.TF2Sound("ambient/bumper_car_quack9.wav"),
            new TF2FrameworkInterface.TF2Sound("ambient/bumper_car_quack11.wav")
        };
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_PROJECTOR_MOVIE = new TF2FrameworkInterface.TF2Sound("ui/projector_movie.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_PROJECTOR_UP = new TF2FrameworkInterface.TF2Sound("ui/projector_screen_up_long.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_JARATE_TOSS1 = new TF2FrameworkInterface.TF2Sound("vo/sniper_JarateToss01.mp3");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_JARATE_EXPLODE = new TF2FrameworkInterface.TF2Sound("weapons/jar_explode.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_CRATE_OPEN = new TF2FrameworkInterface.TF2Sound("ui/item_open_crate_short.wav");
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_CRATE_RARE_MVM_OPEN = new TF2FrameworkInterface.TF2Sound("ui/itemcrate_smash_ultrarare_short.wav");
        /// <summary>
        /// Valid sound file that plays no sound, but ends any other wav (especially looping sounds)
        /// </summary>
        public static readonly TF2FrameworkInterface.TF2Sound SOUND_STOP_SOUND = new TF2FrameworkInterface.TF2Sound("misc\\blank.wav");
        #endregion sounds

        /// <summary>
        /// <see cref="CrowdControl.Games.Packs.TF2Spectator.TF2Spectator"/> contains the registration of these effects with the same IDs.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Effect> CreateAllEffects()
        {
            return [
                new KillEffect(),
                new ExplodeEffect(),
                new EngineerDestroyBuildingsEffect(),
                new EngineerDestroySentryEffect(),
                new EngineerDestroyDispenserEffect(),
                new EngineerDestroyTeleportersEffect(),
                new SpyRemoveDisguiseEffect(),
                new MedicUberNowEffect(),
                new MedicRadarEffect(),

                new BlackAndWhiteTimedEffect(),
                new SilentMovieTimedEffect(),
                new PixelatedTimedEffect(),
                new DreamTimedEffect(),
                new RaveEffect(),
                new VertigoTimedEffect(),

                new HypeTrainEffect(), // Rave + message, more availability, and party chat message

                // big and small depending on what they usually use.
                new BigGunsTimedEffect(),
                new SmallGunsTimedEffect(),
                new NoGunsToggleEffect(),
                new LongArmsTimedEffect(),
                new VRModeTimedEffect(),
                new InspectEffect(),

                new NoCrosshairEffect(),
                new RainbowCrosshairEffect(),
                new CataractsCrosshairEffect(),
                new GiantCrosshairEffect(),
                new BrrrCrosshairEffect(),
                new AlienCrosshairEffect(),

                new DrunkEffect(),
                new UnderwaterFadeEffect(),
                new RainbowCombatTextEffect(),
                new QuackEffect(),

                new MeleeOnlyEffect(),
                new WeaponShuffleEffect(),
                new WalkEffect(),
                new WeaponShuffleEffect(),

                new ShowScoreboardEffect(),
                new ShowScoreboardMeanEffect(),
                new VoiceMenuEffect(),
                new HackerHUDEffect(),
                new ContrackerEffect(),
                new PopupUIEffect(),
                new HotMicEffect(),
                new ItemPreviewEffect(),

                new MouseSensitivityHighEffect(),
                new MouseSensitivityLowEffect(),

                new QuitEffect(),
                new RetryEffect(),
                new ChangeClassAndDieEffect(),
                new ChangeClassEffect(),
                new SelfKickEffect(),

                new SpinEffect(),
                new WM1Effect(),
                new TauntEffect(),
                new TauntContinouslyEffect(),
                new JumpingEffect(),
                new NoJumpingEffect(),
                new TankModeEffect(),
                new BindLeftRightSwapEffect(),
                new BindForwardBackSwapEffect(),
                new CrabWalkEffect(),
                new NoStrafingEffect(),

                new ChallengeMeleeTimedEffect(),
                new SingleTauntAfterKillEffect(),
                new SingleTauntAfterCritKillEffect(),
                new ChallengeCataractsEffect(),
                new ChallengeBlackAndWhiteTimedEffect(),
                new VertigoCreepAndRestoreEffect(),
                new CataractsCreepAndRestoreEffect(),
                new BindEforExplodeEffect(),

                new DeathAddsPixelatedTimedEffect(),
                new DeathAddsDreamTimedEffect(),
                new DeathAddsVertigoTimedEffect(),
                ];
        }

        public string RunCommand(string command)
        {
            try
            {
                return RunRequiredCommand(command);
            }
            catch (Exception ex)
            {
                ASPEN.Aspen.Log.Warning(ex.Message);
                return "";
            }
        }

        public string RunRequiredCommand(string command)
        {
            if (TF2Proxy == null)
                throw new EffectNotAppliedException(string.Format("No connection available to run command: {0}", command));

            return TF2Proxy.RunCommand(command);
        }

        public void SetInfo(string variable, string value)
        {
            if (TF2Proxy == null)
            {
                ASPEN.Aspen.Log.Warning(string.Format("No connection available to set info: {0} = {1}", variable, value));
                return;
            }

            TF2Proxy.SetInfo(variable, value);
        }

        public void SetValue(string variable, string value)
        {
            try
            {
                SetRequiredValue(variable, value);
            }
            catch (Exception ex)
            {
                ASPEN.Aspen.Log.Warning(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <exception cref="EffectNotAppliedException"></exception>
        public void SetRequiredValue(string variable, string value)
        {
            if (TF2Proxy == null)
                throw new EffectNotAppliedException(string.Format("No connection available to set value: {0} = {1}", variable, value));

            TF2Proxy.SetValue(variable, value);
        }

        public string? GetValue(string variable)
        {
            return TF2Proxy?.GetValue(variable);
        }

        /// <summary>
        /// Play a TF2 Sound File
        /// </summary>
        /// <param name="sound"></param>
        public void Play(TF2FrameworkInterface.TF2Sound sound)
        {
            _ = TF2Proxy?.RunCommand(string.Format("play {0}", sound.File));
        }

        /// <summary>
        /// Play a TF2 Sound File at a lower volume.  
        /// Note, some files at 100% (1.0) will still be quieter than using <see cref="Play(TF2Sound)"/>.
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="volumePercent">number between 0.0 and 1.0 (larger values just act like 1.0)</param>
        public void PlayVol(TF2FrameworkInterface.TF2Sound sound, double volumePercent)
        {
            _ = TF2Proxy?.RunCommand(string.Format("playvol {0} {1}", sound.File, volumePercent));
        }
    }
}