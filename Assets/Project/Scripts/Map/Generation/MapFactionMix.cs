using UnityEngine;

namespace CGD.Map
{
    // How the anomaly has split the ship between the factions on one map: one faction
    // infesting nearly everything, an even split, contested territories with no-man's land
    // between them, or rooms flipped to a random faction with no territories at all.
    // Content lists several by weight (each map rolls one); a run modifier can force one.
    [CreateAssetMenu(fileName = "MapFactionMix", menuName = "CGD/Map/Faction Mix")]
    public class MapFactionMix : ScriptableObject
    {
        [SerializeField] private string _displayName = "Faction Mix";
        [Tooltip("0 = every faction gets an equal share of the rooms. 1 = one faction, picked per map, holds nearly all of them")]
        [SerializeField, Range(0f, 1f)] private float _dominance;
        [Tooltip("Chance each room ignores the territories and belongs to a random faction. 1 = fully random")]
        [SerializeField, Range(0f, 1f)] private float _scatter = 0.1f;
        [Tooltip("Share of rooms no faction holds")]
        [SerializeField, Range(0f, 1f)] private float _unclaimed;
        [Tooltip("Separate territories each faction grows from. More = smaller, broken-up territories")]
        [SerializeField, Min(1)] private int _pocketsPerFaction = 1;

        public string DisplayName       => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public float  Dominance         => _dominance;
        public float  Scatter           => _scatter;
        public float  Unclaimed         => _unclaimed;
        public int    PocketsPerFaction => Mathf.Max(1, _pocketsPerFaction);
    }
}
