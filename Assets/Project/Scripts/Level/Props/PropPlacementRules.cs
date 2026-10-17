using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Where a RoomPropPlacement may put a prop in one room: the candidate spots (tiles and
    // facing) that suit its tags, zone, walls and lattice, and whether a spot is still
    // placeable now that other props have taken floor. RoomPropPlanner chooses among them.
    internal sealed class PropPlacementRules
    {
        private readonly RoomStructure _structure;
        private readonly Vector2 _anchor;
        private readonly RectInt _bounds;
        private readonly SightlineMap _sight;
        private readonly int _mainZone;
        private readonly Vector2Int _latticeOrigin;

        public PropPlacementRules(RoomStructure structure, Vector2 anchor, SightlineMap sight)
        {
            _structure     = structure;
            _anchor        = anchor;
            _bounds        = structure.Footprint.Bounds;
            _sight         = sight;
            _mainZone      = LargestZone(structure);
            _latticeOrigin = new Vector2Int(Mathf.FloorToInt(anchor.x), Mathf.FloorToInt(anchor.y));
        }

        internal static Vector2Int FootprintOf(RoomPropPlacement placement) =>
            new(Mathf.Max(1, placement.Footprint.x), Mathf.Max(1, placement.Footprint.y));

        // A single tile that needs no particular side up can take any angle.
        internal static bool HasFreeYaw(RoomPropPlacement placement, Vector2Int size) =>
            size == Vector2Int.one && placement.Clearance == 0 && !placement.Symmetric
            && placement.Facing is not (PropFacing.AgainstWall or PropFacing.AcrossSightline or PropFacing.AlongRoom);

        internal List<PropCandidate> Candidates(RoomPropPlacement placement, Vector2Int size)
        {
            var candidates = new List<PropCandidate>();
            int rotations = HasFreeYaw(placement, size) ? 1 : 4;
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                for (int rotation = 0; rotation < rotations; rotation++)
                {
                    Vector2Int extent = rotation % 2 == 0 ? size : new Vector2Int(size.y, size.x);
                    var candidate = new PropCandidate(new RectInt(tile, extent), rotation);
                    if (Suits(candidate, placement, size, checkLattice: true)) candidates.Add(candidate);
                }
            return candidates;
        }

        // What can't change while the room is furnished: the floor plan, tags, zones and walls.
        internal bool Suits(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, bool checkLattice)
        {
            RectInt rect = candidate.Rect;
            if (checkLattice && !OnLattice(rect.min, placement.Lattice)) return false;
            bool matchesOn = placement.On == RoomTileTags.None;

            for (int y = rect.yMin; y < rect.yMax; y++)
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (!_structure.Footprint.Contains(tile) || _structure.Has(tile, placement.Avoid)) return false;
                    if (placement.Zone == PropZone.Largest && _structure.ZoneOf(tile) != _mainZone) return false;
                    if (placement.Zone == PropZone.Others  && _structure.ZoneOf(tile) == _mainZone) return false;
                    if (_structure.Has(tile, placement.On)) matchesOn = true;

                    if (x + 1 < rect.xMax && _structure.IsPartitioned(tile, tile + Vector2Int.right)) return false;
                    if (y + 1 < rect.yMax && _structure.IsPartitioned(tile, tile + Vector2Int.up))    return false;
                }
            if (!matchesOn) return false;

            switch (placement.Facing)
            {
                case PropFacing.AgainstWall:
                    Vector2Int back = -candidate.Forward;
                    SideSpan(rect, back, out Vector2Int first, out Vector2Int along, out int length);
                    for (int i = 0; i < length; i++)
                        if (!_structure.IsWall(first + along * i, back)) return false;
                    return true;
                case PropFacing.TowardCentre when !HasFreeYaw(placement, size):
                    return candidate.Rotation == RotationToward(rect.center);
                case PropFacing.AlongRoom:
                    return candidate.Rotation == RotationAlongRoom(rect.center);
                default:
                    return true;
            }
        }

        // What changes as props go down: free floor, spacing and access. A decoration only needs
        // to keep off pillars and props.
        internal bool Placeable(PropCandidate candidate, RoomPropPlacement placement, List<RectInt> own)
        {
            RectInt rect = candidate.Rect;
            for (int y = rect.yMin; y < rect.yMax; y++)
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (placement.Decor ? _structure.Has(tile, SightlineMap.BlocksSight) : !_structure.IsFree(tile)) return false;
                }

            foreach (RectInt other in own)
                if (TileDistance(rect, other) <= placement.Spacing) return false;

            return placement.Decor || placement.Clearance == 0 || ClearanceIsOpen(candidate, placement.Clearance);
        }

        // Whether a barrier-like prop would face the way its sightline runs.
        internal bool Eligible(PropCandidate candidate, RoomPropPlacement placement)
        {
            if (placement.Facing != PropFacing.AcrossSightline) return true;

            (int alongX, int alongY) = _sight.Runs(candidate.Rect.min);
            bool facesX = candidate.Rotation % 2 == 1;
            return alongX == alongY || facesX == (alongX > alongY);
        }

        private bool ClearanceIsOpen(PropCandidate candidate, int depth)
        {
            Vector2Int forward = candidate.Forward;
            SideSpan(candidate.Rect, forward, out Vector2Int first, out Vector2Int along, out int length);
            for (int i = 0; i < length; i++)
            {
                Vector2Int tile = first + along * i;
                for (int step = 0; step < depth; step++)
                {
                    Vector2Int next = tile + forward;
                    if (_structure.IsPartitioned(tile, next) || !_structure.Footprint.Contains(next)
                        || _structure.Has(next, RoomTileTags.Structure | RoomTileTags.Prop)) return false;
                    tile = next;
                }
            }
            return true;
        }

        // The tiles along one side of a rectangle: the first, the step to the next, and how many.
        internal static void SideSpan(RectInt rect, Vector2Int side, out Vector2Int first, out Vector2Int step, out int length)
        {
            if (side.y != 0)
            {
                first  = new Vector2Int(rect.xMin, side.y > 0 ? rect.yMax - 1 : rect.yMin);
                step   = Vector2Int.right;
                length = rect.width;
            }
            else
            {
                first  = new Vector2Int(side.x > 0 ? rect.xMax - 1 : rect.xMin, rect.yMin);
                step   = Vector2Int.up;
                length = rect.height;
            }
        }

        // Steps between the nearest tiles of two rectangles, the larger of the two axes: side by
        // side is 1, one empty tile between is 2 (the Chebyshev distance of two single tiles).
        private static int TileDistance(RectInt a, RectInt b)
        {
            int dx = Mathf.Max(0, Mathf.Max(a.xMin - (b.xMax - 1), b.xMin - (a.xMax - 1)));
            int dy = Mathf.Max(0, Mathf.Max(a.yMin - (b.yMax - 1), b.yMin - (a.yMax - 1)));
            return Mathf.Max(dx, dy);
        }

        // Grid cells of the given pitch, counted from the room's middle.
        private bool OnLattice(Vector2Int origin, Vector2Int pitch) =>
            Mod(origin.x - _latticeOrigin.x, Mathf.Max(1, pitch.x)) == 0 && Mod(origin.y - _latticeOrigin.y, Mathf.Max(1, pitch.y)) == 0;

        private static int Mod(int value, int by) => (value % by + by) % by;

        // Front along the longer axis of the room, toward its middle (+x/-x in a wide room, +y/-y in a tall one).
        private int RotationAlongRoom(Vector2 point) => _bounds.width >= _bounds.height
            ? (point.x <= _anchor.x ? 1 : 3)
            : (point.y <= _anchor.y ? 0 : 2);

        private int RotationToward(Vector2 point)
        {
            Vector2 toward = _anchor - point;
            if (toward.sqrMagnitude < 0.01f) return 0;
            return Mathf.Abs(toward.x) > Mathf.Abs(toward.y) ? (toward.x > 0f ? 1 : 3) : (toward.y > 0f ? 0 : 2);
        }

        private static int LargestZone(RoomStructure structure)
        {
            var sizes = new int[structure.ZoneCount];
            foreach (Vector2Int tile in structure.Footprint.Tiles) sizes[structure.ZoneOf(tile)]++;

            int largest = 0;
            for (int zone = 1; zone < sizes.Length; zone++)
                if (sizes[zone] > sizes[largest]) largest = zone;
            return largest;
        }
    }
}
