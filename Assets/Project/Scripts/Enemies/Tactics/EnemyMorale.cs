using UnityEngine;

namespace CGD.Enemies
{
    // How much fight is left in an enemy (1 = steady). Seeing allies go down and getting
    // badly hurt shake it; time brings it back. Below the profile's retreat line it breaks
    // off to regroup, and only fights again once it has recovered past the rally line, so it
    // doesn't flicker between the two. The faction's doctrine (MoraleDoctrine) decides how
    // much each shock counts, what steadies it or wears it down, and where the lines sit.
    internal sealed class EnemyMorale
    {
        private const float LowHealth        = 0.35f;
        // Kin close by are counted this often, not every frame.
        private const float KinCountEvery    = 0.5f;
        private const float ShieldBrokenBelow = 0.1f;

        private readonly EnemyAI _ai;
        private bool  _hurtCounted;
        private int   _kinNear;
        private float _nextKinCount;

        public EnemyMorale(EnemyAI ai) => _ai = ai;

        public float Value     { get; private set; } = 1f;
        public bool  IsShaken  { get; private set; }

        private PerceptionProfile Profile  => _ai.Perception.Profile;
        private MoraleDoctrine    Doctrine => _ai.Doctrine.Morale;

        public void Tick(float deltaTime)
        {
            Value = Mathf.Clamp01(Value + Doctrine.RecoveryPerSecond(Profile.MoraleRecovery, KinNear(), InFight,
                TargetIsWeak, HostilesSensed(), ShieldIsBroken) * deltaTime);
            UpdateShaken();
        }

        // An ally it can see or hear nearby went down; a higher-ranking one shakes it more.
        public void OnAllyDown(EnemyAI fallen)
        {
            int fallenTier = fallen != null ? fallen.Data.Tier : _ai.Data.Tier;
            Value = Mathf.Max(0f, Value - Doctrine.AllyDownLoss(Profile.MoraleLossAllyDown, fallenTier, _ai.Data.Tier));
            UpdateShaken();
        }

        // Something outside the fight rattled it (the squad leader went down).
        public void Shake(float amount)
        {
            Value = Mathf.Max(0f, Value - amount);
            UpdateShaken();
        }

        // Something put fight back into it (it stepped up to lead).
        public void Steady(float amount)
        {
            Value = Mathf.Min(1f, Value + Mathf.Max(0f, amount));
            UpdateShaken();
        }

        public void OnDamaged()
        {
            var health = _ai.Health;
            if (_hurtCounted || health.MaxHealth <= 0f || health.Health / health.MaxHealth > LowHealth) return;

            _hurtCounted = true;
            Value = Mathf.Max(0f, Value - Doctrine.HurtLoss(Profile.MoraleLossHurt));
            UpdateShaken();
        }

        public void Reset()
        {
            Value         = 1f;
            IsShaken      = false;
            _hurtCounted  = false;
            _kinNear      = 0;
            _nextKinCount = 0f;
        }

        private bool InFight => _ai.Perception.Level is AwarenessLevel.Engaged or AwarenessLevel.Searching;

        private bool TargetIsWeak => _ai.Perception.Target != null && Doctrine.IsTargetWeak(_ai.Perception.TargetHealthShare);

        private bool ShieldIsBroken
        {
            get
            {
                var health = _ai.Health;
                return health.MaxShield > 0f && health.Shield / health.MaxShield < ShieldBrokenBelow;
            }
        }

        // Hostiles it actually senses, not just any within range.
        private int HostilesSensed()
        {
            if (!InFight) return 0;

            var weighed = _ai.Perception.Weighed;
            int count = 0;
            for (int i = 0; i < weighed.Count; i++)
                if (weighed[i].Sensed) count++;
            return count;
        }

        private int KinNear()
        {
            if (!Doctrine.IsSteadiedByKin && !InFight) return 0;
            if (Time.time >= _nextKinCount)
            {
                _nextKinCount = Time.time + KinCountEvery;
                _kinNear = EnemyRoster.KinWithin(_ai, Doctrine.KinSteadiesRadius);
            }
            return _kinNear;
        }

        // The director's pacing moves the break point: harder to shake at the peak, quicker to
        // fall back in the calm. Rallying stays where it is (kept above the break point).
        private void UpdateShaken()
        {
            float rallyAbove   = Mathf.Clamp(Profile.RallyAbove * Doctrine.RallyAboveScale, 0.1f, 1f);
            float retreatBelow = Mathf.Min(Profile.RetreatBelow * Doctrine.RetreatBelowScale * EnemyPacing.Current.FallBackScale, rallyAbove - 0.05f);

            if (!IsShaken && Value < retreatBelow)
            {
                IsShaken = true;
                if (_ai.Doctrine.Escalation.CallsForHelpWhenShaken) _ai.Perception.CallForHelp();
            }
            else if (IsShaken && Value >= rallyAbove)
            {
                IsShaken = false;
            }
        }
    }
}
