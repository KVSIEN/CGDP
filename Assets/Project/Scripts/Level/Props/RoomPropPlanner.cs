using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Furnishes one room from RoomPropPlacements, in the order they are added: each picks tiles
    // that carry its tags, sit in its zone, keep its spacing and have the floor in front it
    // asks for, and claims them (Prop) so later placements, enemies and consoles go elsewhere.
    //
    // Whatever a placement asks, a prop is only put down if every free tile of the room stays
    // reachable from every doorway (RoomStructure.TryClaim). Clutter stops at a share of the
    // floor (MaxFill), so a room is never packed solid. Add the important things first —
    // what a player comes for, then furniture, then cover and loose clutter — as each one
    // takes space the next can't have.
    public class RoomPropPlanner
    {
        // How far an AgainstWall prop one tile deep moves from its tile's middle toward the wall, in tiles.
        private const float WallOffset = 0.25f;
        // Props in a pile stand this far from the tile's middle along each axis, in tiles.
        private const float PileOffset = 0.22f;
        // How far a fully jittered prop wanders from its spot, in tiles.
        private const float JitterReach = 0.2f;
        // How far a pile's members are turned off square, in degrees.
        private const float PileTurn = 12f;

        // Share of the floor props may take by default, with pillars and furniture counted in.
        public const float DefaultFillShare = 0.35f;
        // Floor area a placement's Count is written for, and how far room size may scale it.
        private const float ReferenceTiles = 64f;
        private const float MinScale = 0.4f, MaxScale = 2f;
        // Spread looks at this many candidates per pick and takes the one farthest from the rest.
        private const int SpreadSamples = 8;
        // Cluster looks at this many and takes the one nearest the rest, and now and then starts a new patch.
        private const int ClusterSamples = 12;
        private const float NewPatchChance = 0.12f;
        // For Line, a step across the room costs this many steps along it, so a row fills before another starts.
        private const float LineAcrossWeight = 6f;
        // Cover only goes where it breaks an open stretch at least this many tiles long.
        private const int CoverMinRun = 4;
        private const int MaxPile = 4;

        private const RoomTileTags BlocksSight = RoomTileTags.Structure | RoomTileTags.Prop;

        private static readonly Vector2Int[] Cardinals = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private static readonly Vector2[] Quarters = { new(-1f, -1f), new(1f, -1f), new(-1f, 1f), new(1f, 1f) };

        private readonly RoomStructure _structure;
        private readonly Vector2 _anchor;
        private readonly RandomStream _random;
        private readonly int _mainZone;
        private readonly int _fillCap;
        private readonly float _countScale;
        private readonly Vector2Int _latticeOrigin;
        private readonly List<PlannedProp> _planned = new();
        // The middle of everything placed, for Spread.
        private readonly List<Vector2> _centres = new();
        // Length of the open stretch of floor through each tile along x and along y, kept current as
        // props go down so Cover can score any tile at once (0 on a tile that is blocked).
        private readonly RectInt _bounds;
        private readonly int[] _runX;
        private readonly int[] _runY;
        private int _claimed;

        // fillShare: the part of the floor props may take (a clean hall less, a crowded office
        // more). countScale multiplies every placement's count, required ones excepted.
        public RoomPropPlanner(RoomStructure structure, Vector2 anchor, RandomStream random, float fillShare = DefaultFillShare, float countScale = 1f)
        {
            _structure  = structure;
            _anchor     = anchor;
            _random     = random;
            _mainZone   = LargestZone(structure);
            _fillCap    = Mathf.FloorToInt(structure.Footprint.Count * Mathf.Clamp01(fillShare));
            _countScale = Mathf.Max(0f, countScale);
            _latticeOrigin = new Vector2Int(Mathf.FloorToInt(anchor.x), Mathf.FloorToInt(anchor.y));

            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (structure.Has(tile, RoomTileTags.Structure | RoomTileTags.Prop)) _claimed++;

            _bounds = structure.Footprint.Bounds;
            _runX   = new int[_bounds.width * _bounds.height];
            _runY   = new int[_bounds.width * _bounds.height];
            RefreshRuns(_bounds);
        }

        public IReadOnlyList<PlannedProp> Planned => _planned;

        public void Add(IEnumerable<RoomPropPlacement> placements)
        {
            foreach (RoomPropPlacement placement in placements) Add(placement);
        }

        // Places one kind of furnishing; returns how many it placed.
        public int Add(RoomPropPlacement placement)
        {
            if (placement == null || placement.Prefabs == null || placement.Prefabs.Length == 0) return 0;

            int count = Scaled(placement, RollCount(placement));
            if (count <= 0) return 0;

            int placed = Fill(placement, count, required: false);
            for (int level = 1; placed == 0 && placement.Required && level <= 2; level++)
                placed = Fill(placement.Relaxed(level), 1, required: true);
            return placed;
        }

        private int RollCount(RoomPropPlacement placement)
        {
            int count = placement.Count.Evaluate(_random);
            if (!placement.ScaleByRoomSize || count <= 0) return count;

            float scale = Mathf.Clamp(_structure.Footprint.Count / ReferenceTiles, MinScale, MaxScale);
            return Mathf.Max(1, Mathf.RoundToInt(count * scale));
        }

        // What the room's style does to a placement's count: Required placements are exempt.
        private int Scaled(RoomPropPlacement placement, int count) =>
            placement.Required || count <= 0 || Mathf.Approximately(_countScale, 1f) ? count : Mathf.Max(1, Mathf.RoundToInt(count * _countScale));

        // --- Choosing -------------------------------------------------------------------

        private int Fill(RoomPropPlacement placement, int count, bool required)
        {
            Vector2Int size = FootprintOf(placement);
            List<Candidate> pool = Candidates(placement, size);
            _random.Shuffle(pool);

            var own = new List<RectInt>();
            int placed = 0;
            while (placed < count)
            {
                if (!required && !placement.Decor && _claimed + size.x * size.y > _fillCap) break;

                int index = Select(placement, pool, own);
                if (index < 0) break;

                Candidate chosen = pool[index];
                pool.RemoveAt(index);
                if (!TryPlace(chosen, placement, own)) continue;

                placed++;
                if (placed < count && placement.Symmetric && TryPlaceTwin(chosen, placement, size, own, required)) placed++;
            }
            return placed;
        }

        // The mirror image of a placed prop, if the room has the floor for it.
        private bool TryPlaceTwin(Candidate placed, RoomPropPlacement placement, Vector2Int size, List<RectInt> own, bool required)
        {
            Candidate twin = Mirror(placed);
            if (twin.Rect.Equals(placed.Rect) || (!required && _claimed + size.x * size.y > _fillCap)) return false;
            return Suits(twin, placement, size, checkLattice: false) && Placeable(twin, placement, own) && TryPlace(twin, placement, own);
        }

        // Across the line down the middle of the room's long side.
        private Candidate Mirror(Candidate candidate)
        {
            RectInt rect = candidate.Rect;
            if (_bounds.width >= _bounds.height)
            {
                var flipped = new RectInt(rect.xMin, _bounds.yMin + _bounds.yMax - rect.yMax, rect.width, rect.height);
                return new Candidate(flipped, candidate.Rotation % 2 == 0 ? (candidate.Rotation + 2) % 4 : candidate.Rotation);
            }
            var turned = new RectInt(_bounds.xMin + _bounds.xMax - rect.xMax, rect.yMin, rect.width, rect.height);
            return new Candidate(turned, candidate.Rotation % 2 == 1 ? (candidate.Rotation + 2) % 4 : candidate.Rotation);
        }

        // The index of the candidate to try next, or -1 when none is left. Candidates that stopped
        // being placeable are dropped on the way (floor only ever gets taken, never freed).
        private int Select(RoomPropPlacement placement, List<Candidate> pool, List<RectInt> own) => placement.Selection switch
        {
            PropSelection.Spread => SelectSpread(placement, pool, own),
            PropSelection.Cover  => SelectCover(placement, pool, own),
            PropSelection.Cluster => SelectCluster(placement, pool, own),
            PropSelection.Line   => SelectLine(placement, pool, own),
            _                    => SelectRandom(placement, pool, own),
        };

        // The pool is shuffled, so its last usable entry is a random one.
        private int SelectRandom(RoomPropPlacement placement, List<Candidate> pool, List<RectInt> own)
        {
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (!Placeable(pool[i], placement, own)) pool.RemoveAt(i);
                else if (Eligible(pool[i], placement)) return i;
            }
            return -1;
        }

        // Mitchell's best candidate: of a few random candidates, the one farthest from every prop
        // already placed — an approximation of blue noise that is cheap and never clumps.
        private int SelectSpread(RoomPropPlacement placement, List<Candidate> pool, List<RectInt> own)
        {
            int best = -1;
            float bestDistance = -1f;
            int seen = 0;
            for (int i = pool.Count - 1; i >= 0 && seen < SpreadSamples; i--)
            {
                if (!Placeable(pool[i], placement, own))
                {
                    pool.RemoveAt(i);
                    if (best > i) best--;
                    continue;
                }
                if (!Eligible(pool[i], placement)) continue;

                seen++;
                float distance = DistanceToPlaced(pool[i].Rect.center, placement, own);
                if (distance <= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        // Of a few random candidates, the one nearest what this placement has already put down, so
        // props gather in patches; the odd one starts a new patch elsewhere.
        private int SelectCluster(RoomPropPlacement placement, List<Candidate> pool, List<RectInt> own)
        {
            if (own.Count == 0 || _random.Chance(NewPatchChance)) return SelectRandom(placement, pool, own);

            int best = -1;
            float bestDistance = float.MaxValue;
            int seen = 0;
            for (int i = pool.Count - 1; i >= 0 && seen < ClusterSamples; i--)
            {
                if (!Placeable(pool[i], placement, own))
                {
                    pool.RemoveAt(i);
                    if (best > i) best--;
                    continue;
                }
                if (!Eligible(pool[i], placement)) continue;

                seen++;
                float distance = DistanceToOwn(pool[i].Rect.center, own);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        // The candidate that carries on the row already started: the nearest to what is placed,
        // counting a step across the room as dearer than one along it. The first one goes anywhere.
        private int SelectLine(RoomPropPlacement placement, List<Candidate> pool, List<RectInt> own)
        {
            if (own.Count == 0) return SelectRandom(placement, pool, own);

            pool.RemoveAll(candidate => !Placeable(candidate, placement, own));

            int best = -1;
            float bestCost = float.MaxValue;
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (!Eligible(pool[i], placement)) continue;

                float cost = LineCost(pool[i].Rect.center, own);
                if (cost >= bestCost) continue;
                best = i;
                bestCost = cost;
            }
            return best;
        }

        private float LineCost(Vector2 point, List<RectInt> own)
        {
            bool wide = _bounds.width >= _bounds.height;
            float nearest = float.MaxValue;
            foreach (RectInt rect in own)
            {
                Vector2 apart = rect.center - point;
                float along  = Mathf.Abs(wide ? apart.x : apart.y);
                float across = Mathf.Abs(wide ? apart.y : apart.x);
                nearest = Mathf.Min(nearest, along + LineAcrossWeight * across);
            }
            return nearest;
        }

        // The candidate on the longest open line of sight, so each piece cuts the room's worst
        // firing lane; stops when nothing is left to break.
        private int SelectCover(RoomPropPlacement placement, List<Candidate> pool, List<RectInt> own)
        {
            pool.RemoveAll(candidate => !Placeable(candidate, placement, own));

            int best = -1;
            int bestRun = CoverMinRun - 1;
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (!Eligible(pool[i], placement)) continue;

                int run = LongestRun(pool[i].Rect.min);
                if (run <= bestRun) continue;
                best = i;
                bestRun = run;
            }
            return best;
        }

        // How far a point is from the nearest prop: of everything in the room for furniture, of this
        // placement's own for decorations (which aren't in the room's list).
        private float DistanceToPlaced(Vector2 point, RoomPropPlacement placement, List<RectInt> own)
        {
            if (placement.Decor) return DistanceToOwn(point, own);

            float nearest = float.MaxValue;
            foreach (Vector2 centre in _centres)
                nearest = Mathf.Min(nearest, (centre - point).magnitude);
            return nearest;
        }

        private static float DistanceToOwn(Vector2 point, List<RectInt> own)
        {
            float nearest = float.MaxValue;
            foreach (RectInt rect in own)
                nearest = Mathf.Min(nearest, (rect.center - point).magnitude);
            return nearest;
        }

        // --- Candidates -----------------------------------------------------------------

        // A place a prop could go: the tiles it would cover and the quarter turn it faces
        // (0 = +Z, the tile's +y; each step turns toward +X).
        private readonly struct Candidate
        {
            public Candidate(RectInt rect, int rotation)
            {
                Rect     = rect;
                Rotation = rotation;
            }

            public RectInt    Rect     { get; }
            public int        Rotation { get; }
            public Vector2Int Forward  => Cardinals[Rotation];
        }

        private static Vector2Int FootprintOf(RoomPropPlacement placement) =>
            new(Mathf.Max(1, placement.Footprint.x), Mathf.Max(1, placement.Footprint.y));

        // A single tile that needs no particular side up can take any angle.
        private static bool HasFreeYaw(RoomPropPlacement placement, Vector2Int size) =>
            size == Vector2Int.one && placement.Clearance == 0 && !placement.Symmetric
            && placement.Facing is not (PropFacing.AgainstWall or PropFacing.AcrossSightline or PropFacing.AlongRoom);

        private List<Candidate> Candidates(RoomPropPlacement placement, Vector2Int size)
        {
            var candidates = new List<Candidate>();
            int rotations = HasFreeYaw(placement, size) ? 1 : 4;
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                for (int rotation = 0; rotation < rotations; rotation++)
                {
                    Vector2Int extent = rotation % 2 == 0 ? size : new Vector2Int(size.y, size.x);
                    var candidate = new Candidate(new RectInt(tile, extent), rotation);
                    if (Suits(candidate, placement, size, checkLattice: true)) candidates.Add(candidate);
                }
            return candidates;
        }

        // What can't change while the room is furnished: the floor plan, tags, zones and walls.
        private bool Suits(Candidate candidate, RoomPropPlacement placement, Vector2Int size, bool checkLattice)
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
        private bool Placeable(Candidate candidate, RoomPropPlacement placement, List<RectInt> own)
        {
            RectInt rect = candidate.Rect;
            for (int y = rect.yMin; y < rect.yMax; y++)
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    var tile = new Vector2Int(x, y);
                    if (placement.Decor ? _structure.Has(tile, BlocksSight) : !_structure.IsFree(tile)) return false;
                }

            foreach (RectInt other in own)
                if (TileDistance(rect, other) <= placement.Spacing) return false;

            return placement.Decor || placement.Clearance == 0 || ClearanceIsOpen(candidate, placement.Clearance);
        }

        // Whether a barrier-like prop would face the way its sightline runs.
        private bool Eligible(Candidate candidate, RoomPropPlacement placement)
        {
            if (placement.Facing != PropFacing.AcrossSightline) return true;

            (int alongX, int alongY) = Runs(candidate.Rect.min);
            bool facesX = candidate.Rotation % 2 == 1;
            return alongX == alongY || facesX == (alongX > alongY);
        }

        private bool ClearanceIsOpen(Candidate candidate, int depth)
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
        private static void SideSpan(RectInt rect, Vector2Int side, out Vector2Int first, out Vector2Int step, out int length)
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

        // --- Sightlines -----------------------------------------------------------------

        // The open stretch through a tile along x and along y, the tile included.
        private (int alongX, int alongY) Runs(Vector2Int tile)
        {
            int index = (tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin);
            return (_runX[index], _runY[index]);
        }

        private int LongestRun(Vector2Int tile)
        {
            (int alongX, int alongY) = Runs(tile);
            return Mathf.Max(alongX, alongY);
        }

        // After props take floor, the rows and columns they stand in are measured again.
        private void RefreshRuns(RectInt changed)
        {
            for (int y = changed.yMin; y < changed.yMax; y++)
                MeasureLine(new Vector2Int(_bounds.xMin, y), Vector2Int.right, _bounds.width, _runX);
            for (int x = changed.xMin; x < changed.xMax; x++)
                MeasureLine(new Vector2Int(x, _bounds.yMin), Vector2Int.up, _bounds.height, _runY);
        }

        // Gives every tile of a row or column the length of the unbroken stretch of see-through
        // floor it is in; a wall, pillar, prop or inner wall ends a stretch.
        private void MeasureLine(Vector2Int origin, Vector2Int step, int length, int[] runs)
        {
            int start = 0;
            for (int i = 0; i <= length; i++)
            {
                Vector2Int tile = origin + step * i;
                bool open = i < length && _structure.Footprint.Contains(tile) && !_structure.Has(tile, BlocksSight);
                bool cut = open && i > start && _structure.IsPartitioned(tile - step, tile);
                if (open && !cut) continue;

                for (int j = start; j < i; j++)
                {
                    Vector2Int inside = origin + step * j;
                    runs[(inside.y - _bounds.yMin) * _bounds.width + (inside.x - _bounds.xMin)] = i - start;
                }
                if (!open && i < length) runs[(tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin)] = 0;
                start = open ? i : i + 1;
            }
        }

        // --- Placing --------------------------------------------------------------------

        private bool TryPlace(Candidate candidate, RoomPropPlacement placement, List<RectInt> own)
        {
            if (placement.Decor)
            {
                Emit(candidate, placement);
                own.Add(candidate.Rect);
                return true;
            }

            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in candidate.Rect.allPositionsWithin) tiles.Add(tile);
            if (!_structure.TryClaim(tiles, RoomTileTags.Prop)) return false;

            ReserveAccess(candidate, placement.Clearance);
            RefreshRuns(candidate.Rect);
            Emit(candidate, placement);
            own.Add(candidate.Rect);
            _centres.Add(candidate.Rect.center);
            _claimed += tiles.Count;
            return true;
        }

        private void ReserveAccess(Candidate candidate, int depth)
        {
            Vector2Int forward = candidate.Forward;
            SideSpan(candidate.Rect, forward, out Vector2Int first, out Vector2Int along, out int length);
            for (int i = 0; i < length; i++)
                for (int step = 1; step <= depth; step++)
                    _structure.Tag(first + along * i + forward * step, RoomTileTags.Access);
        }

        private void Emit(Candidate candidate, RoomPropPlacement placement)
        {
            Vector2Int size = FootprintOf(placement);
            bool single = size == Vector2Int.one;
            Vector2 centre = candidate.Rect.center;
            Vector2 forward = candidate.Forward;
            float yaw = YawFor(candidate, placement, size, centre);

            int pile = single ? Mathf.Clamp(placement.Pile.Evaluate(_random), 1, MaxPile) : 1;
            Vector2 position = centre;
            if (placement.Facing == PropFacing.AgainstWall && size.y == 1 && pile == 1) position -= forward * WallOffset;
            if (placement.Jitter > 0f && single && !placement.Symmetric) position += Jitter(placement, forward);

            if (pile == 1)
            {
                _planned.Add(new PlannedProp(_random.Pick(placement.Prefabs), position, yaw));
                return;
            }

            var quarters = new List<Vector2>(Quarters);
            _random.Shuffle(quarters);
            for (int i = 0; i < pile; i++)
            {
                float turn = _random.Range(-PileTurn, PileTurn);
                _planned.Add(new PlannedProp(_random.Pick(placement.Prefabs), position + quarters[i] * PileOffset, yaw + turn));
            }
        }

        private float YawFor(Candidate candidate, RoomPropPlacement placement, Vector2Int size, Vector2 centre)
        {
            if (!HasFreeYaw(placement, size)) return candidate.Rotation * 90f;

            switch (placement.Facing)
            {
                case PropFacing.Square:
                    return _random.Range(0, 4) * 90f;
                case PropFacing.TowardCentre when (_anchor - centre).sqrMagnitude > 0.01f:
                    return Yaw(_anchor - centre);
                default:
                    return _random.Range(0f, 360f);
            }
        }

        // Along the wall for wall props, any way for the rest.
        private Vector2 Jitter(RoomPropPlacement placement, Vector2 forward)
        {
            float reach = placement.Jitter * JitterReach;
            if (placement.Facing == PropFacing.AgainstWall)
                return new Vector2(Mathf.Abs(forward.y), Mathf.Abs(forward.x)) * _random.Range(-reach, reach);
            return new Vector2(_random.Range(-reach, reach), _random.Range(-reach, reach));
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

        // Unity yaw: 0° faces +Z (tile +y), 90° faces +X.
        private static float Yaw(Vector2 forward) => Mathf.Atan2(forward.x, forward.y) * Mathf.Rad2Deg;

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
