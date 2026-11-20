using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Furnishes one room from RoomPropPlacements, in the order they are added: each picks tiles
    // that carry its tags, sit in its zone, keep its spacing and have the floor in front it
    // asks for, and claims them (Prop) so later placements, enemies and consoles go elsewhere.
    //
    // A placement may set pieces round each prop (a group: chairs round a card table), stand
    // near what an earlier keyed placement put down (loungers round the pool) and grow with the
    // room (a pool basin), its prefab stretched to the floor it gets.
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
        private readonly float _concealmentScale;
        private readonly RectInt _bounds;
        private readonly SightlineMap _sight;
        private readonly RouteMap _routes;
        private readonly PropPlacementRules _rules;
        private readonly PropSelector _selector;
        private readonly List<PlannedProp> _planned = new();
        private readonly List<PropPlacementOutcome> _outcomes = new();
        // What each keyed placement put down (RoomPropPlacement.Key), for the ones Near it.
        private readonly Dictionary<string, List<RectInt>> _keyed = new();
        private int _claimed;
        private bool _hitFillCap;

        // fillShare: the part of the floor props may take (a clean hall less, a crowded office
        // more). countScale multiplies every placement's count, required ones excepted;
        // concealmentScale multiplies it again for placements of concealment props.
        public RoomPropPlanner(RoomStructure structure, Vector2 anchor, RandomStream random, float fillShare = DefaultFillShare,
                               float countScale = 1f, float concealmentScale = 1f)
        {
            _structure  = structure;
            _anchor     = anchor;
            _random     = random;
            _fillCap    = Mathf.FloorToInt(structure.Footprint.Count * Mathf.Clamp01(fillShare));
            _countScale = Mathf.Max(0f, countScale);
            _concealmentScale = Mathf.Max(0f, concealmentScale);

            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (structure.Has(tile, RoomTileTags.Structure | RoomTileTags.Prop)) _claimed++;

            _bounds   = structure.Footprint.Bounds;
            _sight    = new SightlineMap(structure);
            _routes   = new RouteMap(structure);
            _rules    = new PropPlacementRules(structure, anchor, _sight, _routes, _keyed);
            _selector = new PropSelector(_rules, _sight, random, structure);
        }

        public IReadOnlyList<PlannedProp> Planned => _planned;
        // How the furnished room reads on each trait, from the sightlines kept while placing.
        public RoomTraitReading Traits => RoomTraitMeasure.Measure(_structure, _planned, _sight);
        // One per placement added, in order: what it asked for and what it got.
        public IReadOnlyList<PropPlacementOutcome> Outcomes => _outcomes;

        // `exactConcealment`: concealment placements keep their counts whatever the
        // concealment scale (what a room type needs whoever holds it). `zones`: only in these
        // zones of the room (one faction's side of a Breach room), with counts that scale by
        // room size read from their floor alone; null = the whole room. `countScale`: these
        // placements' counts only, on top of the room's (a frontier faction's thinner props).
        public void Add(IEnumerable<RoomPropPlacement> placements, bool exactConcealment = false, ICollection<int> zones = null,
                        float countScale = 1f)
        {
            foreach (RoomPropPlacement placement in placements) Add(placement, exactConcealment, zones, countScale);
        }

        // Places one kind of furnishing; returns how many it placed.
        public int Add(RoomPropPlacement placement, bool exactConcealment = false, ICollection<int> zones = null, float countScale = 1f)
        {
            if (placement == null || !placement.HasPieces) return 0;
            if (zones != null && zones.Count == 0) return 0;
            // A piece for bigger rooms is left out of a small one: not a shortfall.
            if (placement.MinRoomTiles > 0 && FloorTiles(zones) < placement.MinRoomTiles) return 0;

            int count = Scaled(placement, RollCount(placement, zones), exactConcealment, countScale);
            if (count <= 0) return 0;

            _hitFillCap = false;
            var own = new List<RectInt>();
            int placed = Fill(placement, count, required: false, zones, own);
            bool hitFillCap = _hitFillCap;
            bool relaxed = false;
            for (int level = 1; placed == 0 && placement.Required && level <= 2; level++)
            {
                placed  = Fill(placement.Relaxed(level), 1, required: true, zones, own);
                relaxed = placed > 0;
            }
            Remember(placement.Key, own);
            _outcomes.Add(new PropPlacementOutcome(placement, count, placed, hitFillCap, relaxed));
            return placed;
        }

        private void Remember(string key, List<RectInt> placed)
        {
            if (string.IsNullOrEmpty(key) || placed.Count == 0) return;
            if (!_keyed.TryGetValue(key, out List<RectInt> rects)) _keyed[key] = rects = new List<RectInt>();
            rects.AddRange(placed);
        }

        private int RollCount(RoomPropPlacement placement, ICollection<int> zones)
        {
            int count = placement.Count.Evaluate(_random);
            if (!placement.ScaleByRoomSize || count <= 0) return count;

            float scale = Mathf.Clamp(FloorTiles(zones) / ReferenceTiles, MinScale, MaxScale);
            return Mathf.Max(1, Mathf.RoundToInt(count * scale));
        }

        private int FloorTiles(ICollection<int> zones)
        {
            if (zones == null) return _structure.Footprint.Count;
            int tiles = 0;
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                if (zones.Contains(_structure.ZoneOf(tile))) tiles++;
            return tiles;
        }

        // What the room's style does to a placement's count: Required placements are exempt.
        private int Scaled(RoomPropPlacement placement, int count, bool exactConcealment, float countScale)
        {
            if (placement.Required || count <= 0) return count;

            float scale = _countScale * Mathf.Max(0f, countScale);
            if (!exactConcealment && !Mathf.Approximately(_concealmentScale, 1f) && Conceals(placement)) scale *= _concealmentScale;
            return Mathf.Approximately(scale, 1f) ? count : Mathf.Max(1, Mathf.RoundToInt(count * scale));
        }

        private static bool Conceals(RoomPropPlacement placement)
        {
            foreach (GameObject prefab in placement.Prefabs)
                if (RoomTraitMeasure.Conceals(prefab)) return true;
            if (!placement.HasMembers) return false;
            foreach (PropMember member in placement.Members)
                foreach (GameObject prefab in member.Prefabs)
                    if (RoomTraitMeasure.Conceals(prefab)) return true;
            return false;
        }

        // --- Choosing -------------------------------------------------------------------

        // `own`: what this placement has put down, added to as it goes. A placement that grows
        // with the room tries its largest footprint first, then smaller ones for what is left.
        private int Fill(RoomPropPlacement placement, int count, bool required, ICollection<int> zones, List<RectInt> own)
        {
            int placed = 0;
            foreach (Vector2Int size in _rules.Footprints(placement))
            {
                placed += FillAt(size, placement, count - placed, required, zones, own);
                if (placed >= count || _hitFillCap) break;
            }
            return placed;
        }

        private int FillAt(Vector2Int size, RoomPropPlacement placement, int count, bool required, ICollection<int> zones, List<RectInt> own)
        {
            List<PropCandidate> pool = _rules.Candidates(placement, size, zones);
            _random.Shuffle(pool);

            int placed = 0;
            while (placed < count)
            {
                if (!required && UsesFill(placement) && _claimed + size.x * size.y > _fillCap)
                {
                    _hitFillCap = true;
                    break;
                }

                int index = _selector.Select(placement, pool, own);
                if (index < 0) break;

                PropCandidate chosen = pool[index];
                pool.RemoveAt(index);
                if (!TryPlace(chosen, placement, size, own)) continue;

                placed++;
                if (placed < count && placement.Symmetric && TryPlaceTwin(chosen, placement, size, own, required, zones)) placed++;
            }
            return placed;
        }

        // The mirror image of a placed prop, if the room has the floor for it.
        private bool TryPlaceTwin(PropCandidate placed, RoomPropPlacement placement, Vector2Int size, List<RectInt> own, bool required,
                                  ICollection<int> zones)
        {
            PropCandidate twin = Mirror(placed);
            if (twin.Rect.Equals(placed.Rect) || (!required && UsesFill(placement) && _claimed + size.x * size.y > _fillCap)) return false;
            return _rules.Suits(twin, placement, size, checkLattice: false, zones) && _rules.Placeable(twin, placement, own) && TryPlace(twin, placement, size, own);
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

        private bool TryPlace(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, List<RectInt> own)
        {
            if (placement.Decor)
            {
                Emit(candidate, placement, size);
                own.Add(candidate.Rect);
                return true;
            }

            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in candidate.Rect.allPositionsWithin) tiles.Add(tile);
            if (!_structure.TryClaim(tiles, RoomTileTags.Prop)) return false;

            ReserveAccess(candidate, placement.Clearance);
            if (placement.MarksRoute) MarkRoute(candidate);
            _sight.Refresh(candidate.Rect);
            Emit(candidate, placement, size);
            own.Add(candidate.Rect);
            _selector.NotePlaced(candidate.Rect.center);
            if (UsesFill(placement)) _claimed += tiles.Count;
            return true;
        }

        // Decorations take no floor, and a feature that grows with the room is part of its plan.
        private static bool UsesFill(RoomPropPlacement placement) => !placement.Decor && !placement.GrowsWithRoom;

        private void ReserveAccess(PropCandidate candidate, int depth)
        {
            Vector2Int forward = candidate.Forward;
            PropPlacementRules.SideSpan(candidate.Rect, forward, out Vector2Int first, out Vector2Int along, out int length);
            for (int i = 0; i < length; i++)
                for (int step = 1; step <= depth; step++)
                    _structure.Tag(first + along * i + forward * step, RoomTileTags.Access);
        }

        // The row of floor in front of a door-like prop: where its traffic comes out.
        private void MarkRoute(PropCandidate candidate)
        {
            Vector2Int forward = candidate.Forward;
            PropPlacementRules.SideSpan(candidate.Rect, forward, out Vector2Int first, out Vector2Int along, out int length);
            for (int i = 0; i < length; i++) _routes.Mark(first + along * i + forward);
        }

        // `size`: the footprint it was given, across and front to back (before turning).
        private void Emit(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size)
        {
            bool single = size == Vector2Int.one;
            Vector2 centre = candidate.Rect.center;
            Vector2 forward = candidate.Forward;
            float yaw = YawFor(candidate, placement, size, centre);

            int pile = single && !placement.HasMembers ? Mathf.Clamp(placement.Pile.Evaluate(_random), 1, MaxPile) : 1;
            Vector2 position = centre;
            if (placement.Facing == PropFacing.AgainstWall && size.y == 1 && pile == 1) position -= forward * WallOffset;
            if (placement.Jitter > 0f && single && !placement.Symmetric) position += Jitter(placement, forward);

            if (pile == 1)
            {
                Vector2 stretch = placement.Stretch ? (Vector2)size : Vector2.one;
                if (placement.Prefabs != null && placement.Prefabs.Length > 0)
                    _planned.Add(new PlannedProp(_random.Pick(placement.Prefabs), position, yaw, candidate.Rect, placement.Decor, stretch,
                                                 placement.Decor ? null : placement.Activity));
                if (placement.HasMembers) EmitMembers(placement, position, yaw, candidate.Rect);
                return;
            }

            var quarters = new List<Vector2>(Quarters);
            _random.Shuffle(quarters);
            for (int i = 0; i < pile; i++)
            {
                float turn = _random.Range(-PileTurn, PileTurn);
                _planned.Add(new PlannedProp(_random.Pick(placement.Prefabs), position + quarters[i] * PileOffset, yaw + turn,
                                             candidate.Rect, placement.Decor));
            }
        }

        // A group's members, turned with it about its middle; each comes in by its chance.
        private void EmitMembers(RoomPropPlacement placement, Vector2 position, float yaw, RectInt tiles)
        {
            float turn = yaw * Mathf.Deg2Rad;
            var front = new Vector2(Mathf.Sin(turn), Mathf.Cos(turn));
            var right = new Vector2(front.y, -front.x);
            foreach (PropMember member in placement.Members)
            {
                if (member == null || member.Prefabs == null || member.Prefabs.Length == 0) continue;
                if (member.Chance < 1f && !_random.Chance(member.Chance)) continue;

                Vector2 at = position + right * member.Offset.x + front * member.Offset.y;
                _planned.Add(new PlannedProp(_random.Pick(member.Prefabs), at, yaw + member.Yaw, tiles, placement.Decor));
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
                case PropFacing.TowardNear when _rules.TryNearest(candidate.Rect, placement.Near, int.MaxValue, out RectInt target):
                    return Yaw(target.center - centre);
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
