using System;
using UnityEngine;

namespace CGD.Map
{
    // Tuning for Organic growth (MapLayoutSettings.Growth). Room count still comes from the
    // main path length plus the optional rooms, the Boss's distance from Min Boss Depth.
    [Serializable]
    public class MapOrganicSettings
    {
        [Tooltip("Chance a new room also links to each other room it lands beside, making a crossing. 0 = a pure tree, 1 = every neighbour linked. High values pull everything close to Start")]
        [SerializeField, Range(0f, 1f)] private float _intersectionChance = 0.3f;
        [Tooltip("0 = rooms grow out at the edges (long, sprawling arms), 1 = rooms fill in gaps (a dense block)")]
        [SerializeField, Range(0f, 1f)] private float _compactness = 0.4f;

        public float IntersectionChance => _intersectionChance;
        public float Compactness        => _compactness;
    }
}
