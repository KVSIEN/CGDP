using System;
using UnityEngine;

namespace CGD.Combat
{
    // An on-hit entry on a weapon, melee step, grenade or projectile.
    [Serializable]
    public class StatusEffectApplication
    {
        public StatusEffect Effect;
        [Tooltip("Chance per hit to apply the effect")]
        [Range(0f, 1f)] public float Chance = 1f;

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
