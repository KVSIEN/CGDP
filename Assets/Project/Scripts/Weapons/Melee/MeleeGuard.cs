using UnityEngine;

namespace CGD.Weapons
{
    // The rules of a raised melee guard, Unity-independent apart from vector maths.
    // A guard only covers hits from in front (within Arc); a hit that lands within the
    // parry window after raising it is parried; otherwise it is blocked, at a stamina
    // cost per point of damage stopped — if that can't be paid, the guard breaks.
    public class MeleeGuard
    {
        private readonly GuardSettings _settings;
        private float _raisedAt = float.NegativeInfinity;

        public MeleeGuard(GuardSettings settings) => _settings = settings;

        public bool IsRaised { get; private set; }

        public void Raise(float time)
        {
            if (IsRaised) return;
            IsRaised  = true;
            _raisedAt = time;
        }

        public void Lower() => IsRaised = false;

        // `toAttacker`: direction from the defender to the attacker (any length);
        // `facing`: the defender's forward. `canPay(cost)` spends stamina, true if paid.
        public GuardOutcome Resolve(float time, Vector3 facing, Vector3 toAttacker, float damage,
                                    System.Func<float, bool> canPay, out float damageThrough)
        {
            damageThrough = damage;
            if (!IsRaised || !Covers(facing, toAttacker)) return GuardOutcome.Open;

            if (time - _raisedAt <= _settings.ParryWindow)
            {
                damageThrough = 0f;
                return GuardOutcome.Parried;
            }

            float stopped = damage * (1f - _settings.BlockDamageMultiplier);
            if (canPay != null && !canPay(stopped * _settings.StaminaPerDamage))
            {
                Lower();
                return GuardOutcome.Broken;
            }

            damageThrough = damage - stopped;
            return GuardOutcome.Blocked;
        }

        // Flat angle on the horizontal plane; hits with no known direction count as frontal.
        private bool Covers(Vector3 facing, Vector3 toAttacker)
        {
            facing.y = 0f; toAttacker.y = 0f;
            if (toAttacker.sqrMagnitude < 0.0001f || facing.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(facing, toAttacker) <= _settings.ArcDeg * 0.5f;
        }
    }
}
