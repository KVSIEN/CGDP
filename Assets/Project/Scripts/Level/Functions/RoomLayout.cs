using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // One way a place can be arranged: a casino as a slot hall, as table pits or as a small
    // high-roller salon. Each room of a function with layouts picks one, so the place comes out
    // in many shapes, sizes and arrangements while keeping what makes it that place: the
    // function's own props (its cashier cage, its front desk) go into every layout.
    //
    // A layout only says what it changes. Anything left empty or at 0 is the function's.
    [CreateAssetMenu(fileName = "RoomLayout", menuName = "CGD/Level/Room Layout")]
    public class RoomLayout : ScriptableObject
    {
        [Tooltip("Shown after the place's name in the Room Lab and the gallery placard (Casino: Table Pits)")]
        [SerializeField] private string _displayName = "Layout";
        [Tooltip("How often this layout is picked relative to the function's others")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Floor plans for this layout. Empty = the function's")]
        [SerializeField] private RoomShape[] _shapes = Array.Empty<RoomShape>();
        [Tooltip("Grid cells the room spans (1 or 2 each way). 0 = the function's")]
        [SerializeField] private Vector2Int _cells;
        [Tooltip("Single-cell rooms only: width and depth in tiles. 0 = the function's")]
        [SerializeField] private IntRange _size;
        [Tooltip("Structure for this layout, applied in order once doorways are known. Empty = the function's")]
        [SerializeField] private RoomStructureRule[] _structure = Array.Empty<RoomStructureRule>();
        [Tooltip("The layout's set pieces, placed before the function's own props: what this arrangement is built around (the pool basin, the card-table pits)")]
        [SerializeField] private List<RoomPropPlacement> _props = new();
        [Tooltip("Shared kits placed after the set pieces (a control station, a pipe run), so every layout that lists one gets it the same way")]
        [SerializeField] private RoomPropSet[] _sets = Array.Empty<RoomPropSet>();

        // The arrangement is a building: a hall with walled rooms, each furnished by its own place
        // (BuildingStructureRule), so it has no set pieces of its own.
        public bool IsBuilding
        {
            get
            {
                foreach (RoomStructureRule rule in _structure)
                    if (rule is BuildingStructureRule) return true;
                return false;
            }
        }

        public string DisplayName => _displayName;
        public float  Weight      => _weight;
        public IReadOnlyList<RoomShape>         Shapes    => _shapes;
        public IReadOnlyList<RoomStructureRule> Structure => _structure;
        public IReadOnlyList<RoomPropPlacement> Props     => _props;
        public IReadOnlyList<RoomPropSet>       Sets      => _sets;

        // Everything this layout puts down: its own set pieces, then its sets'.
        public IEnumerable<RoomPropPlacement> Pieces
        {
            get
            {
                foreach (RoomPropPlacement placement in _props) yield return placement;
                foreach (RoomPropPlacement placement in RoomPropSet.PropsOf(_sets)) yield return placement;
            }
        }

        // Null when the layout keeps the function's cells.
        public Vector2Int? Cells => _cells.x > 0 && _cells.y > 0
            ? new Vector2Int(Mathf.Clamp(_cells.x, 1, 2), Mathf.Clamp(_cells.y, 1, 2))
            : (Vector2Int?)null;

        public bool HasSize => _size.Max > 0;

        public int RollSize(RandomStream random) => _size.Evaluate(random);
    }
}
