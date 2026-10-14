using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Perks
{
    [Serializable]
    public struct WeightedPerk
    {
        public GearPerk Perk;
        [Min(0f)] public float Weight;
        [Tooltip("Lowest tier this perk can roll on, e.g. Legendary for a run-defining perk like Magnetized")]
        public ItemTier MinTier;
    }
}
