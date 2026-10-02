using System;
using UnityEngine;

namespace CGD.Map
{
    // One layout a MapGenerationSettings style can be built on, and how often.
    [Serializable]
    public class MapLayoutOption
    {
        [SerializeField] private MapLayoutSettings _layout;
        [Tooltip("Relative chance this layout is picked. 0 = never, unless every weight is 0")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        // Used by the Inspector when an option is added to the list.
        public MapLayoutOption() { }

        public MapLayoutOption(MapLayoutSettings layout, float weight)
        {
            _layout = layout;
            _weight = weight;
        }

        public MapLayoutSettings Layout => _layout;
        public float             Weight => _weight;
    }
}
