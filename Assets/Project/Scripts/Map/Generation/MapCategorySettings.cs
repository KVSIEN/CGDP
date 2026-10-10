using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // What kind of ship space each room is, and what may sit beside what. The generator
    // first splits the map into zone areas (Civilian, Operations, Services), then picks each
    // room's category from its zone, weighing in how well it suits the rooms already beside
    // it. Every rule is a preference, not a ban, so an unlikely neighbour still turns up now
    // and then — the ship has been warped for a decade. Leave the content's Categories empty
    // to skip all of this and leave rooms uncategorised.
    [CreateAssetMenu(fileName = "MapCategorySettings", menuName = "CGD/Map/Map Category Settings")]
    public class MapCategorySettings : ScriptableObject
    {
        [Header("Zones")]
        [SerializeField] private List<CategoryZoneRule> _zones = new();
        [Tooltip("Multiplier on a category picked outside the room's own zone. Small but above 0, so odd neighbours stay possible")]
        [SerializeField, Range(0f, 1f)] private float _outOfZoneWeight = 0.08f;

        [Header("Categories")]
        [SerializeField] private List<CategoryRule> _categories = new();
        [Tooltip("Multiplier on a category inside one of its preferred sections")]
        [SerializeField, Min(1f)] private float _sectionPreference = 2f;
        [Tooltip("Each category already used makes it this much likelier to be skipped next time (1 = no effect), so a zone doesn't become one category")]
        [SerializeField, Range(0.1f, 1f)] private float _repeatDecay = 0.8f;

        [Header("Fixed by node type")]
        [Tooltip("Node types that are always one category, whatever zone they fall in")]
        [SerializeField] private List<TypeCategory> _fixedCategories = new();

        [Header("Neighbours")]
        [SerializeField] private List<CategoryAffinity> _affinities = new();
        [Tooltip("Affinity of two neighbours of the same category")]
        [SerializeField, Min(0f)] private float _sameCategoryAffinity = 1.3f;
        [Tooltip("Direct doors join rooms that touch, so affinities count this many times over (an exponent)")]
        [SerializeField, Min(1f)] private float _directDoorStrength = 2f;
        [Tooltip("The validator flags neighbours whose affinity is below this")]
        [SerializeField, Range(0f, 1f)] private float _clashBelow = 0.25f;

        public IReadOnlyList<CategoryZoneRule> Zones   => _zones;
        public float OutOfZoneWeight   => _outOfZoneWeight;
        public float RepeatDecay       => _repeatDecay;
        public float DirectDoorStrength => _directDoorStrength;
        public float ClashBelow        => _clashBelow;

        // Categories with a weight above 0, each once.
        public void CollectEnabled(List<RoomCategory> results)
        {
            results.Clear();
            foreach (CategoryRule rule in _categories)
                if (rule != null && rule.Category != RoomCategory.None && rule.Weight > 0f && !results.Contains(rule.Category))
                    results.Add(rule.Category);
        }

        public bool HasEnabledCategory(ShipZone zone)
        {
            foreach (CategoryRule rule in _categories)
                if (rule != null && rule.Weight > 0f && rule.Category.Zone() == zone) return true;
            return false;
        }

        // Base chance of a category in a room of `section` (null = no section).
        public float WeightOf(RoomCategory category, MapSectionDefinition section)
        {
            foreach (CategoryRule rule in _categories)
            {
                if (rule == null || rule.Category != category) continue;
                return rule.Prefers(section) ? rule.Weight * _sectionPreference : rule.Weight;
            }
            return 0f;
        }

        // RoomCategory.None when the type isn't fixed to a category.
        public RoomCategory FixedFor(MapNodeType type)
        {
            foreach (TypeCategory fixedCategory in _fixedCategories)
                if (fixedCategory != null && fixedCategory.Type == type) return fixedCategory.Category;
            return RoomCategory.None;
        }

        // How well two categories sit side by side: 1 = neutral.
        public float Affinity(RoomCategory a, RoomCategory b)
        {
            if (a == b) return _sameCategoryAffinity;
            foreach (CategoryAffinity pair in _affinities)
                if (pair != null && pair.IsPair(a, b)) return pair.Affinity;
            return 1f;
        }
    }
}
