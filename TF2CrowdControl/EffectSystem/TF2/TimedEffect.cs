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

    /// <summary>
    /// no-kill class change that tries to verify you eventually spawn as the class.
    /// Not a subclass of ForcedChangeClassEffect because we need a Duration to keep forcing the class change.
    /// </summary>
    public class ChangeClassEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "join_class_eventually";
        public ChangeClassEffect()
            : this(EFFECT_ID)
        {
        }
        private string commandFormat = "join_class {1}";
        protected ChangeClassEffect(string id)
            : base(id, TimeSpan.FromMinutes(7))
        {
            Availability = new InMap();
            // register needed variable
            _ = TF2Effects.Instance.GetValue(variable);
        }
        public override bool IsSelectableGameState => IsAvailable;

        protected string classSelection = string.Empty;
        protected string requestor = string.Empty;
        protected override void StartEffect(EffectDispatchRequest request)
        {
            // need to pull request parameter as part of the command

            // 0: part of format, but not used currently 
            requestor = request.Requestor;
            // 1: part of format
            classSelection = request.Parameter.ToLower(); // join_class supports "random" directly

            base.StartEffect(request);
        }

        private string variable = "hud_classautokill";
        private string prev = "0";
        public override void StartEffect()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for spawn right now.");

            string formattedMainCommand = string.Format(commandFormat, requestor, classSelection);

            prev = TF2Effects.Instance.GetValue(variable)
                ?? "0";
            if (prev != "0")
                _ = TF2Effects.Instance.RunRequiredCommand(
                    // extra insurance - we don't want to get this one wrong.
                    string.Format("{0} {1}; wait 10; {2}", variable, "0", formattedMainCommand));
            else
                _ = TF2Effects.Instance.RunRequiredCommand(formattedMainCommand);
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            if (TF2Effects.Instance.TF2Proxy == null)
                return; // hopefully next update it'll be set.

            // keep joining the requested class until we spawn as that class (or time is up)
            // - better: only do it if we detect a different class requested.
            if (TF2Effects.Instance.TF2Proxy.ClassSelection == classSelection)
                // NOTE not via OnUserSpawned because this exception needs to be during update's thread.
                throw new EffectFinishedEarlyException(string.Format("User spawned as {0}", classSelection));

            // Run this always - user could swap classes in spawn and it wouldn't output "next" info, so we can't rely on that value unless we also check Current class when it's different that the starting class.
            // Lots of work, when we could just spam the selected class which doesn't output if it's not new value.
            _ = TF2Effects.Instance.RunCommand(string.Format(commandFormat, requestor, classSelection));

            // - need to account for X minutes before they die/respawn as class
            // - should probably pause when dead even though it can be redeemed while dead.
            // TODO we would do that ^, but pausing prevents us from updating currently, and we need updates to correct user class changes.
            // - would like to account for Y minutes minimum time as class
            //
            // maybe 5 minutes to die and 2 minutes minimum?
            // Then shows 7 minutes they feel like they're stuck with class which isn't too bad.
            // maybe just 5 total if it pauses while dead.
            // FUTURE: Manually track 2 minute minimum as class and finish early.
        }

        public override void StopEffect()
        {
            if (prev != "0")
                TF2Effects.Instance.SetRequiredValue(variable, prev);
        }

        //protected override void CheckEffectWorked()
        //{
        //    // availability doesn't change, but if it became unavailable it probably won't take.
        //    if (Availability != null
        //        && !Availability.IsAvailable(TF2Effects.Instance.TF2Proxy))
        //        throw new EffectNotVerifiedException("Left the map before class applied");

        //    if (!string.IsNullOrEmpty(classSelection)
        //        && classSelection != "random" // although we're not offering this currently.
        //        && classSelection != TF2Effects.Instance.TF2Proxy?.NextClassSelection
        //        // also acceptable, although "Next" is the really relevant one.
        //        && classSelection != TF2Effects.Instance.TF2Proxy?.ClassSelection
        //        )
        //        throw new EffectNotVerifiedException("Class doesn't appear to have been applied");
        //}
    }

    public class JumpingEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "jumping";

        public JumpingEffect()
            : this(EFFECT_ID, TimeSpan.FromSeconds(45))
        {
            Mutex.Add(TF2Effects.MUTEX_FORCE_MOVE_JUMP);
            // don't try to taunt at the same time.
            Mutex.Add(nameof(TauntEffect));
            Mutex.Add(nameof(TauntAfterKillEffect));
        }
        protected JumpingEffect(string id, TimeSpan duration)
            : base(id, duration)
        {
            Availability = new AliveInMap();
        }

        public override bool IsSelectableGameState => IsAvailable;

        public override void StartEffect()
        {
            _ = TF2Effects.Instance.RunCommand("+jump");
        }

        private bool plusjump = true;
        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            base.Update(timeSinceLastUpdate);

            if (plusjump)
                _ = TF2Effects.Instance.RunCommand("+jump");
            else
                _ = TF2Effects.Instance.RunCommand("-jump");
            plusjump = !plusjump;

        }

        public override void StopEffect()
        {
            _ = TF2Effects.Instance.RunCommand("-jump");
        }
    }

    public class TauntEffect : TimedEffect
    {
        public static readonly string EFFECT_ID = "taunt_now";
        private DateTime startTime = DateTime.MinValue;

        public TauntEffect()
            : this(EFFECT_ID, TimeSpan.FromSeconds(5)) // enough time to finish jump and taunt.
        {
            Mutex.Add(nameof(TauntEffect)); // just mutex with itself.
        }
        protected TauntEffect(string id, TimeSpan duration)
            : base(id, duration)
        {
            Availability = new AliveInMap();
        }

        public override bool IsSelectableGameState => IsAvailable
            && null != TF2Effects.Instance.TF2Proxy;

        public override void StartEffect()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch status right now.");

            StartTaunt();
        }

        protected void StartTaunt()
        {
            startTime = DateTime.Now;
            SendTaunt();
        }

        protected virtual void SendTaunt()
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
            TimeSpan longestAttempt = GetLongestAttemptSpan();
            if (DateTime.Now.Subtract(startTime) <= longestAttempt)
            {
                // reset the attempts if we're mid-jump.
                if (TF2Effects.Instance.TF2Proxy?.IsJumping ?? false)
                    JumpedDuringUpdate();
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

        /// <summary>
        /// How long taunt commands should be attempted (since they stopped jumping).
        /// Override for e.g. "taunt for the next 60 seconds"
        /// </summary>
        /// <returns></returns>
        protected virtual TimeSpan GetLongestAttemptSpan()
        {
            // Shortest ability taunt is 1.2 seconds and we don't want to accidentally taunt twice.
            // ... but we're often mid-air longer than that, so it's worth the risk I think.
            // ... but now we kind of detect when you're jumping and reset our timing, so 1 second post-jump is plenty.
            return TimeSpan.FromSeconds(1.2);
        }

        protected virtual void JumpedDuringUpdate()
        {
            startTime = DateTime.Now;
        }

        public override void StopEffect()
        {
            startTime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// keep taunting for a long duration continuously
    /// </summary>
    public class TauntContinouslyEffect : TauntEffect
    {
        new public static readonly string EFFECT_ID = "taunt_continuously";

        public TauntContinouslyEffect()
            : this(EFFECT_ID, TimeSpan.FromSeconds(30))
        {
        }
        protected TauntContinouslyEffect(string id, TimeSpan duration)
            : base(id, duration)
        {
            Mutex.Add(nameof(TauntEffect)); //mutex with parent
        }

        protected override TimeSpan GetLongestAttemptSpan()
        {
            return base.Duration;
        }
    }

    // could make this abstract since this is not used directly anymore.
    public class TauntAfterKillEffect : TauntEffect
    {
        new public static readonly string EFFECT_ID = "taunt_after_kill";

        public TauntAfterKillEffect()
            : this(EFFECT_ID, DefaultTimeSpan)
        {
        }
        protected TauntAfterKillEffect(string id, TimeSpan duration)
            : base(id, duration)
        {
            Mutex.Add(nameof(TauntAfterKillEffect)); //hierarchy is all mutex
            // specifically not mutex with a basic taunt - they can do that while waiting for these,
            // and they can queue these while an immediate taunt is in progress.
        }

        public override void StartEffect()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            TF2Effects.Instance.TF2Proxy.OnUserKill += TauntAfterKillEffect_OnUserKill;
        }

        private void TauntAfterKillEffect_OnUserKill(string victim, string weapon, bool crit)
        {
            if (ShouldTaunt(victim, weapon, crit))
                StartTaunt();

            //FUTURE tempting to add "say ha ha I killed you, {victim}"
        }

        virtual protected bool ShouldTaunt(string victim, string weapon, bool crit)
        {
            return true;
        }

        public override void StopEffect()
        {
            base.StopEffect();

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
            Mutex.Add(TF2Effects.MUTEX_FORCE_MOVE_ROTATE);
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
            Mutex.Add(TF2Effects.MUTEX_FORCE_MOVE_FORWARD);
            Mutex.Add(TF2Effects.MUTEX_FORCE_MOVE_ATTACK);
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
            challenge = new KillsChallenge(1, minimumSurvivalTime: TimeSpan.FromSeconds(0.7));
        }

        protected override void JumpedDuringUpdate()
        {
            base.JumpedDuringUpdate();

            (challenge as KillsChallenge)?.SurvivedSince(DateTime.Now);
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
            challenge = new CritKillsChallenge(1, minimumSurvivalTime: TimeSpan.FromSeconds(0.7));
        }

        protected override void JumpedDuringUpdate()
        {
            base.JumpedDuringUpdate();

            // can't start taunt - they need to survive til landing and taunt started.
            (challenge as CritKillsChallenge)?.SurvivedSince(DateTime.Now);
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
            challenge = new DeathsChallenge(6);// 6th death halving scale is more than basic pixelated
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

}