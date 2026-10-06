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

        [Header("Corners")]
        [Tooltip("Tiles cut off each outside room corner at 45° (0 = sharp). Room functions can override it")]
        [SerializeField, Range(0, RoomOutline.MaxChamfer)] private int _roomChamfer = 1;
        [Tooltip("Rounding passes for curved rooms (domes, rings): 0 = 45° walls, more = smoother arcs of shorter wall pieces")]
        [SerializeField, Range(0, RoomOutline.MaxSmoothing)] private int _curveSmoothing = 2;
        [Tooltip("Cut the outside corner of every corridor bend at 45°")]
        [SerializeField] private bool _chamferCorridorBends = true;

        [Header("Wall Kit")]
        [Tooltip("Art placed along every wall (straight pieces, bevels, posts, door frames). The box walls stay as invisible collision. Empty = plain box walls")]
        [SerializeField] private WallKit _wallKit;
        [Tooltip("Kits for rooms a faction holds, instead of the one above (corridors always use it)")]
        [SerializeField] private List<FactionWallKit> _factionWallKits = new();

        [Header("Ceilings")]
        [Tooltip("Roof every room and corridor at its wall height. Interiors then need their own lights")]
        [SerializeField] private bool _buildCeilings;
        [SerializeField] private Material _ceilingMaterial;
        [Tooltip("Keep this layer out of the world map's capture mask, or the top-down snapshot sees only roofs")]
        [SerializeField] private int _ceilingLayer;

        [Header("Doors")]
        [Tooltip("Origin at the doorway's centre on the floor, +Z pointing out of the room. Optional for normal connections")]
        [SerializeField] private GameObject _doorPrefab;
        [Tooltip("Gate for Keycard locks (e.g. a Door with a Key set)")]
        [SerializeField] private GameObject _lockedDoorPrefab;
        [Tooltip("Gate for Terminal locks: a Door with a ConditionLock as its Condition. Empty = the locked door prefab")]
        [SerializeField] private GameObject _terminalDoorPrefab;
        [Tooltip("Gate for Secret connections (e.g. a Destructible fake wall)")]
        [SerializeField] private GameObject _secretDoorPrefab;
        [Tooltip("Door for one-way shortcuts; needs a Door (on the root or a child), which the builder bars from the near side. Empty = the normal door prefab")]
        [SerializeField] private GameObject _oneWayDoorPrefab;
        [Tooltip("Placed in the room the map graph picks for each Locked gate's key (e.g. an ItemPickup of the item the locked door asks for). Empty = no keys are placed")]
        [SerializeField] private GameObject _keyPrefab;
        [Tooltip("Placed in each room a Terminal lock picks (a LockTerminal), bound to that gate's ConditionLock. Empty = no terminals are placed")]
        [SerializeField] private GameObject _terminalPrefab;

        [Header("Door Signs")]
        [Tooltip("A bar over each doorway in the colour of the room it leads to")]
        [SerializeField] private bool _buildDoorSigns = true;
        [Tooltip("Copied and tinted per room type (emission is turned on). Empty = the floor material")]
        [SerializeField] private Material _doorSignMaterial;
        [Tooltip("Rooms at or above this intensity get a danger marker on their door signs (tier-3 rooms and the Boss always do)")]
        [SerializeField, Range(0f, 1f)] private float _dangerIntensity = 0.75f;

        [Header("Room tiers")]
        [Tooltip("Enemy tier odds in rooms of tier 1, 2 and 3 (map node tier)")]
        [SerializeField] private EnemyTierOdds[] _enemyTierOdds = { new(0.1f, 0f), new(0.45f, 0.1f), new(0.45f, 0.45f) };
        [Tooltip("Extra loot luck on containers and rewards in rooms of tier 1, 2 and 3 (slightly better odds higher up)")]
        [SerializeField] private float[] _lootLuckByTier = { 0f, 0.5f, 1f };

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
        public int   RoomChamfer    => _roomChamfer;
        public bool  ChamferCorridorBends => _chamferCorridorBends;
        public int   CurveSmoothing => _curveSmoothing;
        public WallKit WallKit      => _wallKit;

        // The kit a room's walls use: its faction's, else the default. Null room = a corridor.
        public WallKit WallKitFor(LevelRoom room)
        {
            if (_wallKit == null) return null;
            if (room?.Faction == null) return _wallKit;

            FactionWallKit match = _factionWallKits.Find(k => k != null && k.Faction == room.Faction && k.Kit != null);
            return match != null ? match.Kit : _wallKit;
        }
        public bool  BuildCeilings  => _buildCeilings;
        public Material CeilingMaterial => _ceilingMaterial != null ? _ceilingMaterial : _wallMaterial;
        public int   CeilingLayer   => _ceilingLayer;
        public int   SpawnMarginTiles => _spawnMarginTiles;
        public int   MinRoomWidthTiles => _minRoomWidthTiles;

        public GameObject DoorPrefabFor(MapConnection connection) => connection.Type switch
        {
            ConnectionType.Locked when connection.Lock == MapLockKind.Terminals && _terminalDoorPrefab != null => _terminalDoorPrefab,
            ConnectionType.Locked => _lockedDoorPrefab,
            ConnectionType.Secret => _secretDoorPrefab,
            _ when connection.OneWay && _oneWayDoorPrefab != null => _oneWayDoorPrefab,
            _                     => _doorPrefab,
        };

        public GameObject KeyPrefab      => _keyPrefab;
        public GameObject TerminalPrefab => _terminalPrefab;

        public bool     BuildDoorSigns   => _buildDoorSigns;
        public Material DoorSignMaterial => _doorSignMaterial != null ? _doorSignMaterial : _floorMaterial;
        public float    DangerIntensity  => _dangerIntensity;

        public RoomContentRule RuleFor(MapNodeType type) => _rooms.Find(r => r.Type == type);

        public EnemyTierOdds EnemyTierOddsFor(int roomTier) =>
            _enemyTierOdds.Length == 0 ? default : _enemyTierOdds[Mathf.Clamp(roomTier - 1, 0, _enemyTierOdds.Length - 1)];

        public float LootLuckFor(int roomTier) =>
            _lootLuckByTier.Length == 0 ? 0f : _lootLuckByTier[Mathf.Clamp(roomTier - 1, 0, _lootLuckByTier.Length - 1)];

        public IReadOnlyList<RoomShape> DefaultShapes => _shapes;

        public IReadOnlyList<RoomFunction> FunctionsFor(MapNodeType type) =>
            (IReadOnlyList<RoomFunction>)RuleFor(type)?.Functions ?? Array.Empty<RoomFunction>();
    }
}
