using System;
using UnityEngine;

namespace CGD.Map
{
    // Rhythm along the main path, on top of the intensity curve: no endless chains of
    // fights (MapNodeTypeExtensions.IsFight), and a sawtooth of peaks the intensity painter
    // raises and the map director builds as set pieces (MapPacing). Per-type spacing lives on
    // MapNodeTypeRule.
    [Serializable]
    public class MapPacingSettings
    {
        // For a graph without content: the defaults below.
        public static readonly MapPacingSettings Default = new();

        [Tooltip("Most fights in a row on the main path. 0 = no limit")]
        [SerializeField, Min(0)] private int _maxCombatInARow = 3;
        [Tooltip("Most calm rooms in a row on the main path: where another would make it more, rooms are filled with a fight instead. Guaranteed rooms, pins and each type's minimum come first. 0 = no limit")]
        [SerializeField, Min(0)] private int _maxCalmInARow = 3;
        [Tooltip("Used for a room no rule can take without breaking the pacing")]
        [SerializeField] private MapNodeType _restType = MapNodeType.Event;
        [Tooltip("A fight this intense or more, and more intense than every fight next to it, is the peak of its stretch of map: the big set-piece fight")]
        [SerializeField, Range(0f, 1f)] private float _peakIntensity = 0.6f;
        [Tooltip("Most fights in a row on the main path with no peak among them: the generator raises the strongest of a longer run into one, and gives a way to the boss with two fights or more a peak (0 = neither)")]
        [SerializeField, Range(0, 8)] private int _maxFightsWithoutPeak = 3;

        public MapPacingSettings() { }

        public MapPacingSettings(int maxCombatInARow = 3, float peakIntensity = 0.6f, int maxFightsWithoutPeak = 3, MapNodeType restType = MapNodeType.Event,
                                 int maxCalmInARow = 3)
        {
            _maxCombatInARow      = maxCombatInARow;
            _maxCalmInARow        = maxCalmInARow;
            _peakIntensity        = peakIntensity;
            _maxFightsWithoutPeak = maxFightsWithoutPeak;
            _restType             = restType;
        }

        public int         MaxCombatInARow      => _maxCombatInARow;
        public int         MaxCalmInARow        => _maxCalmInARow;
        public MapNodeType RestType             => _restType;
        public float       PeakIntensity        => _peakIntensity;
        public int         MaxFightsWithoutPeak => _maxFightsWithoutPeak;
    }
}
