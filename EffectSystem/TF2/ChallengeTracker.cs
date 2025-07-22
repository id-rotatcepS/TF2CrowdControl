

namespace EffectSystem.TF2
{
    // Assume no effect until the first part of the duration is completed (ending early if challenge is met), then bad effect for the rest of the duration.
    // "5m trial: 5 streak or face 10m Black & White"
    // 5m duration (with pauses), at end it 'pauses' for (or changes duration to) 10m, then finishes
    // "30s trial: 1 kill or Explode"
    // These need a "challenge completed" sound.
    //public abstract class TrialTimedEffect : TimedEffect
    //{

    //}

    public interface ChallengeTracker
    {
        /// <summary>
        /// throws EffectNotAppliedException if challenge cannot be started
        /// </summary>
        void Start();
        bool IsCompleted { get; }
        void Stop();
    }

    public class KillstreakChallenge : ChallengeTracker
    {
        private int killstreakGoal;
        private int killstreak = 0;

        public KillstreakChallenge(int goal)
        {
            this.killstreakGoal = goal;
        }

        public void Start()
        {
            killstreak = 0;
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            TF2Effects.Instance.TF2Proxy.OnUserDied += ResetStreak;
            TF2Effects.Instance.TF2Proxy.OnUserKill += IncrementStreak;
        }

        private void ResetStreak()
        {
            killstreak = 0;
        }

        private void IncrementStreak(string victim, string weapon, bool crit)
        {
            ++killstreak;
            // Stop immediately if we're done, rather than risk resetting the streak.
            if (IsCompleted)
                Stop();
        }

        public bool IsCompleted => (killstreak >= killstreakGoal);

        public void Stop()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                return;

            TF2Effects.Instance.TF2Proxy.OnUserDied -= ResetStreak;
            TF2Effects.Instance.TF2Proxy.OnUserKill -= IncrementStreak;
        }
    }

    public class KillsChallenge : ChallengeTracker
    {
        private readonly TimeSpan minimumSurvival;
        private readonly int killsGoal;
        private int kills = 0;

        public KillsChallenge(int goal)
            : this(goal, TimeSpan.Zero)
        {
        }

        public KillsChallenge(int goal, TimeSpan minimumSurvivalTime)
        {
            this.killsGoal = goal;
            this.minimumSurvival = minimumSurvivalTime;
        }

        public void Start()
        {
            kills = 0;
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for kills right now.");

            TF2Effects.Instance.TF2Proxy.OnUserKill += IncrementStreak;
            TF2Effects.Instance.TF2Proxy.OnUserDied += InvalidateIncrement;
        }

        private DateTime lastKill = DateTime.MinValue;
        private void InvalidateIncrement()
        {
            if (kills > 0 && DateTime.Now.Subtract(lastKill) < minimumSurvival)
            {
                --kills;
                //TODO really needs to restore previous time when goal is more than 1.
                lastKill = DateTime.MinValue;
            }
        }

        private void IncrementStreak(string victim, string weapon, bool crit)
        {
            if (ShouldIncrement(victim, weapon, crit))
            {
                ++kills;
                lastKill = DateTime.Now;
            }
        }

        protected virtual bool ShouldIncrement(string victim, string weapon, bool crit)
        {
            return true;
        }

        public bool IsCompleted => (kills >= killsGoal
            // yes, if lastKill is MinValue this will pass - weird case, let's call it completed to make sure we finish the effect.
            && DateTime.Now.Subtract(lastKill) > minimumSurvival);

        public void Stop()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                return;

            TF2Effects.Instance.TF2Proxy.OnUserKill -= IncrementStreak;
            TF2Effects.Instance.TF2Proxy.OnUserDied -= InvalidateIncrement;
        }

        /// <summary>
        /// External influence on what surviving for minimumSurvivalTime is based on.
        /// Only updates the value if it already is set.
        /// </summary>
        /// <param name="survivalStart"></param>
        public void SurvivedSince(DateTime survivalStart)
        {
            if (lastKill != DateTime.MinValue)
                lastKill = survivalStart;
        }
    }

    public class CritKillsChallenge : KillsChallenge
    {
        public CritKillsChallenge(int goal)
            : base(goal)
        {
        }

        public CritKillsChallenge(int goal, TimeSpan minimumSurvivalTime)
            : base(goal, minimumSurvivalTime)
        {
        }

        protected override bool ShouldIncrement(string victim, string weapon, bool crit)
        {
            return crit;
        }
    }

    public class DeathsChallenge : ChallengeTracker
    {
        private int deathsGoal;
        private int deaths = 0;

        public DeathsChallenge(int goal)
        {
            this.deathsGoal = goal;
        }

        public void Start()
        {
            deaths = 0;
            if (TF2Effects.Instance.TF2Proxy == null)
                throw new EffectNotAppliedException("Unexpected error - unable to watch for deaths right now.");

            TF2Effects.Instance.TF2Proxy.OnUserDied += IncrementStreak;
        }

        private void IncrementStreak()
        {
            ++deaths;
        }

        public bool IsCompleted => (deaths >= deathsGoal);

        public void Stop()
        {
            if (TF2Effects.Instance.TF2Proxy == null)
                return;

            TF2Effects.Instance.TF2Proxy.OnUserDied -= IncrementStreak;
        }
    }

}