using UnityEngine;

namespace CGD.Enemies
{
    // How much fight is left in an enemy (1 = steady). Seeing allies go down and getting
    // badly hurt shake it; time brings it back. Below the profile's retreat line it breaks
    // off to regroup, and only fights again once it has recovered past the rally line, so it
    // doesn't flicker between the two.
    internal sealed class EnemyMorale
    {
        private const float LowHealth = 0.35f;

        private readonly EnemyAI _ai;
        private bool _hurtCounted;

        public EnemyMorale(EnemyAI ai) => _ai = ai;

        public float Value     { get; private set; } = 1f;
        public bool  IsShaken  { get; private set; }

        private PerceptionProfile Profile => _ai.Perception.Profile;

        public void Tick(float deltaTime)
        {
            Value = Mathf.Min(1f, Value + Profile.MoraleRecovery * deltaTime);
            UpdateShaken();
        }

        public void OnAllyDown()
        {
            Value = Mathf.Max(0f, Value - Profile.MoraleLossAllyDown);
            UpdateShaken();
        }

        // Something outside the fight rattled it (the squad leader went down).
        public void Shake(float amount)
        {
            Value = Mathf.Max(0f, Value - amount);
            UpdateShaken();
        }

        public void OnDamaged()
        {
            var health = _ai.Health;
            if (_hurtCounted || health.MaxHealth <= 0f || health.Health / health.MaxHealth > LowHealth) return;

            _hurtCounted = true;
            Value = Mathf.Max(0f, Value - Profile.MoraleLossHurt);
            UpdateShaken();
        }

        public void Reset()
        {
            Value        = 1f;
            IsShaken     = false;
            _hurtCounted = false;
        }

        // The director's pacing moves the break point: harder to shake at the peak, quicker to
        // fall back in the calm. Rallying stays where it is (kept above the break point).
        private void UpdateShaken()
        {
            float retreatBelow = Mathf.Min(Profile.RetreatBelow * EnemyPacing.Current.FallBackScale, Profile.RallyAbove - 0.05f);
            if (!IsShaken && Value < retreatBelow) IsShaken = true;
            else if (IsShaken && Value >= Profile.RallyAbove) IsShaken = false;
        }
    }
}
