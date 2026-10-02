using UnityEngine;

namespace CGD.Map
{
    // How the anomaly has split the ship between its realities (factions) on one map: one
    // infesting nearly everything, an even split, broken-up contested territories, or rooms
    // flipped to a random reality with no territories at all. Every room is always one of them.
    // Content lists several by weight (each map rolls one); a run modifier can force one.
    [CreateAssetMenu(fileName = "MapFactionMix", menuName = "CGD/Map/Faction Mix")]
    public class MapFactionMix : ScriptableObject
    {
        [SerializeField] private string _displayName = "Faction Mix";
        [Tooltip("0 = every faction gets an equal share of the rooms. 1 = one faction, picked per map, holds nearly all of them")]
        [SerializeField, Range(0f, 1f)] private float _dominance;
        [Tooltip("Chance each room ignores the territories and belongs to a random faction. 1 = fully random")]
        [SerializeField, Range(0f, 1f)] private float _scatter = 0.1f;
        [Tooltip("Separate territories each faction grows from. More = smaller, broken-up territories")]
        [SerializeField, Min(1)] private int _pocketsPerFaction = 1;

        public string DisplayName       => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public float  Dominance         => _dominance;
        public float  Scatter           => _scatter;
        public int    PocketsPerFaction => Mathf.Max(1, _pocketsPerFaction);
    }
}
