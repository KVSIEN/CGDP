using System;
using UnityEngine;

namespace CGD.Map
{
    // How a run modifier changes one room type's rule (see MapRunModifier).
    [Serializable]
    public class MapTypeAdjustment
    {
        public MapNodeType Type = MapNodeType.Treasure;
        [Tooltip("Multiplies the rule's fill weight")]
        [Min(0f)] public float WeightMultiplier = 1f;
        [Tooltip("Added to the rule's minimum and maximum")]
        public int ExtraMin;
        public int ExtraMax;
    }
}
