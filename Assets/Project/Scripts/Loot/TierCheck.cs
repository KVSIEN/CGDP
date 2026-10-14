using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Loot
{
    // One step of a LootTable's rarity checks, which run from the highest tier down: the chance of
    // this tier given no higher one hit, e.g. Legendary 1% → Epic 5% → Rare 15% → Uncommon 35% →
    // Common 100%. A tier can have its own pool, so what drops can depend on the tier it lands on.
    [Serializable]
    public struct TierCheck
    {
        public ItemTier Tier;
        [Tooltip("Chance of this tier when no higher tier hit. Give the lowest tier 100% so every drop gets one")]
        [Range(0f, 1f)] public float Chance;
        [Tooltip("Optional: gear that lands on this tier is rolled from this table instead, all at this tier (e.g. Legendary-only uniques). Empty = the drawn item at this tier")]
        public LootTable Pool;
    }
}
