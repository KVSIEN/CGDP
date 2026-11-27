using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // How the map director reads a map and what it changes in each room's beat (MapDirector,
    // BeatTuning). Which rooms are peaks is the map's pacing (MapPacingSettings). Referenced from LevelBuildSettings; without it every room is built as before.
    [CreateAssetMenu(fileName = "MapDirector", menuName = "CGD/Level/Map Director")]
    public class MapDirectorSettings : ScriptableObject
    {
        [Tooltip("Most calm rooms in a row on the main path before it is reported as dragging (0 = not checked)")]
        [SerializeField, Range(0, 8)] private int _calmStretch = 3;
        [Tooltip("A peak or boss room is built up to hold more cover for its size than every fight beside it; one whose additions run out before it gets there is built again from up to this many more seeds, keeping the best")]
        [SerializeField, Range(0, 6)] private int _coverRetries = 3;
        [Tooltip("Size of a fight room at the start of the map, scaling up to Late Size by the boss (beside its beat's own size): small chambers early, bigger ones deeper in")]
        [SerializeField, Range(0.5f, 1.5f)] private float _earlySize = 0.9f;
        [Tooltip("Size of a fight room by the boss")]
        [SerializeField, Range(0.5f, 1.5f)] private float _lateSize = 1.1f;
        [Tooltip("How likely a room is to be the same place as a room next to it, against any other place (1 = no matter, near 0 = only when nothing else fits): two bars or two gyms side by side read as one room twice")]
        [SerializeField, Range(0.01f, 1f)] private float _repeatNeighbour = 0.15f;
        [Tooltip("What each beat changes. A beat not listed is left as the generator makes it")]
        [SerializeField] private List<BeatTuning> _beats = new();

        public float RepeatNeighbour => _repeatNeighbour;
        public int CalmStretch => _calmStretch;
        public int CoverRetries => _coverRetries;

        // A fight room's size for how far into the map it lies (0 = the start, 1 = the boss).
        public float SizeAt(float progress) => Mathf.Lerp(_earlySize, _lateSize, progress);

        public BeatTuning TuningFor(RoomBeat beat)
        {
            foreach (BeatTuning tuning in _beats)
                if (tuning != null && tuning.Beat == beat) return tuning;
            return BeatTuning.Neutral;
        }
    }
}
