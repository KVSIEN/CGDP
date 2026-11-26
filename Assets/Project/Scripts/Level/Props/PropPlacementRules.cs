using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Where a RoomPropPlacement may put a prop in one room: the candidate spots (tiles and
    // facing) that suit its tags, zone, flow band, walls, lattice, distance from the routes and from what
    // it stands near, and whether a spot is still placeable now that other props have taken
    // floor. RoomPropPlanner chooses among them.
    internal sealed class PropPlacementRules
    {
        // A footprint that grows with the room is tried at this many sizes above its smallest.
        private const int GrowSteps = 5;

        private readonly RoomStructure _structure;
        private readonly Vector2 _anchor;
        private readonly RectInt _bounds;
        // The firing lanes a barrier turns across: what someone crouched sees along rows and columns.
        private readonly SightlineMap _sight;
        private readonly int _mainZone;
        private readonly Vector2Int _latticeOrigin;
        private readonly RouteMap _routes;
        // What each keyed placement has put down so far (RoomPropPlacement.Key), for Near.
        private readonly IReadOnlyDictionary<string, List<RectInt>> _keyed;

        public PropPlacementRules(RoomStructure structure, Vector2 anchor, SightlineMap sight, RouteMap routes,
                                  IReadOnlyDictionary<string, List<RectInt>> keyed)
        {
            _keyed         = keyed;
            _structure     = structure;
            _anchor        = anchor;
            _bounds        = structure.Footprint.Bounds;
            _sight         = sight;
            _routes        = routes;
            _mainZone      = structure.LargestZone;
            _latticeOrigin = new Vector2Int(Mathf.FloorToInt(anchor.x), Mathf.FloorToInt(anchor.y));
        }

        internal static Vector2Int FootprintOf(RoomPropPlacement placement) =>
            new(Mathf.Max(1, placement.Footprint.x), Mathf.Max(1, placement.Footprint.y));

        // The footprints to try, largest first: for a placement that grows with the room, its
        // share of the room's sides, then a few steps smaller down to its Footprint; otherwise
        // just the Footprint.
        internal List<Vector2Int> Footprints(RoomPropPlacement placement)
        {
            Vector2Int least = FootprintOf(placement);
            var sizes = new List<Vector2Int>();
            if (placement.GrowsWithRoom)
            {
                int longSide  = Mathf.Max(_bounds.width, _bounds.height);
                int shortSide = Mathf.Min(_bounds.width, _bounds.height);
                var grown = new Vector2Int(Mathf.Max(least.x, Mathf.FloorToInt(longSide * placement.Grow.x)),
                                           Mathf.Max(least.y, Mathf.FloorToInt(shortSide * placement.Grow.y)));
                for (int step = GrowSteps; step > 0; step--)
                {
                    float t = (float)step / GrowSteps;
                    var size = new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(least.x, grown.x, t)), Mathf.RoundToInt(Mathf.Lerp(least.y, grown.y, t)));
                    if (size != least && !sizes.Contains(size)) sizes.Add(size);
                }
            }
            sizes.Add(least);
            return sizes;
        }

        // Only loose clutter on a single tile takes any angle. Anything turned with intent (toward
        // the centre, toward what it stands near) snaps to the nearest quarter turn, so benches,
        // cases and seats stand square to the room instead of at odd angles.
        internal static bool HasFreeYaw(RoomPropPlacement placement, Vector2Int size) =>
            size == Vector2Int.one && placement.Clearance == 0 && !placement.Symmetric && placement.Facing == PropFacing.Random;

        // How far a spot is from where a Centred placement fills outward from: what it stands
        // Near when it has a Near, otherwise the room's middle.
        internal float FocusDistance(RectInt rect, RoomPropPlacement placement)
        {
            if (placement.StandsNear && TryNearest(rect, placement.Near, int.MaxValue, out RectInt target))
                return TileDistance(rect, target) + (rect.center - target.center).magnitude * 0.01f;
            return (rect.center - _anchor).magnitude;
        }

        // `zones`: only spots wholly inside these zones (null = anywhere in the room).
        internal List<PropCandidate> Candidates(RoomPropPlacement placement, Vector2Int size, ICollection<int> zones = null)
        {
            var candidates = new List<PropCandidate>();
            int rotations = HasFreeYaw(placement, size) ? 1 : 4;
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                for (int rotation = 0; rotation < rotations; rotation++)
                {
                    Vector2Int extent = rotation % 2 == 0 ? size : new Vector2Int(size.y, size.x);
                    var candidate = new PropCandidate(new RectInt(tile, extent), rotation);
                    if (Suits(candidate, placement, size, checkLattice: true, zones)) candidates.Add(candidate);
                }
            return candidates;
        }

        // What can't change while the room is furnished: the floor plan, tags, zones and walls.
        internal bool Suits(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, bool checkLattice, ICollection<int> zones = null)
        {
            RectInt rect = candidate.Rect;
            if (checkLattice && !OnLattice(rect.min, placement.Lattice)) return false;
            bool matchesOn = placement.On == RoomTileTags.None;

            for (int y = rect.yMin; y < rect.yMax; y++)
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (!_structure.Contains(tile) || _structure.Has(tile, placement.Avoid | RoomTileTags.Stairs)) return false;
                    if (zones != null && !zones.Contains(_structure.ZoneOf(tile))) return false;
                    if (placement.Zone == PropZone.Largest && _structure.ZoneOf(tile) != _mainZone) return false;
                    if (placement.Zone == PropZone.Others  && _structure.ZoneOf(tile) == _mainZone) return false;
                    if (placement.Zone == PropZone.Raised  && _structure.LevelOf(tile) <= 0) return false;
                    if (!RoomFlowPlanner.InBand(_structure, tile, placement.Band)) return false;
                    if (_structure.Has(tile, placement.On)) matchesOn = true;

                    // A prop stands on one piece of floor: no wall through it, and all of it on one level.
                    if (x + 1 < rect.xMax && !_structure.CanStep(tile, tile + Vector2Int.right)) return false;
                    if (y + 1 < rect.yMax && !_structure.CanStep(tile, tile + Vector2Int.up))    return false;
                }
            if (!matchesOn || !SuitsRoutes(rect, placement)) return false;
            if (placement.StandsNear && !TryNearest(rect, placement.Near, placement.NearDistance, out _)) return false;

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
                case PropFacing.TowardNear when placement.StandsNear && !HasFreeYaw(placement, size):
                    return TryNearest(rect, placement.Near, int.MaxValue, out RectInt target)
                           && candidate.Rotation == RotationToward(rect.center, target.center);
                default:
                    return true;
            }
        }

        // The nearest of what the placements keyed `key` have put down, within `reach` steps.
        internal bool TryNearest(RectInt rect, string key, int reach, out RectInt nearest)
        {
            nearest = default;
            if (string.IsNullOrEmpty(key) || !_keyed.TryGetValue(key, out List<RectInt> placed)) return false;

            int best = int.MaxValue;
            foreach (RectInt other in placed)
            {
                int steps = TileDistance(rect, other);
                if (steps > reach || steps >= best) continue;
                best    = steps;
                nearest = other;
            }
            return best != int.MaxValue;
        }

        private bool SuitsRoutes(RectInt rect, RoomPropPlacement placement)
        {
            if (placement.NearRoute <= 0 && placement.OffRoute <= 0) return true;
            if (!_routes.HasRoutes) return true;

            int steps = _routes.DistanceOf(rect);
            if (placement.NearRoute > 0 && steps > placement.NearRoute) return false;
            return placement.OffRoute <= 0 || (steps >= placement.OffRoute && steps != RouteMap.Unreached);
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
                    if (placement.Decor ? _structure.Has(tile, SightBlocking.Tiles) : !_structure.IsFree(tile)) return false;
                }

            foreach (RectInt other in own)
                if (TileDistance(rect, other) <= placement.Spacing) return false;

            if (placement.MinEmpty > 0 && !IsBare(rect, placement.MinEmpty)) return false;
            return placement.Decor || placement.Clearance == 0 || ClearanceIsOpen(candidate, placement.Clearance);
        }

        // Whether no prop or pillar stands within `reach` tiles round a rectangle.
        private bool IsBare(RectInt rect, int reach)
        {
            for (int y = rect.yMin - reach; y < rect.yMax + reach; y++)
                for (int x = rect.xMin - reach; x < rect.xMax + reach; x++)
                    if (_structure.Has(new Vector2Int(x, y), RoomTileTags.Prop | RoomTileTags.Structure)) return false;
            return true;
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
                    if (!_structure.CanStep(tile, next)
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

        private int RotationToward(Vector2 point) => RotationToward(point, _anchor);

        // The quarter turn whose front points most nearly from `point` to `target`.
        internal static int RotationToward(Vector2 point, Vector2 target)
        {
            Vector2 toward = target - point;
            if (toward.sqrMagnitude < 0.01f) return 0;
            return Mathf.Abs(toward.x) > Mathf.Abs(toward.y) ? (toward.x > 0f ? 1 : 3) : (toward.y > 0f ? 0 : 2);
        }
    }
}
