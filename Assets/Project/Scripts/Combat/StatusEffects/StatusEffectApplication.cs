using System;
using UnityEngine;

namespace CGD.Combat
{
    // An on-hit entry on a weapon, melee step, grenade, projectile or ability. Point it at the
    // shared effect asset (Bleed, Slow, Stun...) and set how strong and how long this source makes
    // it here, rather than making an asset per source.
    [Serializable]
    public class StatusEffectApplication
    {
        public StatusEffect Effect;
        [Tooltip("Chance per hit to apply the effect")]
        [Range(0f, 1f)] public float Chance = 1f;
        [Tooltip("How strong this source makes the effect: 1 = as the asset is tuned, 0.5 = half (its damage, its slow...)")]
        [Min(0f)] public float Potency = 1f;
        [Tooltip("Seconds the effect lasts from this source; 0 = the asset's own Duration")]
        [Min(0f)] public float Duration = 0f;

        public StatusDose Dose => new(Potency, Duration);

        // Both lists as one, for a hit that carries effects from two places (an ability's own and
        // its bonus ones). Returns either list as it is when the other is empty.
        public static StatusEffectApplication[] Combine(StatusEffectApplication[] first, StatusEffectApplication[] second)
        {
            if (second == null || second.Length == 0) return first;
            if (first == null || first.Length == 0) return second;

            var all = new StatusEffectApplication[first.Length + second.Length];
            first.CopyTo(all, 0);
            second.CopyTo(all, first.Length);
            return all;
        }
    }
}
