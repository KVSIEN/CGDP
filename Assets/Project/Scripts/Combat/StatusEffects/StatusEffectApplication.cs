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
    }
}
