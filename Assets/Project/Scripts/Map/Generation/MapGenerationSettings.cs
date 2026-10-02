using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Constraints for MapGenerator: the shape of the map, which experiences go where,
    // how difficulty rises and how factions spread. The generator only answers "what
    // does the player experience, and how is it connected?" — no room geometry.
    //
    // The structure settings cover a wide range of map styles from one generator — a
    // straight corridor, a branching spine, a hub around Start, a winding web — so a new
    // kind of map is a new asset rather than new code.
    [CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "CGD/Map/Map Generation Settings")]
    public class MapGenerationSettings : ScriptableObject
    {
        [Header("Size")]
        [Tooltip("Total rooms, Start, Boss and Exit included. Branches are added until the map reaches it")]
        [SerializeField] private IntRange _roomCount = new(14, 20);
        [Tooltip("Fewest connections from Start to the Boss on any route, shortcuts included. Loops and shortcuts that would cut below it are skipped")]
        [SerializeField, Min(2)] private int _minBossDepth = 5;
        [Tooltip("Rows of rooms allowed above and below Start's. 0 = unlimited. Low values pack rooms into tangled maps; high values let them sprawl")]
        [SerializeField, Min(0)] private int _maxSpread;
        [SerializeField, Min(2)] private int _maxConnectionsPerNode = 4;

        [Header("Structure")]
        [SerializeField] private MapPathSettings   _mainPath = new();
        [SerializeField] private MapBranchSettings _branches = new();
        [SerializeField] private MapLoopSettings   _loops    = new();
        [SerializeField] private MapGateSettings   _gates    = new();

        [Header("Early Loot")]
        [Tooltip("Guarantees one Treasure this many rooms from Start, on a branch when one is that deep. 0–0 = off")]
        [SerializeField] private IntRange _earlyTreasureDepth = new(0, 0);

        [Header("Node Types")]
        [Tooltip("Start, Boss and Exit are placed structurally; rules for them are ignored")]
        [SerializeField] private List<MapNodeTypeRule> _nodeRules = DefaultRules();
        [Tooltip("Used when no rule can take a node")]
        [SerializeField] private MapNodeType _fillType = MapNodeType.Combat;

        [Header("Intensity")]
        [Tooltip("Base intensity by depth (0 = Start, 1 = Boss)")]
        [SerializeField] private AnimationCurve _intensityByDepth = DefaultIntensityCurve();
        [SerializeField, Range(0f, 0.5f)] private float _intensityJitter = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _bossIntensity = 1f;

        [Header("Factions")]
        [Tooltip("Each faction claims one origin node; its influence fades with every connection away from it")]
        [SerializeField] private string[] _factions = { "TECH", "BIO", "VOID" };
        [Tooltip("Influence kept per connection travelled")]
        [SerializeField, Range(0f, 1f)] private float _factionFalloff = 0.6f;
        [Tooltip("Nodes below this influence belong to no faction")]
        [SerializeField, Range(0f, 1f)] private float _factionThreshold = 0.2f;

        [Header("Layout (editor only)")]
        [SerializeField] private Vector2 _nodeSpacing = new(220f, 110f);

        public IntRange RoomCount             => _roomCount;
        public int      MinBossDepth          => _minBossDepth;
        public int      MaxSpread             => _maxSpread;
        public int      MaxConnectionsPerNode => _maxConnectionsPerNode;

        public MapPathSettings   MainPath => _mainPath;
        public MapBranchSettings Branches => _branches;
        public MapLoopSettings   Loops    => _loops;
        public MapGateSettings   Gates    => _gates;

        public IntRange EarlyTreasureDepth => _earlyTreasureDepth;
        public bool     HasEarlyTreasure   => _earlyTreasureDepth.Max > 0;

        public IReadOnlyList<MapNodeTypeRule> NodeRules => _nodeRules;
        public MapNodeType FillType => _fillType;

        public float IntensityJitter => _intensityJitter;
        public float BossIntensity   => _bossIntensity;

        public IReadOnlyList<string> Factions => _factions;
        public float FactionFalloff   => _factionFalloff;
        public float FactionThreshold => _factionThreshold;

        public Vector2 NodeSpacing => _nodeSpacing;

        public static bool IsStructural(MapNodeType type) =>
            type == MapNodeType.Start || type == MapNodeType.Boss || type == MapNodeType.Exit;

        public float BaseIntensity(float progress) => Mathf.Clamp01(_intensityByDepth.Evaluate(progress));

        public MapNodeTypeRule GetRule(MapNodeType type) => _nodeRules.Find(r => r.Type == type);

        public string FactionName(int faction) =>
            faction >= 0 && faction < _factions.Length ? _factions[faction] : "None";

        private static List<MapNodeTypeRule> DefaultRules() => new()
        {
            new(MapNodeType.Combat,   min: 3, max: 99, weight: 5f),
            new(MapNodeType.Elite,    min: 1, max: 3,  weight: 1.5f, minDepth: 0.4f, allowAdjacentSameType: false, intensityBonus: 0.25f),
            new(MapNodeType.Puzzle,   min: 0, max: 2,  weight: 1f,   intensityBonus: -0.2f),
            new(MapNodeType.Shop,     min: 1, max: 2,  weight: 0.6f, placement: MapPlacement.MainPathOnly, minDepth: 0.3f, maxDepth: 0.9f, allowAdjacentSameType: false, intensityBonus: -0.5f),
            new(MapNodeType.Event,    min: 1, max: 3,  weight: 1f,   minDepth: 0.1f, intensityBonus: -0.2f),
            new(MapNodeType.Treasure, min: 1, max: 3,  weight: 0.8f, placement: MapPlacement.BranchOnly, preferDeadEnds: true, intensityBonus: -0.3f),
        };

        // Rises through the run, dips just before the boss to give a breather.
        private static AnimationCurve DefaultIntensityCurve() => new(
            new Keyframe(0f,    0.1f),
            new Keyframe(0.7f,  0.7f),
            new Keyframe(0.85f, 0.5f),
            new Keyframe(1f,    0.9f));
    }
}
