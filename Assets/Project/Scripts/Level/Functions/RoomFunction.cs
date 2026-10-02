using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // What a room is on the ship — a lobby, a restaurant, a park — and so how it's built:
    // which floor plans suit it, how big and tall it is and what structure it has. A room
    // type (Combat, Shop…) lists the functions it can be.
    [CreateAssetMenu(fileName = "RoomFunction", menuName = "CGD/Level/Room Function")]
    public class RoomFunction : ScriptableObject
    {
        [SerializeField] private string _displayName = "Room";
        [Tooltip("How often this function is picked relative to the others a room type lists")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Ship sections where this kind of room is more likely (crew quarters in Habitation, cargo bays in Engineering)")]
        [SerializeField] private MapSectionDefinition[] _preferredSections = Array.Empty<MapSectionDefinition>();
        [Tooltip("Weight multiplier inside a preferred section")]
        [SerializeField, Min(1f)] private float _sectionPreference = 3f;
        [Tooltip("Floor plans for this kind of room. Empty = the build settings' shapes")]
        [SerializeField] private RoomShape[] _shapes = Array.Empty<RoomShape>();
        [Tooltip("Grid cells the room spans (1 or 2 each way; turned to fit). Falls back to one cell when its neighbours leave no room")]
        [SerializeField] private Vector2Int _cells = Vector2Int.one;
        [Tooltip("Single-cell rooms only: width and depth in tiles, at most the build settings' Room Tiles. 0 = the full size")]
        [SerializeField] private IntRange _size;
        [Tooltip("Metres. 0 = the build settings' wall height")]
        [SerializeField, Min(0f)] private float _wallHeight;
        [Tooltip("No ceiling over this room even when the build settings add ceilings (parks, domes open to the stars)")]
        [SerializeField] private bool _openCeiling;
        [Tooltip("Optional hand-built room used instead of a generated floor plan and structure")]
        [SerializeField] private LandmarkRoomDefinition _landmark;
        [Tooltip("Applied in order once doorways are known: pillars, dividers…")]
        [SerializeField] private RoomStructureRule[] _structure = Array.Empty<RoomStructureRule>();
        [Tooltip("Furnishings, placed in order after the structure (tables, crates, beds…)")]
        [SerializeField] private List<RoomPropPlacement> _props = new();

        public string DisplayName => _displayName;
        public float  Weight      => _weight;
        public float  WallHeight  => _wallHeight;
        public Vector2Int Cells   => new(Mathf.Clamp(_cells.x, 1, 2), Mathf.Clamp(_cells.y, 1, 2));
        public LandmarkRoomDefinition Landmark => _landmark;
        public bool   OpenCeiling => _openCeiling;
        public IReadOnlyList<RoomShape>         Shapes    => _shapes;
        public IReadOnlyList<RoomStructureRule> Structure => _structure;
        public IReadOnlyList<RoomPropPlacement> Props     => _props;

        // Weight for a room in `section` (null = no section).
        public float WeightIn(MapSectionDefinition section) =>
            section != null && Array.IndexOf(_preferredSections, section) >= 0 ? _weight * _sectionPreference : _weight;

        // Tiles per side for one room, between minTiles and maxTiles.
        public int RollSize(RandomStream random, int minTiles, int maxTiles)
        {
            int size = _size.Max > 0 ? _size.Evaluate(random) : maxTiles;
            return Mathf.Clamp(size, minTiles, maxTiles);
        }
    }
}
