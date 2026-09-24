using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Constraints for MapGenerator: the shape of the map, which experiences go where,
    // how difficulty rises and how factions spread. The generator only answers "what
    // does the player experience, and how is it connected?" — no room geometry.
    [CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "CGD/Map/Map Generation Settings")]
    public class MapGenerationSettings : ScriptableObject
    {
        [Header("Main Path")]
        [Tooltip("Rooms between Start and Boss")]
        [SerializeField] private IntRange _mainPathLength = new(6, 9);

        [Header("Branches")]
        [SerializeField] private IntRange _branchCount = new(2, 4);
        [Tooltip("Rooms per branch")]
        [SerializeField] private IntRange _branchLength = new(1, 3);
        [Tooltip("Chance a branch reconnects to the main path further ahead instead of dead-ending")]
        [SerializeField, Range(0f, 1f)] private float _branchRejoinChance = 0.3f;
        [Tooltip("Chance a branch's entrance is Locked")]
        [SerializeField, Range(0f, 1f)] private float _lockedBranchChance = 0.15f;
        [Tooltip("Chance a branch's entrance is Secret (rolled only when not Locked)")]
        [SerializeField, Range(0f, 1f)] private float _secretBranchChance = 0.15f;

        [Header("Loops")]
        [Tooltip("Shortcut connections that skip ahead along the main path")]
        [SerializeField] private IntRange _shortcutCount = new(0, 1);
        [SerializeField, Min(2)] private int _maxConnectionsPerNode = 4;

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
        [SerializeField] private string[] _factions = { "Cult", "Machines" };
        [Tooltip("Influence kept per connection travelled")]
        [SerializeField, Range(0f, 1f)] private float _factionFalloff = 0.6f;
        [Tooltip("Nodes below this influence belong to no faction")]
        [SerializeField, Range(0f, 1f)] private float _factionThreshold = 0.2f;

        [Header("Layout (editor only)")]
        [SerializeField] private Vector2 _nodeSpacing = new(220f, 110f);

        public IntRange MainPathLength       => _mainPathLength;
        public IntRange BranchCount          => _branchCount;
        public IntRange BranchLength         => _branchLength;
        public float    BranchRejoinChance   => _branchRejoinChance;
        public float    LockedBranchChance   => _lockedBranchChance;
        public float    SecretBranchChance   => _secretBranchChance;
        public IntRange ShortcutCount        => _shortcutCount;
        public int      MaxConnectionsPerNode => _maxConnectionsPerNode;

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
