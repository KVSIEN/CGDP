using System;
using UnityEngine;
using CGD.Combat;
using CGD.Feedback;

namespace CGD.Weapons
{
    // What an incoming hit does against the player's guard: nothing for hits without an
    // attacker, a parry (the attacker is staggered up close, a shot is deflected), a block,
    // or a broken guard. MeleeController stays the IDamageInterceptor and hands each hit here.
    public sealed class ParryResponse
    {
        private readonly MeleeGuard     _guard;
        private readonly Reflector      _reflector;
        private readonly ReflectProfile _rangedParryReflect;
        private readonly float          _meleeParryRange;
        private readonly Func<float, bool> _payStamina;

        // `meleeParryRange`: a parried hit from farther than this was a shot, so it is
        // deflected (and sent back by `rangedParryReflect`, if set) instead of staggering.
        public ParryResponse(MeleeGuard guard, Reflector reflector, ReflectProfile rangedParryReflect,
                             float meleeParryRange, Func<float, bool> payStamina)
        {
            _guard              = guard;
            _reflector          = reflector;
            _rangedParryReflect = rangedParryReflect;
            _meleeParryRange    = meleeParryRange;
            _payStamina         = payStamina;
        }

        // Returns the damage that gets through. `parried` is true when the guard parried the
        // hit; `parryData` is the weapon that opened the parry window.
        public float Resolve(in DamageInfo info, float amount, Transform self, Vector3 aimForward,
                             MeleeWeaponData parryData, out bool parried)
        {
            parried = false;

            // Only hits with an attacker can be parried or guarded; status ticks and hazards go through.
            GameObject attacker = info.Source.Owner;
            if (attacker == null) return amount;

            Vector3 toAttacker = attacker.transform.position - self.position;
            GuardOutcome outcome = _guard.Resolve(Time.time, aimForward, toAttacker, amount, _payStamina, out float through);
            switch (outcome)
            {
                case GuardOutcome.Parried:
                    if (toAttacker.magnitude > _meleeParryRange) Deflect(info, amount, attacker);
                    else                                          Stagger(info, amount, attacker, parryData);
                    parried = true;
                    break;
                case GuardOutcome.Broken:
                    FeedbackBus.Notify("Guard broken", NotificationStyle.Danger);
                    break;
            }
            return through;
        }

        // A parried strike up close staggers the attacker, and the parrying weapon's own parry
        // reflect (a sword's riposte) answers it.
        private void Stagger(in DamageInfo info, float amount, GameObject attacker, MeleeWeaponData parryData)
        {
            Stunnable stunnable = attacker.GetComponentInParent<Stunnable>();
            if (stunnable != null) stunnable.ApplyStun(parryData.Guard.ParryStun);
            if (parryData.ParryReflect != null && _reflector != null)
                _reflector.Release(parryData.ParryReflect, info, amount, attacker);
            FeedbackBus.Notify("Parried!", NotificationStyle.Success);
        }

        // A parried shot is knocked away, and sent back when a ranged parry reflect is set.
        private void Deflect(in DamageInfo info, float amount, GameObject attacker)
        {
            if (_rangedParryReflect != null && _reflector != null && !info.IsReflected)
                _reflector.Release(_rangedParryReflect, info, amount, attacker);
            else
                FeedbackBus.Notify("Parried!", NotificationStyle.Success);
        }
    }
}
