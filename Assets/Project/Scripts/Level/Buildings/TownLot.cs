using System;
using UnityEngine;

namespace CGD.Level
{
    // One kind of building a town may put on a lot, and how often relative to the others.
    [Serializable]
    public class TownLot
    {
        [SerializeField] private BuildingPlan _plan;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public BuildingPlan Plan   => _plan;
        public float        Weight => _weight;
    }
}
