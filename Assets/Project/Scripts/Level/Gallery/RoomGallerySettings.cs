using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A walk-through of what room generation makes, built by LevelBuilder in place of a map
    // (RoomGalleryGraph). First one room of every place, the factions and ship sections taken
    // in turn so every reality and side shows up; a place that can sit on more than one side
    // of the ship is shown on each. Then a wing per faction: Combat rooms from its frontier
    // inward, then its showcase fights at full strength (a Breach room breached by the next
    // faction). Rooms stand in a grid, joined in a snake from the Start room to the Exit.
    [CreateAssetMenu(fileName = "RoomGallerySettings", menuName = "CGD/Level/Room Gallery")]
    public class RoomGallerySettings : ScriptableObject
    {
        [Tooltip("Factions and ship sections are taken from here, in turn")]
        [SerializeField] private MapContentSettings _content;
        [Tooltip("One room each, in this order. Empty = every place in the Level Build Settings' list")]
        [SerializeField] private RoomFunction[] _places = Array.Empty<RoomFunction>();
        [Tooltip("What the place rooms are: Quiet has no enemies, so each room can be walked round in peace")]
        [SerializeField] private MapNodeType _placeType = MapNodeType.Quiet;
        [Tooltip("A place that lists more than one ship side (a first-class restaurant and a crew mess) gets a room on each, side by side. Off = one room, on whichever side its turn gives it")]
        [SerializeField] private bool _everySide = true;
        [Tooltip("Each faction's wing opens with a Combat room at each of these map influences, frontier first (0.35 = frontier, 0.7 and up = core), so walking it shows the design thickening toward the full-strength rooms that follow")]
        [SerializeField] private float[] _depthSteps = { 0.35f, 0.525f };
        [Tooltip("Fight types shown at full strength in each faction's wing, in a place picked as a level would: cover dressing, concealment, the breach split")]
        [SerializeField] private MapNodeType[] _showcaseTypes = { MapNodeType.Combat, MapNodeType.Stealth, MapNodeType.Breach };
        [SerializeField, Range(1, 3)] private int _tier = 1;
        [SerializeField, Min(2)] private int _roomsPerRow = 6;
        [Tooltip("Cells from one room to the next: 3 keeps two-cell rooms and their neighbours apart")]
        [SerializeField, Min(2)] private int _cellsApart = 3;

        public MapContentSettings Content => _content;
        public IReadOnlyList<RoomFunction> Places => _places;
        public MapNodeType PlaceType => _placeType;
        public bool EverySide => _everySide;
        public IReadOnlyList<float> DepthSteps => _depthSteps;
        public IReadOnlyList<MapNodeType> ShowcaseTypes => _showcaseTypes;
        public int Tier => _tier;
        public int RoomsPerRow => _roomsPerRow;
        public int CellsApart => _cellsApart;
    }
}
