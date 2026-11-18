using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // What a room is on the ship — a lobby, a restaurant, a park — and so how it's built:
    // which floor plans suit it, how big and tall it is and what structure it has. A room
    // type (Combat, Shop…) lists the functions it can be. A map node can pin one (IRoomPlace).
    [CreateAssetMenu(fileName = "RoomFunction", menuName = "CGD/Level/Room Function")]
    public class RoomFunction : ScriptableObject, IRoomPlace
    {
        [SerializeField] private string _displayName = "Room";
        [Tooltip("The kind of ship space this is; map nodes with a category only pick functions of that category")]
        [SerializeField] private RoomCategory _category;
        [Tooltip("How often this function is picked relative to the others a room type lists")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Ship sections where this kind of room is more likely (quarters in Habitation, cargo bays in Engineering)")]
        [SerializeField] private MapSectionDefinition[] _preferredSections = Array.Empty<MapSectionDefinition>();
        [Tooltip("Weight multiplier inside a preferred section")]
        [SerializeField, Min(1f)] private float _sectionPreference = 3f;
        [Tooltip("Who this room can be for, its usual side first. It takes its section's side when listed here (a restaurant in Engineering is a crew mess), otherwise the first. Empty = no side")]
        [SerializeField] private ShipSide[] _sides = Array.Empty<ShipSide>();
        [Tooltip("Floor plans for this kind of room. Empty = the build settings' shapes")]
        [SerializeField] private RoomShape[] _shapes = Array.Empty<RoomShape>();
        [Tooltip("Grid cells the room spans (1 or 2 each way; turned to fit). Falls back to one cell when its neighbours leave no room")]
        [SerializeField] private Vector2Int _cells = Vector2Int.one;
        [Tooltip("Single-cell rooms only: width and depth in tiles, at most the build settings' Room Tiles. 0 = the full size")]
        [SerializeField] private IntRange _size;
        [Tooltip("Metres. 0 = the build settings' wall height")]
        [SerializeField, Min(0f)] private float _wallHeight;
        [Tooltip("Tiles cut off each outside corner at 45°. -1 = the build settings' Room Chamfer")]
        [SerializeField, Range(-1, RoomOutline.MaxChamfer)] private int _chamfer = -1;
        [Tooltip("No ceiling over this room even when the build settings add ceilings (parks, domes open to the stars)")]
        [SerializeField] private bool _openCeiling;
        [Tooltip("At most one room per map is this place (the bridge, the grand atrium). Hand-built landmarks always are")]
        [SerializeField] private bool _unique;
        [Tooltip("Optional hand-built room used instead of a generated floor plan and structure")]
        [SerializeField] private LandmarkRoomDefinition _landmark;
        [Tooltip("Applied in order once doorways are known: pillars, dividers…")]
        [SerializeField] private RoomStructureRule[] _structure = Array.Empty<RoomStructureRule>();
        [Tooltip("Furnishings, placed in order after the structure (tables, crates, beds…). With layouts, these go into every layout, after its own set pieces: what makes the place itself (a casino's cashier cage)")]
        [SerializeField] private List<RoomPropPlacement> _props = new();
        [Tooltip("Shared kits every room of this place gets, after its own props and its layout's (a control station facing the machine, a pipe run)")]
        [SerializeField] private RoomPropSet[] _sets = Array.Empty<RoomPropSet>();
        [Tooltip("Ways this place can be arranged; each room picks one by weight. A layout overrides the shapes, cells, size and structure it sets and adds its own set pieces. Empty = one arrangement, the function's")]
        [SerializeField] private RoomLayout[] _layouts = Array.Empty<RoomLayout>();

        public string DisplayName => _displayName;
        public RoomCategory Category => _category;
        public float  Weight      => _weight;
        public float  WallHeight  => _wallHeight;
        public Vector2Int Cells   => new(Mathf.Clamp(_cells.x, 1, 2), Mathf.Clamp(_cells.y, 1, 2));
        public LandmarkRoomDefinition Landmark => _landmark;
        public bool   IsUnique    => _unique || _landmark != null;
        public bool   OpenCeiling => _openCeiling;
        // Corner chamfer in tiles, or `fallback` when this function doesn't set one.
        public int    ChamferOr(int fallback) => _chamfer >= 0 ? _chamfer : fallback;
        public IReadOnlyList<RoomShape>         Shapes    => _shapes;
        public IReadOnlyList<RoomStructureRule> Structure => _structure;
        public IReadOnlyList<RoomPropPlacement> Props     => _props;
        public IReadOnlyList<RoomPropSet>       Sets      => _sets;
        public IReadOnlyList<RoomLayout>        Layouts   => _layouts;

        // A layout picked by weight, or null when the function has none. `pinned` (a map node's
        // choice) wins when it is one of this function's.
        public RoomLayout PickLayout(RandomStream random, RoomLayout pinned = null)
        {
            if (pinned != null && Array.IndexOf(_layouts, pinned) >= 0) return pinned;

            var candidates = new List<RoomLayout>();
            foreach (RoomLayout layout in _layouts)
                if (layout != null && layout.Weight > 0f) candidates.Add(layout);
            return candidates.Count > 0 ? random.PickWeighted(candidates, l => l.Weight) : null;
        }

        // What a room of this function arranged by `layout` (null = the function alone) is built from.
        public IReadOnlyList<RoomShape> ShapesWith(RoomLayout layout) =>
            layout != null && layout.Shapes.Count > 0 ? layout.Shapes : _shapes;

        public Vector2Int CellsWith(RoomLayout layout) => layout != null && layout.Cells.HasValue ? layout.Cells.Value : Cells;

        public IReadOnlyList<RoomStructureRule> StructureWith(RoomLayout layout) =>
            layout != null && HasAny(layout.Structure) ? layout.Structure : _structure;

        // What a room of this function arranged by `layout` is furnished with, in order. With a
        // layout: the function's anchor (its Required pieces, which the layout's set pieces may
        // stand Near), then the layout's set pieces and sets, then the function's other props.
        // Without one, the function's props in their own order. The function's sets come last.
        public IEnumerable<RoomPropPlacement> PropsWith(RoomLayout layout)
        {
            if (layout == null)
            {
                foreach (RoomPropPlacement placement in _props) yield return placement;
            }
            else
            {
                foreach (RoomPropPlacement placement in _props)
                    if (placement != null && placement.Required) yield return placement;
                foreach (RoomPropPlacement placement in layout.Pieces) yield return placement;
                foreach (RoomPropPlacement placement in _props)
                    if (placement == null || !placement.Required) yield return placement;
            }
            foreach (RoomPropPlacement placement in RoomPropSet.PropsOf(_sets)) yield return placement;
        }

        private static bool HasAny<T>(IReadOnlyList<T> items) where T : class
        {
            foreach (T item in items)
                if (item != null) return true;
            return false;
        }

        // Weight for a room in `section` (null = no section).
        public float WeightIn(MapSectionDefinition section) =>
            section != null && Array.IndexOf(_preferredSections, section) >= 0 ? _weight * _sectionPreference : _weight;

        public IReadOnlyList<ShipSide> Sides => _sides;

        // The side a room of this function is on in `section` (null = no section): the
        // section's side if this function can sit on it, else its usual side.
        public ShipSide SideIn(MapSectionDefinition section)
        {
            if (section != null && section.Side != ShipSide.None && Array.IndexOf(_sides, section.Side) >= 0) return section.Side;
            return _sides.Length > 0 ? _sides[0] : ShipSide.None;
        }

        // Tiles per side for one room, between minTiles and maxTiles; a layout's size wins.
        public int RollSize(RandomStream random, int minTiles, int maxTiles, RoomLayout layout = null)
        {
            int size = layout != null && layout.HasSize ? layout.RollSize(random)
                     : _size.Max > 0                    ? _size.Evaluate(random)
                     : maxTiles;
            return Mathf.Clamp(size, minTiles, maxTiles);
        }
    }
}
