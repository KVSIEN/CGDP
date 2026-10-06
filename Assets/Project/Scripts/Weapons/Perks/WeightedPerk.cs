using System;
using UnityEngine;

namespace CGD.Weapons
{
    [Serializable]
    public struct WeightedPerk
    {
        public WeaponPerk Perk;
        [Min(0f)] public float Weight;
    }
}
