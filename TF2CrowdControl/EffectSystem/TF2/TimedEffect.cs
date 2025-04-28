namespace EffectSystem.TF2
{
    /// <summary>
    /// An effect reports it's "availability" and pauses its duration when not available.
    /// Simplifies StartEffect & StopEffect.
    /// (with optional Challenge to stop the duration early)
    /// </summary>
    abstract public class TimedEffect : PausableEffect
    {
        public TimedEffect(string id, TimeSpan span) : base(id,
            defaultDuration: span)
        {
        }

        override protected bool CanElapse => IsAvailable;
        protected bool IsAvailable => Availability?.IsAvailable(TF2Effects.Instance.TF2Proxy) ?? true;
        public TF2Availability? Availability { get; set; }

        /// <summary>
        /// If non-null, the challenge is started, checked during update for possible early finish, and stopped.
        /// </summary>
        protected ChallengeTracker? challenge = null;

        protected override void StartEffect(EffectDispatchRequest request)
        {
            challenge?.Start();
            StartEffect();
        }
        public abstract void StartEffect();

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            if (challenge != null && challenge.IsCompleted)
            {
                challenge.Stop();
                throw new EffectFinishedEarlyException();
            }
            // does nothing for this type of effect - we just start and stop.
        }

        protected override void StopEffect(TimeSpan timeSinceLastUpdate)
        {
            challenge?.Stop();
            StopEffect();
        }
        public abstract void StopEffect();
    }

    // could make this abstract since this is not used directly anymore.
    public class TauntAfterKillEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "taunt_after_kill";
        private DateTime startTime = DateTime.MinValue;

        public TauntAfterKillEffect()
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }
        protected TauntAfterKillEffect(string id, TimeSpan duration)
            : base(id, duration)
        {
            Mutex.Add(nameof(TauntAfterKillEffect)); //hierarchy is all mutex
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
            {
                startTime = DateTime.Now;
                SendTaunt();
            }
            //FUTURE tempting to add "say ha ha I killed you, {victim}"
        }

        virtual protected bool ShouldTaunt(string victim, string weapon, bool crit)
        {
            return true;
        }

        private void SendTaunt()
        {
            // choose a random taunt AND default taunt in case nothing was equipped there.
            // slot 0 is held-weapon taunt, equippable slots are 1-8
            int tauntSlot = Random.Shared.Next(0, 9);// max is EXclusive.

            _ = TF2Effects.Instance.RunCommand(string.Format("taunt {0}", tauntSlot));
            //FUTURE user-selected taunt by name?
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            if (startTime == DateTime.MinValue)
                return;

            // Makes multiple attempts in case player is mid-jump.
            // Shortest ability taunt is 1.2 seconds and we don't want to accidentally taunt twice.
            // ... but we're often mid-air longer than that, so it's worth the risk I think.
            // ... but now we kind of detect when you're jumping and reset our timing, so 1 second post-jump is plenty.
            TimeSpan longestAttempt = TimeSpan.FromSeconds(1.2);
            if (DateTime.Now.Subtract(startTime) <= longestAttempt)
            {
                // reset the attempts if we're mid-jump.
                if (TF2Effects.Instance.TF2Proxy?.IsJumping ?? false)
                    startTime = DateTime.Now;
                else
                    SendTaunt();
            }
            else
            {
                // final attempt with just default taunt.
                // (may have paused and resumed, so restrict how much time may have passed to do this final attempt).
                bool shouldMakeFinalAttempt = DateTime.Now.Subtract(startTime) <= longestAttempt * 2;

                // clear this before sending command to prevent possible race condition(?)
                startTime = DateTime.MinValue;

                if (shouldMakeFinalAttempt)
                    _ = TF2Effects.Instance.RunCommand("taunt");
            }
        }

        public override void StopEffect()
        {
            startTime = DateTime.MinValue;
            if (TF2Effects.Instance.TF2Proxy != null)
                TF2Effects.Instance.TF2Proxy.OnUserKill -= TauntAfterKillEffect_OnUserKill;
        }
    }

    // could make this abstract since this is not used directly anymore.
    public class TauntAfterCritKillEffect : TauntAfterKillEffect
    {
        new public static readonly string EFFECT_ID = "taunt_after_crit_kill";

        public TauntAfterCritKillEffect()
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }
        protected TauntAfterCritKillEffect(string id, TimeSpan duration)
            : base(id, duration)
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
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }
        protected MeleeOnlyEffect(string id, TimeSpan duration)
            : base(id, duration)
        {
            Mutex.Add(nameof(MeleeOnlyEffect)); //hierarchy is all mutex
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
            base.Update(timeSinceLastUpdate);

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
            : this(EFFECT_ID)
        {
        }
        protected ShowScoreboardEffect(string id)
            : base(id, TimeSpan.FromSeconds(6))
        {
            Availability = new InMap();
            Mutex.Add(TF2Effects.MUTEX_SCOREBOARD);
            Mutex.Add(nameof(ShowScoreboardEffect)); // hierarchy mutex

            _ = TF2Effects.Instance.GetValue(variable);// register the variable for current value.
        }

        public override bool IsSelectableGameState => IsAvailable;

        protected virtual string VariableTemporaryValue => "0";

        private static readonly string variable = "tf_scoreboard_mouse_mode";
        private string? variable_original_config;
        public override void StartEffect()
        {
            variable_original_config = TF2Effects.Instance.GetValue(variable);
            TF2Effects.Instance.SetValue(variable, VariableTemporaryValue);
            _ = TF2Effects.Instance.RunRequiredCommand("+showscores");
        }

        public override void StopEffect()
        {
            _ = TF2Effects.Instance.RunCommand("-showscores");
            if (!string.IsNullOrWhiteSpace(variable_original_config))
                TF2Effects.Instance.SetValue(variable, variable_original_config);
        }
    }

    public class ShowScoreboardMeanEffect : ShowScoreboardEffect
    {
        new public static readonly string EFFECT_ID = "show_score_mean";
        public ShowScoreboardMeanEffect()
            : base(EFFECT_ID)
        {
        }

        protected override string VariableTemporaryValue => "1";
    }

    public class SpinEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "spin_left";

        public SpinEffect()
            : base(EFFECT_ID, TimeSpan.FromSeconds(30))
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

    // A TimedEffect that ends early when a challenge is met.
    // Assume a bad effect at the start with a very long duration but it ends early if challenge is met.
    // "Challenge: Black & White killing spree (5ks)" (30m)
    // "Challenge: W+M1 3 kill" (10m)
    // "Challenge: Melee Only 3 kill" (10m)
    // 
    // Kill Streak Announcements:
    // Player is on a killing spree 5 >>
    // Player is unstoppable 10 >>
    // Player is on a rampage 15 >>
    // Player is god-like 20 >> ("is still god-like" for each 5 after that)
    // (4x in MvM)

    // Cataracts until...
    // Spin until...
    // WM1 until...
    // No Guns until...
    // TimedSetEffect until...

    /// <summary>
    /// 30 minute Effect that cancels upon meeting the 5 kill streak challenge.
    /// </summary>
    public class ChallengeBlackAndWhiteTimedEffect : BlackAndWhiteTimedEffect
    {
        new public static readonly string EFFECT_ID = "blackandwhite_challenge_5ks";

        public ChallengeBlackAndWhiteTimedEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 10, 0))
        {
            challenge = new KillstreakChallenge(5);
        }
    }

    /// <summary>
    /// 10 minute Effect that cancels upon meeting the 3 kills challenge.
    /// </summary>
    public class ChallengeMeleeTimedEffect : MeleeOnlyEffect
    {
        new public static readonly string EFFECT_ID = "melee_only_challenge_3k";

        public ChallengeMeleeTimedEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 10, 0))
        {
            challenge = new KillsChallenge(3);
        }
    }

    /// <summary>
    /// 30 minute Effect that cancels upon meeting the single kill (and survive) challenge.
    /// </summary>
    public class SingleTauntAfterKillEffect : TauntAfterKillEffect
    {
        new public static readonly string EFFECT_ID = "taunt_after_kill_challenge_1k";

        public SingleTauntAfterKillEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 10, 0))
        {
            challenge = new KillsChallenge(1, minimumSurvivalTime: TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>
    /// 30 minute Effect that cancels upon meeting the single crit kill (and survive) challenge.
    /// </summary>
    public class SingleTauntAfterCritKillEffect : TauntAfterCritKillEffect
    {
        new public static readonly string EFFECT_ID = "taunt_after_crit_kill_challenge_1k";

        public SingleTauntAfterCritKillEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 10, 0))
        {
            challenge = new CritKillsChallenge(1, minimumSurvivalTime: TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>
    /// 30 minute Effect that cancels upon meeting the single kill challenge.
    /// </summary>
    public class ChallengeCataractsEffect : CataractsCrosshairEffect
    {
        new public static readonly string EFFECT_ID = "crosshair_cataracts_challenge_3k";

        public ChallengeCataractsEffect()
            : base(EFFECT_ID, new TimeSpan(0, minutes: 10, 0))
        {
            challenge = new KillsChallenge(3);
        }
    }

    public class DeathAddsPixelatedTimedEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "death_adds_pixelated";
        public DeathAddsPixelatedTimedEffect()
            : base(EFFECT_ID, TimeSpan.FromMinutes(10))
        {
            challenge = new DeathsChallenge(7);// 7th death halving scale would put it below 0.01
            Mutex.Add(nameof(PixelatedTimedEffect)); //hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_VIEWPORT);
            Availability = new InMap();
        }

        public override bool IsSelectableGameState => IsAvailable;

        private double currentScale = 1.0;
        public override void StartEffect()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            currentScale = 1.0;
            UpdateScale();
            TF2Effects.Instance.TF2Proxy.OnUserDied += OnDeath;
        }

        private void UpdateScale()
        {
            TF2Effects.Instance.SetRequiredValue("mat_viewportscale", currentScale.ToString());
        }

        private void OnDeath()
        {
            currentScale /= 2.0;
            UpdateScale();
        }

        public override void StopEffect()
        {
            if (TF2Effects.Instance.TF2Proxy != null)
                TF2Effects.Instance.TF2Proxy.OnUserDied -= OnDeath;

            currentScale = 1.0;
            UpdateScale();
        }
    }

    public class DeathAddsDreamTimedEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "death_adds_dream";
        public DeathAddsDreamTimedEffect()
            : base(EFFECT_ID, TimeSpan.FromMinutes(10))
        {
            challenge = new DeathsChallenge(8);// 8th death doubling scale would put it well past 100
            Mutex.Add(nameof(DreamTimedEffect)); //hierarchy is all mutex
            Mutex.Add(TF2Effects.MUTEX_BLOOM);
            Availability = new InMap();
            // register the variables we'll need later.
            _ = TF2Effects.Instance.GetValue("mat_force_bloom");
        }

        public override bool IsSelectableGameState => IsAvailable;

        private double bloomFactor = 1.0;
        private string startBloomFactor = "1";
        public override void StartEffect()
        {
            // save mat_force_bloom
            startBloomFactor = TF2Effects.Instance.GetValue("mat_force_bloom") ?? "1";
            // set mat_force_bloom
            TF2Effects.Instance.SetRequiredValue("mat_force_bloom", "1");

            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            bloomFactor = 1.0;
            UpdateBloom();
            TF2Effects.Instance.TF2Proxy.OnUserDied += OnDeath;
        }

        private void UpdateBloom()
        {
            TF2Effects.Instance.SetRequiredValue("mat_bloom_scalefactor_scalar", bloomFactor.ToString());
        }

        private void OnDeath()
        {
            bloomFactor *= 2.0;
            UpdateBloom();
        }

        public override void StopEffect()
        {
            if (TF2Effects.Instance.TF2Proxy != null)
                TF2Effects.Instance.TF2Proxy.OnUserDied -= OnDeath;

            bloomFactor = 1.0;
            UpdateBloom();

            // restore mat_force_bloom
            TF2Effects.Instance.SetValue("mat_force_bloom", startBloomFactor);
        }
    }
}