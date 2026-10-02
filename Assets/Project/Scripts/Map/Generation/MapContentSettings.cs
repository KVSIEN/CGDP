using System;
using System.Collections.Generic;
using CGD.Core;
using UnityEngine;
using CGD.Factions;

namespace CGD.Map
{
    // What fills a map: which room types go where, rooms guaranteed in key places, how
    // difficulty rises through the run and how factions spread. Independent of the map's
    // shape (MapLayoutSettings), so the same content works on any layout.
    [CreateAssetMenu(fileName = "MapContentSettings", menuName = "CGD/Map/Map Content Settings")]
    public class MapContentSettings : ScriptableObject
    {
        [Header("Node Types")]
        [Tooltip("Start, Boss and Exit are placed by the layout; rules for them are ignored")]
        [SerializeField] private List<MapNodeTypeRule> _nodeRules = DefaultRules();
        [Tooltip("Used when no rule can take a node")]
        [SerializeField] private MapNodeType _fillType = MapNodeType.Combat;

        [Header("Guaranteed Rooms")]
        [Tooltip("Rooms the content always asks for in a set place: before the Boss (ignores the type rule's depth and placement), behind every gate (the reason to find the key), or within a depth window from Start. Placed in that order, after pinned rooms")]
        [SerializeField] private List<MapGuarantee> _guarantees = DefaultGuarantees();

        [Header("Pacing")]
        [SerializeField] private MapPacingSettings _pacing = new();

        [Header("Intensity")]
        [Tooltip("Base intensity by depth (0 = Start, 1 = Boss)")]
        [SerializeField] private AnimationCurve _intensityByDepth = DefaultIntensityCurve();
        [SerializeField, Range(0f, 0.5f)] private float _intensityJitter = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _bossIntensity = 1f;

        [Header("Sections")]
        [Tooltip("Parts of the ship the run passes through, in order from Start to the Boss; each takes an equal share of the depth")]
        [SerializeField] private MapSectionDefinition[] _sections = Array.Empty<MapSectionDefinition>();

        [Header("Factions")]
        [Tooltip("Each faction claims one origin node; its influence fades with every connection away from it")]
        [SerializeField] private FactionDefinition[] _factions = Array.Empty<FactionDefinition>();
        [Tooltip("Influence kept per connection travelled")]
        [SerializeField, Range(0f, 1f)] private float _factionFalloff = 0.6f;
        [Tooltip("Nodes below this influence belong to no faction")]
        [SerializeField, Range(0f, 1f)] private float _factionThreshold = 0.2f;

        public IReadOnlyList<MapNodeTypeRule> NodeRules => _nodeRules;
        public MapNodeType FillType => _fillType;

        public IReadOnlyList<MapGuarantee> Guarantees => _guarantees;
        public MapPacingSettings           Pacing     => _pacing;
        public IReadOnlyList<MapSectionDefinition> Sections => _sections;

        public float IntensityJitter => _intensityJitter;
        public float BossIntensity   => _bossIntensity;

        public IReadOnlyList<FactionDefinition> Factions => _factions;
        public float FactionFalloff   => _factionFalloff;
        public float FactionThreshold => _factionThreshold;

        public float BaseIntensity(float progress) => Mathf.Clamp01(_intensityByDepth.Evaluate(progress));

        public MapNodeTypeRule GetRule(MapNodeType type) => _nodeRules.Find(r => r.Type == type);

        // False once the type's rule has placed its maximum. Types without a rule are unlimited.
        public bool HasRoomFor(MapNodeType type, int placed)
        {
            MapNodeTypeRule rule = GetRule(type);
            return rule == null || placed < rule.Max;
        }

        public MapSectionDefinition GetSection(int section) =>
            section >= 0 && section < _sections.Length ? _sections[section] : null;

        public string SectionName(int section)
        {
            MapSectionDefinition definition = GetSection(section);
            return definition != null ? definition.DisplayName : section >= 0 ? $"Section {section}" : "None";
        }

        // The section a room at this depth (0 = Start, 1 = Boss) belongs to, or MapNode.NoSection.
        public int SectionAt(float progress) =>
            _sections.Length == 0 ? MapNode.NoSection : Mathf.Clamp(Mathf.FloorToInt(progress * _sections.Length), 0, _sections.Length - 1);

        public FactionDefinition GetFaction(int faction) =>
            faction >= 0 && faction < _factions.Length ? _factions[faction] : null;

        public string FactionName(int faction)
        {
            FactionDefinition definition = GetFaction(faction);
            return definition != null ? definition.DisplayName : faction >= 0 ? $"Faction {faction}" : "None";
        }

        private static List<MapNodeTypeRule> DefaultRules() => new()
        {
            new(MapNodeType.Combat,   min: 3, max: 99, weight: 5f),
            new(MapNodeType.Elite,    min: 1, max: 3,  weight: 1.5f, minDepth: 0.4f, allowAdjacentSameType: false, intensityBonus: 0.25f),
            new(MapNodeType.Puzzle,   min: 0, max: 2,  weight: 1f,   intensityBonus: -0.2f),
            new(MapNodeType.Shop,     min: 1, max: 2,  weight: 0.6f, placement: MapPlacement.MainPathOnly, minDepth: 0.3f, maxDepth: 0.9f, allowAdjacentSameType: false, intensityBonus: -0.5f),
            new(MapNodeType.Event,    min: 1, max: 3,  weight: 1f,   minDepth: 0.1f, intensityBonus: -0.2f),
            new(MapNodeType.Treasure, min: 1, max: 3,  weight: 0.8f, placement: MapPlacement.BranchOnly, preferDeadEnds: true, intensityBonus: -0.3f, minSpacing: 3),
            new(MapNodeType.Resupply, min: 0, max: 2,  weight: 0.4f, placement: MapPlacement.MainPathOnly, minDepth: 0.3f, maxDepth: 0.9f, allowAdjacentSameType: false, intensityBonus: -0.5f, minSpacing: 3),
        };

        private static List<MapGuarantee> DefaultGuarantees() => new()
        {
            new(MapNodeType.Treasure, MapGuaranteeSpot.BehindEveryGate),
        };

        // Rises through the run, dips just before the boss to give a breather.
        private static AnimationCurve DefaultIntensityCurve() => new(
            new Keyframe(0f,    0.1f),
            new Keyframe(0.7f,  0.7f),
            new Keyframe(0.85f, 0.5f),
            new Keyframe(1f,    0.9f));
    }
}
