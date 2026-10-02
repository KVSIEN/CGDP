using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // The shape of a map: how many rooms, how the main path runs, and how branches, loops
    // and gates connect everything. Says nothing about what the rooms are — that is
    // MapContentSettings — so any layout can be paired with any content in a
    // MapGenerationSettings style.
    //
    // One generator covers a wide range of shapes — a straight corridor, a branching
    // spine, a hub around Start, a winding web — so a new kind of map is a new asset
    // rather than new code.
    [CreateAssetMenu(fileName = "MapLayoutSettings", menuName = "CGD/Map/Map Layout Settings")]
    public class MapLayoutSettings : ScriptableObject
    {
        // Start, Boss and Exit.
        private const int StructuralRooms = 3;

        [Header("Size")]
        [Tooltip("Rooms off the main path: branches, hub wings and side areas. The main path, Start, Boss and Exit come on top")]
        [SerializeField] private IntRange _optionalRooms = new(6, 10);
        [Tooltip("Fewest connections from Start to the Boss on any route, shortcuts included. Loops and shortcuts that would cut below it are skipped, and a shorter main path is lengthened to reach it")]
        [SerializeField, Min(2)] private int _minBossDepth = 5;
        [Tooltip("Rows of rooms allowed above and below Start's. 0 = unlimited. Low values pack rooms into tangled maps; high values let them sprawl")]
        [SerializeField, Min(0)] private int _maxSpread;
        [SerializeField, Min(2)] private int _maxConnectionsPerNode = 4;

        [Header("Structure")]
        [SerializeField] private MapPathSettings   _mainPath = new();
        [SerializeField] private MapBranchSettings _branches = new();
        [SerializeField] private MapLoopSettings   _loops    = new();
        [SerializeField] private MapGateSettings   _gates    = new();

        public IntRange OptionalRooms         => _optionalRooms;
        public int      MinBossDepth          => _minBossDepth;
        public int      MaxSpread             => _maxSpread;
        public int      MaxConnectionsPerNode => _maxConnectionsPerNode;

        public MapPathSettings   MainPath => _mainPath;
        public MapBranchSettings Branches => _branches;
        public MapLoopSettings   Loops    => _loops;
        public MapGateSettings   Gates    => _gates;

        // Rooms between Start and the Boss for a rolled path length, lengthened when
        // needed to keep the Boss MinBossDepth away.
        public int MainPathLength(int rolled) => Mathf.Max(1, rolled, _minBossDepth - 1);

        // Total rooms a map can have, Start, Boss and Exit included — unless the grid gets
        // too crowded to fit every optional room (the generator warns when that happens).
        public int MinRoomCount => StructuralRooms + MainPathLength(_mainPath.Length.Min) + _optionalRooms.Min;
        public int MaxRoomCount => StructuralRooms + MainPathLength(_mainPath.Length.Max) + _optionalRooms.Max;
    }
}
