using System;
using UnityEngine;

namespace CGD.Weapons
{
    // What one draw orientation does to a bow, as multipliers on its rolled stats.
    [Serializable]
    public struct DrawStance
    {
        [Tooltip("× the time to full draw (below 1 = faster)")]
        [Min(0.1f)] public float ChargeTimeMultiplier;
        [Tooltip("× damage per arrow")]
        [Min(0f)]   public float DamageMultiplier;
        [Tooltip("× the spread cone (below 1 = tighter)")]
        [Min(0f)]   public float SpreadMultiplier;

        public DrawStance(float chargeTime, float damage, float spread)
        {
            ChargeTimeMultiplier = chargeTime;
            DamageMultiplier     = damage;
            SpreadMultiplier     = spread;
        }

        public static DrawStance Neutral => new(1f, 1f, 1f);
    }
}
