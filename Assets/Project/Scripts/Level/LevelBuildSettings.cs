using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // How a MapGraph becomes geometry: tile and room sizes, how far apart rooms sit, room
    // shapes, what the walls look like, which door goes on each kind of connection, and what each room
    // type is filled with.
    [CreateAssetMenu(fileName = "LevelBuildSettings", menuName = "CGD/Level/Level Build Settings")]
    public class LevelBuildSettings : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Metres per tile. Corridors are one tile wide, so this is also the corridor width")]
        [SerializeField, Min(1f)] private float _tileSize = 3f;
        [Tooltip("Room width and depth, in tiles")]
        [SerializeField, Min(4)] private int _roomTiles = 12;
        [Tooltip("Tiles between neighbouring rooms, where corridors run")]
        [SerializeField, Min(4)] private int _gapTiles = 6;

        [Header("Geometry")]
        [SerializeField, Min(1f)] private float _wallHeight = 4f;
        [SerializeField, Min(0.05f)] private float _wallThickness = 0.3f;
        [SerializeField, Min(0.05f)] private float _floorThickness = 0.2f;
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _corridorFloorMaterial;
        [SerializeField] private Material _wallMaterial;
        [Tooltip("Layer the generated floors and walls go on (should be in enemies' obstacle mask)")]
        [SerializeField] private int _geometryLayer;

        [Header("Doors")]
        [Tooltip("Origin at the doorway's centre on the floor, +Z pointing out of the room. Optional for normal connections")]
        [SerializeField] private GameObject _doorPrefab;
        [Tooltip("Gate for Locked connections (e.g. a Door with a Key set)")]
        [SerializeField] private GameObject _lockedDoorPrefab;
        [Tooltip("Gate for Secret connections (e.g. a Destructible fake wall)")]
        [SerializeField] private GameObject _secretDoorPrefab;

        [Header("Rooms")]
        [SerializeField] private List<RoomContentRule> _rooms = new();
        [Tooltip("Floor plans for rooms without a function, or whose function lists none. Empty = square rooms")]
        [SerializeField] private List<RoomShape> _shapes = new();
        [Tooltip("Narrowest passage a generated room may have, in tiles")]
        [SerializeField, Min(1)] private int _minRoomWidthTiles = 2;
        [Tooltip("Keeps spawned enemies and props this many tiles away from the walls")]
        [SerializeField, Min(0)] private int _spawnMarginTiles = 1;

        public float TileSize       => _tileSize;
        public int   RoomTiles      => _roomTiles;
        public int   GapTiles       => _gapTiles;
        public int   CellTiles      => _roomTiles + _gapTiles;
        public float WallHeight     => _wallHeight;
        public float WallThickness  => _wallThickness;
        public float FloorThickness => _floorThickness;
        public Material FloorMaterial         => _floorMaterial;
        public Material CorridorFloorMaterial => _corridorFloorMaterial != null ? _corridorFloorMaterial : _floorMaterial;
        public Material WallMaterial          => _wallMaterial;
        public int   GeometryLayer  => _geometryLayer;
        public int   SpawnMarginTiles => _spawnMarginTiles;
        public int   MinRoomWidthTiles => _minRoomWidthTiles;

        public GameObject DoorPrefabFor(ConnectionType type) => type switch
        {
            ConnectionType.Locked => _lockedDoorPrefab,
            ConnectionType.Secret => _secretDoorPrefab,
            _                     => _doorPrefab,
        };

        public RoomContentRule RuleFor(MapNodeType type) => _rooms.Find(r => r.Type == type);

        public IReadOnlyList<RoomShape> DefaultShapes => _shapes;

        public IReadOnlyList<RoomFunction> FunctionsFor(MapNodeType type) =>
            (IReadOnlyList<RoomFunction>)RuleFor(type)?.Functions ?? Array.Empty<RoomFunction>();
    }
}
