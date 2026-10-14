using System;
using UnityEngine;

namespace CGD.Loot
{
    // One subtype a container can roll. A null Filter is the plain container, with no subtype.
    [Serializable]
    public struct WeightedLootFilter
    {
        public LootFilter Filter;
        [Min(0f)] public float Weight;
    }
}
