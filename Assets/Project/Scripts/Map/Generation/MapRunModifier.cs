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
        [SerializeField, TextArea(2, 4)] private string _description;
        [Tooltip("Relative chance to be picked")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        [Header("Room Types")]
        [SerializeField] private List<MapTypeAdjustment> _types = new();

        [Header("Layout")]
        [SerializeField] private int _extraOptionalRooms;
        [SerializeField] private int _extraLoops;
        [SerializeField, Min(0f)] private float _lockedChanceMultiplier = 1f;
        [SerializeField, Min(0f)] private float _secretChanceMultiplier = 1f;
        [SerializeField] private int _extraGates;

        [Header("Intensity & Factions")]
        [Tooltip("Added to every room's intensity")]
        [SerializeField, Range(-0.5f, 0.5f)] private float _intensityOffset;
        [Tooltip("Replaces the content's faction falloff (0 = keep). High values let factions spread over most of the map")]
        [SerializeField, Range(0f, 1f)] private float _factionFalloff;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;
        public float  Weight      => _weight;
        public IReadOnlyList<MapTypeAdjustment> Types => _types;
        public int    ExtraOptionalRooms     => _extraOptionalRooms;
        public int    ExtraLoops             => _extraLoops;
        public float  LockedChanceMultiplier => _lockedChanceMultiplier;
        public float  SecretChanceMultiplier => _secretChanceMultiplier;
        public int    ExtraGates             => _extraGates;
        public float  IntensityOffset        => _intensityOffset;
        public float  FactionFalloff         => _factionFalloff;
    }
}
