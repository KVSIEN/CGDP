using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Meters;

namespace CGD.Abilities
{
    // Optional on any ability with a resource cost: spend more than the cost (everything up
    // to MaxSpend) for a stronger cast, and add status effects past set amounts. Damage
    // abilities (Targeted, Shockwave, Projectile, Timeline) read the result from the context.
    [Serializable]
    public class ResourceScaling
    {
        [Tooltip("Most it can spend in one cast (at or below the cost = off: always the cost)")]
        [Min(0f)] public float MaxSpend;
        [Tooltip("Damage multiplier when spending MaxSpend (1× at the cost)")]
        [Min(1f)] public float MaxPower = 1f;
        [Tooltip("Status effects added to the cast's hits once it spends at least the given amount")]
        public SpendBonus[] Bonuses = Array.Empty<SpendBonus>();

        public bool IsActive(MeterCost cost) => cost.Meter != null && MaxSpend > cost.Amount;

        // On-hit effects unlocked by spending `spent`; null when none.
        public StatusEffectApplication[] EffectsFor(float spent)
        {
            List<StatusEffectApplication> effects = null;
            foreach (SpendBonus bonus in Bonuses)
            {
                if (bonus.Effect == null || bonus.Effect.Effect == null || spent < bonus.AtSpend) continue;
                (effects ??= new List<StatusEffectApplication>()).Add(bonus.Effect);
            }
            return effects?.ToArray();
        }
    }
}
