using System;
using UnityEngine;

namespace CGD.Map
{
    // How well two room categories sit next to each other. 1 = neutral, above 1 = they
    // like each other's company (casino beside restaurant), below 1 = they clash
    // (accommodation beside engineering). Order doesn't matter. Pairs not listed are neutral.
    [Serializable]
    public class CategoryAffinity
    {
        [SerializeField] private RoomCategory _a = RoomCategory.Accommodation;
        [SerializeField] private RoomCategory _b = RoomCategory.Engineering;
        [Tooltip("Multiplier on a category's chance beside the other. 0.1 = rarely, 1 = neutral, 2 = often")]
        [SerializeField, Min(0f)] private float _affinity = 1f;

        public RoomCategory A        => _a;
        public RoomCategory B        => _b;
        public float        Affinity => _affinity;

        public bool IsPair(RoomCategory x, RoomCategory y) => (_a == x && _b == y) || (_a == y && _b == x);
    }
}
