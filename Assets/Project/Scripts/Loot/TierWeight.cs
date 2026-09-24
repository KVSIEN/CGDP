using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Loot
{
    // Rarity odds for gear rolled by a LootTable, e.g. Common 60 / Uncommon 25 / Rare 10 /
    // Epic 4 / Legendary 1.
    [Serializable]
    public struct TierWeight
    {
        public ItemTier Tier;
        [Min(0f)] public float Weight;
    }
}
