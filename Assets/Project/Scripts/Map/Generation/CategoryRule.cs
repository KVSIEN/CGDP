using System;
using UnityEngine;

namespace CGD.Map
{
    // How likely one room category is to be picked. Weight 0 keeps a category out of the
    // map, for categories no room function exists for yet.
    [Serializable]
    public class CategoryRule
    {
        [SerializeField] private RoomCategory _category = RoomCategory.Accommodation;
        [Tooltip("Relative chance among the categories of its zone. 0 = never picked")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Ship sections where this category is likelier (crew quarters in Habitation, engineering in Engineering)")]
        [SerializeField] private MapSectionDefinition[] _preferredSections = Array.Empty<MapSectionDefinition>();

        public RoomCategory Category => _category;
        public float        Weight   => _weight;

        public bool Prefers(MapSectionDefinition section) =>
            section != null && Array.IndexOf(_preferredSections, section) >= 0;
    }
}
