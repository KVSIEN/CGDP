using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // How likely each enemy in a room is to be tier 2 or 3 (the rest are tier 1).
    [Serializable]
    public struct EnemyTierOdds
    {
        [Range(0f, 1f)] public float Tier2;
        [Range(0f, 1f)] public float Tier3;

        public EnemyTierOdds(float tier2, float tier3)
        {
            Tier2 = tier2;
            Tier3 = tier3;
        }

        public int Roll(RandomStream random)
        {
            float roll = random.Value;
            if (roll < Tier3) return 3;
            return roll < Tier3 + Tier2 ? 2 : 1;
        }
    }
}
