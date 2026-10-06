using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // A twist on one run's map — "Lockdown" (more locked doors), "Scavenger" (more loot,
    // fewer shops), "Infestation" (one faction everywhere). A style picks a few per seed
    // (MapGenerationSettings) and the generator applies them on top of its layout and content.
    [CreateAssetMenu(fileName = "MapRunModifier", menuName = "CGD/Map/Run Modifier")]
    public class MapRunModifier : ScriptableObject
    {
        [SerializeField] private string _displayName = "Modifier";
        [SerializeField] private MapRunModifierKind _kind = MapRunModifierKind.Warning;
        [SerializeField, TextArea(2, 4)] private string _description;
        [Tooltip("Relative chance to be picked")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Added to the luck of every loot container in the level — what a warning pays for its trouble")]
        [SerializeField, Min(0f)] private float _lootLuck;

        [Header("Room Types")]
        [SerializeField] private List<MapTypeAdjustment> _types = new();

        [Header("Layout")]
        [SerializeField] private int _extraOptionalRooms;
        [SerializeField] private int _extraLoops;
        [SerializeField, Min(0f)] private float _lockedChanceMultiplier = 1f;
        [SerializeField, Min(0f)] private float _secretChanceMultiplier = 1f;
        [SerializeField] private int _extraGates;

        [Tooltip("Extra rooms raised to tier 3 (the deepest tier-2 rooms that can take it)")]
        [SerializeField, Min(0)] private int _extraTopTierRooms;

        [Header("Intensity & Factions")]
        [Tooltip("Added to every room's intensity")]
        [SerializeField, Range(-0.5f, 0.5f)] private float _intensityOffset;
        [Tooltip("Forces this faction mix instead of the content's roll (empty = keep)")]
        [SerializeField] private MapFactionMix _factionMix;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;
        public float  Weight      => _weight;
        public MapRunModifierKind Kind => _kind;
        public float  LootLuck    => _lootLuck;
        public IReadOnlyList<MapTypeAdjustment> Types => _types;
        public int    ExtraOptionalRooms     => _extraOptionalRooms;
        public int    ExtraTopTierRooms      => _extraTopTierRooms;
        public int    ExtraLoops             => _extraLoops;
        public float  LockedChanceMultiplier => _lockedChanceMultiplier;
        public float  SecretChanceMultiplier => _secretChanceMultiplier;
        public int    ExtraGates             => _extraGates;
        public float  IntensityOffset        => _intensityOffset;
        public MapFactionMix FactionMix      => _factionMix;
    }
}
