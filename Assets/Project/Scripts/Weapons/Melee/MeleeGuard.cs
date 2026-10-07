using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // The rules of the player's guard and bash parry, Unity-independent apart from vector maths.
    // Blocking uses the guard in use (a shield, or the melee weapon's own); a parry uses the
    // settings of whatever bashed or struck. Both only cover hits from in front (within their
    // arc). A hit landing within the parry window of a bash starting is parried, guard raised
    // or not; otherwise a raised (or passive) guard blocks it, at a stamina cost per point of
    // damage stopped — if that can't be paid, the guard breaks.
    // Parrying is a gamble: a bash whose window closes without parrying anything leaves the
    // player exposed for a moment: no guard and no bash, so whatever comes next lands.
    public class MeleeGuard
    {
        private GuardSettings _block;
        private GuardSettings _parry;
        private float _parryUntil = float.NegativeInfinity;
        private bool  _parried;

        public bool CanBlock  { get; private set; }
        // A tower shield: blocks without being raised.
        public bool IsPassive { get; private set; }
        public bool IsRaised  { get; private set; }

        // The guard in use changed; without one nothing blocks (parries still work).
        public void SetBlock(GuardSettings? settings, bool passive)
        {
            CanBlock  = settings.HasValue;
            _block    = settings.GetValueOrDefault();
            IsPassive = CanBlock && passive;
            if (!CanBlock) IsRaised = false;
        }

        public void Raise() => IsRaised = CanBlock;

        public void Lower() => IsRaised = false;

        // A bash (or an offhand opener) started: hits in the next ParryWindow seconds are parried.
        public void OpenParry(float time, GuardSettings settings)
        {
            _parry      = settings;
            _parryUntil = time + settings.ParryWindow;
            _parried    = false;
        }

        public bool IsParrying(float time) => time <= _parryUntil;

        // Bashed too early: the window closed with nothing parried.
        public bool IsExposed(float time) =>
            !_parried && time > _parryUntil && time <= _parryUntil + _parry.WhiffExposure;

        private bool IsBlocking(float time) => CanBlock && (IsRaised || (IsPassive && !IsExposed(time)));

        // `toAttacker`: direction from the defender to the attacker (any length);
        // `facing`: the defender's forward. `canPay(cost)` spends stamina, true if paid.
        public GuardOutcome Resolve(float time, Vector3 facing, Vector3 toAttacker, float damage,
                                    System.Func<float, bool> canPay, out float damageThrough)
        {
            damageThrough = damage;
            if (IsParrying(time) && FacingArc.Contains(facing, toAttacker, _parry.ArcDeg))
            {
                _parried      = true;
                damageThrough = 0f;
                return GuardOutcome.Parried;
            }

            if (!IsBlocking(time) || !FacingArc.Contains(facing, toAttacker, _block.ArcDeg)) return GuardOutcome.Open;

            float stopped = damage * (1f - _block.BlockDamageMultiplier);
            if (canPay != null && !canPay(stopped * _block.StaminaPerDamage))
            {
                Lower();
                return GuardOutcome.Broken;
            }

            damageThrough = damage - stopped;
            return GuardOutcome.Blocked;
        }
    }
}
