using System;
using UnityEngine;

namespace CGD.Perks
{
    [Serializable]
    public struct WeightedPerk
    {
        public GearPerk Perk;
        [Min(0f)] public float Weight;
    }
}
