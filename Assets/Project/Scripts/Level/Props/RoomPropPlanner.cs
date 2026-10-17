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
        private const int MaxPile = 4;

        private static readonly Vector2[] Quarters = { new(-1f, -1f), new(1f, -1f), new(-1f, 1f), new(1f, 1f) };

        private readonly RoomStructure _structure;
        private readonly Vector2 _anchor;
        private readonly RandomStream _random;
        private readonly int _fillCap;
        private readonly float _countScale;
        private readonly RectInt _bounds;
        private readonly SightlineMap _sight;
        private readonly PropPlacementRules _rules;
        private readonly PropSelector _selector;
        private readonly List<PlannedProp> _planned = new();
        private int _claimed;

        // fillShare: the part of the floor props may take (a clean hall less, a crowded office
        // more). countScale multiplies every placement's count, required ones excepted.
        public RoomPropPlanner(RoomStructure structure, Vector2 anchor, RandomStream random, float fillShare = DefaultFillShare, float countScale = 1f)
        {
            _structure  = structure;
            _anchor     = anchor;
            _random     = random;
            _fillCap    = Mathf.FloorToInt(structure.Footprint.Count * Mathf.Clamp01(fillShare));
            _countScale = Mathf.Max(0f, countScale);

            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (structure.Has(tile, RoomTileTags.Structure | RoomTileTags.Prop)) _claimed++;

            _bounds   = structure.Footprint.Bounds;
            _sight    = new SightlineMap(structure);
            _rules    = new PropPlacementRules(structure, anchor, _sight);
            _selector = new PropSelector(_rules, _sight, random, _bounds);
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
            Vector2Int size = PropPlacementRules.FootprintOf(placement);
            List<PropCandidate> pool = _rules.Candidates(placement, size);
            _random.Shuffle(pool);

            var own = new List<RectInt>();
            int placed = 0;
            while (placed < count)
            {
                if (!required && !placement.Decor && _claimed + size.x * size.y > _fillCap) break;

                int index = _selector.Select(placement, pool, own);
                if (index < 0) break;

                PropCandidate chosen = pool[index];
                pool.RemoveAt(index);
                if (!TryPlace(chosen, placement, own)) continue;

                placed++;
                if (placed < count && placement.Symmetric && TryPlaceTwin(chosen, placement, size, own, required)) placed++;
            }
            return placed;
        }

        // The mirror image of a placed prop, if the room has the floor for it.
        private bool TryPlaceTwin(PropCandidate placed, RoomPropPlacement placement, Vector2Int size, List<RectInt> own, bool required)
        {
            PropCandidate twin = Mirror(placed);
            if (twin.Rect.Equals(placed.Rect) || (!required && _claimed + size.x * size.y > _fillCap)) return false;
            return _rules.Suits(twin, placement, size, checkLattice: false) && _rules.Placeable(twin, placement, own) && TryPlace(twin, placement, own);
        }

        // Across the line down the middle of the room's long side.
        private PropCandidate Mirror(PropCandidate candidate)
        {
            RectInt rect = candidate.Rect;
            if (_bounds.width >= _bounds.height)
            {
                var flipped = new RectInt(rect.xMin, _bounds.yMin + _bounds.yMax - rect.yMax, rect.width, rect.height);
                return new PropCandidate(flipped, candidate.Rotation % 2 == 0 ? (candidate.Rotation + 2) % 4 : candidate.Rotation);
            }
            var turned = new RectInt(_bounds.xMin + _bounds.xMax - rect.xMax, rect.yMin, rect.width, rect.height);
            return new PropCandidate(turned, candidate.Rotation % 2 == 1 ? (candidate.Rotation + 2) % 4 : candidate.Rotation);
        }

        // --- Placing --------------------------------------------------------------------

        private bool TryPlace(PropCandidate candidate, RoomPropPlacement placement, List<RectInt> own)
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
            _sight.Refresh(candidate.Rect);
            Emit(candidate, placement);
            own.Add(candidate.Rect);
            _selector.NotePlaced(candidate.Rect.center);
            _claimed += tiles.Count;
            return true;
        }

        private void ReserveAccess(PropCandidate candidate, int depth)
        {
            Vector2Int forward = candidate.Forward;
            PropPlacementRules.SideSpan(candidate.Rect, forward, out Vector2Int first, out Vector2Int along, out int length);
            for (int i = 0; i < length; i++)
                for (int step = 1; step <= depth; step++)
                    _structure.Tag(first + along * i + forward * step, RoomTileTags.Access);
        }

        private void Emit(PropCandidate candidate, RoomPropPlacement placement)
        {
            Vector2Int size = PropPlacementRules.FootprintOf(placement);
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

        private float YawFor(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, Vector2 centre)
        {
            if (!PropPlacementRules.HasFreeYaw(placement, size)) return candidate.Rotation * 90f;

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

        // Unity yaw: 0° faces +Z (tile +y), 90° faces +X.
        private static float Yaw(Vector2 forward) => Mathf.Atan2(forward.x, forward.y) * Mathf.Rad2Deg;
    }
}
