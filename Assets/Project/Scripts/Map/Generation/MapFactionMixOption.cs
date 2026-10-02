using System;
using UnityEngine;

namespace CGD.Map
{
    // One faction mix MapContentSettings can roll, and how often.
    [Serializable]
    public class MapFactionMixOption
    {
        [SerializeField] private MapFactionMix _mix;
        [Tooltip("Relative chance this mix is picked. 0 = never, unless every weight is 0")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        // Used by the Inspector when an option is added to the list.
        public MapFactionMixOption() { }

        public MapFactionMixOption(MapFactionMix mix, float weight)
        {
            _mix    = mix;
            _weight = weight;
        }

        public MapFactionMix Mix    => _mix;
        public float         Weight => _weight;
    }
}
