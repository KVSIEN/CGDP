using System.Collections.Generic;
using UnityEngine;
using CGD.Factions;
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
        [Tooltip("Structure short enough to shoot over (broken columns, wall stubs, counters), so low cover reads apart from full-height pieces. Empty = the wall material")]
        [SerializeField] private Material _lowCoverMaterial;
        [Tooltip("Layer the generated floors and walls go on (should be in enemies' obstacle mask)")]
        [SerializeField] private int _geometryLayer;

        [Header("Corners")]
        [Tooltip("Tiles cut off each outside room corner at 45° (0 = sharp). Room functions can override it")]
        [SerializeField, Range(0, RoomOutline.MaxChamfer)] private int _roomChamfer = 1;
        [Tooltip("Rounding passes for curved rooms (domes, rings): 0 = 45° walls, more = smoother arcs of shorter wall pieces. Rooms 24 tiles across or more get one more pass (up to the maximum)")]
        [SerializeField, Range(0, RoomOutline.MaxSmoothing)] private int _curveSmoothing = 2;
        [Tooltip("Cut the outside corner of every corridor bend at 45°")]
        [SerializeField] private bool _chamferCorridorBends = true;
        [Tooltip("Tiles back along each arm a corridor's square bend is cut into a 45° run, walled straight across (needs Chamfer Corridor Bends; 0 = square bends). Bends with shorter arms, or no room inside them, are cut as far as they allow, and under 2 not at all")]
        [SerializeField, Range(0, CorridorCorners.MaxSteps)] private int _diagonalCorridorSteps;

        [Header("Elevation")]
        [Tooltip("The lowest floor level a room may be sunk to by its Elevation and Terraces structure rules: -1 lets sunken floors and courts in, lower lets tiered ones go deeper; 0 keeps every room's floor at or above the doorways' level. The walls reach down this far")]
        [SerializeField, Range(RoomElevation.Lowest, 0)] private int _lowestLevel;
        [Tooltip("The highest floor level a room may be raised to by its Elevation and Terraces structure rules: 1 lets platforms and raised walks in, 2 second tiers, higher taller tiers; 0 keeps every room's floor at or below the doorways' level. A level also needs 2 m of headroom under the room's ceiling. Decks (overpasses) aren't held to this, only to the ceiling. Both 0 = every room flat, whatever its rules")]
        [SerializeField, Range(0, RoomElevation.Highest)] private int _highestLevel;
        [Tooltip("How much of a room's floor should stand off the doorways' level, and what raises or sinks it where the room's own structure rules leave it short, so most rooms have a second level")]
        [SerializeField] private ElevationBudget _elevationBudget = new();
        [Tooltip("A flight of stairs between two floor levels: made for a 3 m tile and a 1 m climb, its origin on the lower floor in the middle of the tile, climbing toward +Z. Empty = a plain ramp")]
        [SerializeField] private GameObject _stairsPrefab;
        [Tooltip("The railing along a railed ledge: made for a 3 m tile edge, its origin at its foot, running along X. Empty = a plain bar")]
        [SerializeField] private GameObject _railingPrefab;

        [Header("Wall Kit")]
        [Tooltip("Art placed along every wall (straight pieces, bevels, posts, door frames). The box walls stay as invisible collision. Empty = plain box walls")]
        [SerializeField] private WallKit _wallKit;
        [Tooltip("Kits for rooms a faction holds, instead of the one above (corridors always use it)")]
        [SerializeField] private List<FactionWallKit> _factionWallKits = new();
        [Tooltip("How each faction builds its rooms: favoured floor plans and sizes, structure, props and how crowded they are. A faction without one is built plainly")]
        [SerializeField] private List<FactionDesign> _factionDesigns = new();
        [Tooltip("What each side of the ship (guest first class, guest steerage, crew) does to its rooms, on top of the faction's design. A side without one changes nothing")]
        [SerializeField] private List<ShipSideDesign> _sideDesigns = new();

        [Header("Ceilings")]
        [Tooltip("Roof every room and corridor at its wall height. Interiors then need their own lights")]
        [SerializeField] private bool _buildCeilings;
        [SerializeField] private Material _ceilingMaterial;
        [Tooltip("Keep this layer out of the world map's capture mask, or the top-down snapshot sees only roofs")]
        [SerializeField] private int _ceilingLayer;

        [Header("Sky domes")]
        [Tooltip("Roof every open-air room (a function with Open Ceiling: parks, towns) with a simulated sky, as on a colony ship's habitat decks. Needs a Sky Dome Material")]
        [SerializeField] private bool _buildSkyDomes = true;
        [Tooltip("An opaque unlit material (Universal Render Pipeline/Unlit). Its base map is replaced by the horizon-to-zenith gradient")]
        [SerializeField] private Material _skyDomeMaterial;
        [Tooltip("Metres from the top of the room's walls to the crown of its dome")]
        [SerializeField, Min(1f)] private float _skyDomeHeight = 30f;
        [SerializeField] private Color _skyHorizon = new(0.78f, 0.89f, 1f);
        [SerializeField] private Color _skyZenith  = new(0.27f, 0.52f, 0.92f);

        [Header("Buildings")]
        [Tooltip("A light in each roofed building of a town, since its roof shuts out the sky. Rooms with ceilings need their own lights anyway")]
        [SerializeField] private bool _lightBuildings = true;
        [SerializeField] private Color _buildingLightColor = new(1f, 0.93f, 0.8f);
        [SerializeField, Min(0f)] private float _buildingLightIntensity = 2f;

        [Header("Doors")]
        [Tooltip("Origin at the doorway's centre on the floor, +Z pointing out of the room. Optional for normal connections; a faction design's door takes its place in that faction's rooms")]
        [SerializeField] private GameObject _doorPrefab;
        [Tooltip("Ordinary connections into a Stealth room (either end), where enemies mustn't get through: a Door without a DoorSensor. A faction design's stealth door takes its place in that faction's rooms. Empty = the normal door")]
        [SerializeField] private GameObject _stealthDoorPrefab;
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
        [Tooltip("Write the name of the room beyond under each sign, with its faction and tier")]
        [SerializeField] private bool _doorSignLabels = true;
        [Tooltip("Rooms at or above this intensity get a danger marker on their door signs (tier-3 rooms and the Boss always do)")]
        [SerializeField, Range(0f, 1f)] private float _dangerIntensity = 0.75f;

        [Header("Floor stripes")]
        [Tooltip("A glowing guide line down the middle of every corridor (its bends and 45° runs cut on the diagonal), and a stripe along the top of every open drop (a ledge with no railing or half wall, a cut corner's diagonal too)")]
        [SerializeField] private bool _floorStripes;
        [Tooltip("Copied and tinted in the colours below (emission is turned on). Empty = the floor material")]
        [SerializeField] private Material _stripeMaterial;
        [SerializeField] private Color _corridorStripeColor = new(0.35f, 0.75f, 1f);
        [SerializeField] private Color _ledgeStripeColor = new(1f, 0.7f, 0.15f);
        [Tooltip("Metres across")]
        [SerializeField, Range(0.05f, 0.5f)] private float _stripeWidth = 0.15f;

        [Header("Room tiers")]
        [Tooltip("Enemy tier odds in rooms of tier 1, 2 and 3 (map node tier)")]
        [SerializeField] private EnemyTierOdds[] _enemyTierOdds = { new(0.1f, 0f), new(0.45f, 0.1f), new(0.45f, 0.45f) };
        [Tooltip("Extra loot luck on containers and rewards in rooms of tier 1, 2 and 3 (slightly better odds higher up)")]
        [SerializeField] private float[] _lootLuckByTier = { 0f, 0.5f, 1f };

        [Header("Rooms")]
        [Tooltip("Every place a room can become (Lobby, Medical Bay…). A room picks one of its own category, whatever its node type")]
        [SerializeField] private List<RoomFunction> _functions = new();
        [SerializeField] private List<RoomContentRule> _rooms = new();
        [Tooltip("Floor plans for rooms without a function, or whose function lists none. Empty = square rooms")]
        [SerializeField] private List<RoomShape> _shapes = new();
        [Tooltip("Narrowest passage a generated room may have, in tiles")]
        [SerializeField, Min(1)] private int _minRoomWidthTiles = 2;
        [Tooltip("Keeps spawned enemies and props this many tiles away from the walls")]
        [SerializeField, Min(0)] private int _spawnMarginTiles = 1;
        [Tooltip("Fight rooms a faction design holds are built from this many seeds — structure, then furnishing — keeping the one closest to the faction's priorities. 1 = no steering")]
        [SerializeField, Range(1, 8)] private int _priorityTries = 4;
        [Tooltip("Each of those furnishings that misses the faction's priorities gets up to this many tries at moving one of its loose cover props elsewhere, keeping each move that brings the room closer. Cheaper than another try, and keeps what was right about the room. 0 = no moving, tries only")]
        [SerializeField, Range(0, 64)] private int _refineSteps = 24;
        [Tooltip("Which readings of a room see in every direction (what can be seen from each tile, over the floor plan) rather than along its rows and columns. Traits: walls and lines of fire, in generation and in the Room Lab. Posts: where enemies hold chokes and overwatch. Cover: where cover goes. More accurate in round and diagonal rooms, but slower, and the trait bands were tuned for the row-and-column reading (see ROOM_GENERATION.md, Sight). None = the old reading")]
        [SerializeField] private IsovistUse _isovistFor = IsovistUse.None;
        [Tooltip("Cover put just inside the doorway a fight room is entered by (its threshold, RoomFlowPlanner), so a player arrives at a foothold rather than in the open. A faction design may swap it like any dressing. Empty = none")]
        [SerializeField] private RoomDressing _footholdDressing;
        [Tooltip("Placed last in every furnished room, each piece with MinEmpty set so it only goes into patches of floor the rest of the furnishing left bare: shelves and lockers on empty wall, crates and lamps in empty corners and middles. Keeps a room from being dense in one corner and blank in the next. Empty = none")]
        [SerializeField] private RoomDressing _gapDressing;
        [Tooltip("How much structure (walls, buildings, pillars, half walls, weighted) a room should have for its size, and what is added when it has less, so a hall is never a field of bare floor")]
        [SerializeField] private StructureBudget _structureBudget = new();
        [Tooltip("How strongly a faction's design shows in a room, from how firmly the map's faction painter holds it: full in the core of a territory, thinner on its frontier")]
        [SerializeField] private TerritoryDepth _territoryDepth = new();

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
        public Material LowCoverMaterial      => _lowCoverMaterial != null ? _lowCoverMaterial : _wallMaterial;
        public int   GeometryLayer  => _geometryLayer;
        public int   RoomChamfer    => _roomChamfer;
        public bool  ChamferCorridorBends => _chamferCorridorBends;
        public int   DiagonalCorridorSteps => _chamferCorridorBends ? _diagonalCorridorSteps : 0;
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

        // The design a faction builds by, or null for a faction (or no faction) without one.
        public FactionDesign DesignFor(FactionDefinition faction) =>
            faction == null ? null : _factionDesigns.Find(d => d != null && d.Faction == faction);

        public ShipSideDesign DesignFor(ShipSide side) =>
            side == ShipSide.None ? null : _sideDesigns.Find(d => d != null && d.Side == side);

        public bool  BuildCeilings  => _buildCeilings;
        public Material CeilingMaterial => _ceilingMaterial != null ? _ceilingMaterial : _wallMaterial;
        public int   CeilingLayer   => _ceilingLayer;
        public bool  LightBuildings => _lightBuildings;
        public Color BuildingLightColor => _buildingLightColor;
        public float BuildingLightIntensity => _buildingLightIntensity;
        public bool  BuildSkyDomes  => _buildSkyDomes;
        public Material SkyDomeMaterial => _skyDomeMaterial;
        public float SkyDomeHeight  => _skyDomeHeight;
        public Color SkyHorizon     => _skyHorizon;
        public Color SkyZenith      => _skyZenith;
        public int   SpawnMarginTiles => _spawnMarginTiles;
        public int   MinRoomWidthTiles => _minRoomWidthTiles;
        public int   PriorityTries  => _priorityTries;
        public int   RefineSteps    => _refineSteps;
        public IsovistUse IsovistFor => _isovistFor;
        public int LowestLevel  => _lowestLevel;
        public int HighestLevel => _highestLevel;
        public ElevationBudget ElevationBudget => _elevationBudget;
        public GameObject StairsPrefab  => _stairsPrefab;
        public GameObject RailingPrefab => _railingPrefab;
        public bool UsesIsovist(IsovistUse use) => (_isovistFor & use) != 0;
        public RoomDressing FootholdDressing => _footholdDressing;
        public RoomDressing GapDressing => _gapDressing;
        public StructureBudget StructureBudget => _structureBudget;
        public TerritoryDepth TerritoryDepth => _territoryDepth;

        // The holding faction's design as this room shows it (full strength without a faction influence).
        public FactionStyle StyleFor(LevelRoom room) => new(DesignFor(room.Faction), room.DesignStrength);

        // The door on a connection (`intoStealth`: one end is a Stealth room). `gateRoom` is the room whose side of the corridor the door
        // sits on: its faction's design may fit its own kind of door to an ordinary connection.
        public GameObject DoorPrefabFor(MapConnection connection, LevelRoom gateRoom, bool intoStealth = false) => connection.Type switch
        {
            ConnectionType.Locked when connection.Lock == MapLockKind.Terminals && _terminalDoorPrefab != null => _terminalDoorPrefab,
            ConnectionType.Locked => _lockedDoorPrefab,
            ConnectionType.Secret => _secretDoorPrefab,
            _ when connection.OneWay && _oneWayDoorPrefab != null => _oneWayDoorPrefab,
            _ when intoStealth    => FirstSet(StealthDoorOf(gateRoom), _stealthDoorPrefab, FactionDoorOf(gateRoom), _doorPrefab),
            _                     => FirstSet(FactionDoorOf(gateRoom), _doorPrefab),
        };

        // The door hung in a doorway inside a room (an office's): the holder's door, else the plain one.
        public GameObject InteriorDoorPrefabFor(LevelRoom room) => FirstSet(FactionDoorOf(room), _doorPrefab);

        private GameObject FactionDoorOf(LevelRoom room) => DesignFor(room?.Faction)?.DoorPrefab;
        private GameObject StealthDoorOf(LevelRoom room) => DesignFor(room?.Faction)?.StealthDoorPrefab;

        // `??` skips Unity's null check, which an unassigned reference may fail in the editor.
        private static GameObject FirstSet(params GameObject[] options)
        {
            foreach (GameObject option in options)
                if (option != null) return option;
            return null;
        }

        public GameObject KeyPrefab      => _keyPrefab;
        // A door whose Condition is a ConditionLock (falls back to the keycard door).
        public GameObject TerminalDoorPrefab => _terminalDoorPrefab != null ? _terminalDoorPrefab : _lockedDoorPrefab;
        public GameObject TerminalPrefab => _terminalPrefab;

        public bool     FloorStripes        => _floorStripes;
        public Material StripeMaterial      => _stripeMaterial != null ? _stripeMaterial : _floorMaterial;
        public Color    CorridorStripeColor => _corridorStripeColor;
        public Color    LedgeStripeColor    => _ledgeStripeColor;
        public float    StripeWidth         => _stripeWidth;
        public bool     BuildDoorSigns   => _buildDoorSigns;
        public Material DoorSignMaterial => _doorSignMaterial != null ? _doorSignMaterial : _floorMaterial;
        public bool     DoorSignLabels   => _doorSignLabels;
        public float    DangerIntensity  => _dangerIntensity;

        public RoomContentRule RuleFor(MapNodeType type) => _rooms.Find(r => r.Type == type);

        public EnemyTierOdds EnemyTierOddsFor(int roomTier) =>
            _enemyTierOdds.Length == 0 ? default : _enemyTierOdds[Mathf.Clamp(roomTier - 1, 0, _enemyTierOdds.Length - 1)];

        public float LootLuckFor(int roomTier) =>
            _lootLuckByTier.Length == 0 ? 0f : _lootLuckByTier[Mathf.Clamp(roomTier - 1, 0, _lootLuckByTier.Length - 1)];

        public IReadOnlyList<RoomShape> DefaultShapes => _shapes;

        public IReadOnlyList<RoomFunction> Functions => _functions;
    }
}
