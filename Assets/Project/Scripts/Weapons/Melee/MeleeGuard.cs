using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // The rules of a melee guard and bash parry, Unity-independent apart from vector maths.
    // Both only cover hits from in front (within Arc). A hit landing within the parry window
    // of a bash starting is parried, guard raised or not; otherwise a raised guard blocks it,
    // at a stamina cost per point of damage stopped — if that can't be paid, the guard breaks.
    public class MeleeGuard
    {
        private readonly GuardSettings _settings;
        private float _parryUntil = float.NegativeInfinity;

        public MeleeGuard(GuardSettings settings) => _settings = settings;

        public bool IsRaised { get; private set; }

        public void Raise() => IsRaised = true;

        public void Lower() => IsRaised = false;

        // A bash started: hits in the next ParryWindow seconds are parried.
        public void OpenParry(float time) => _parryUntil = time + _settings.ParryWindow;

        public bool IsParrying(float time) => time <= _parryUntil;

        // `toAttacker`: direction from the defender to the attacker (any length);
        // `facing`: the defender's forward. `canPay(cost)` spends stamina, true if paid.
        public GuardOutcome Resolve(float time, Vector3 facing, Vector3 toAttacker, float damage,
                                    System.Func<float, bool> canPay, out float damageThrough)
        {
            damageThrough = damage;
            bool parrying = IsParrying(time);
            if (!parrying && !IsRaised) return GuardOutcome.Open;
            if (!FacingArc.Contains(facing, toAttacker, _settings.ArcDeg)) return GuardOutcome.Open;

            if (parrying)
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
    }
}
