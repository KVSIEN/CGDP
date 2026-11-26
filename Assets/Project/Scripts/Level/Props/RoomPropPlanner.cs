using System;
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
    //
    // Placements added as *refinable* may be moved afterwards, one prop at a time (IRefinablePlan,
    // RoomPlanRefiner): the loose cover and clutter that a room's traits depend on.
    public class RoomPropPlanner : IRefinablePlan
    {
        // How far an AgainstWall prop one tile deep moves from its tile's middle toward the wall, in tiles, at most:
        // a deep prop (a bed) is set back less, so its back stops short of the wall.
        private const float WallOffset = 0.25f;
        // A tile is 3 m in the placeholder kit; a wall's half thickness, in tiles, the back must stay clear of.
        private const float TileMetres = 3f;
        private const float WallClearance = 0.04f;
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
        private readonly RandomStream _random;
        private readonly int _fillCap;
        private readonly float _countScale;
        private readonly float _concealmentScale;
        private readonly RectInt _bounds;
        // What a standing eye sees (the traits, the arrival's view) and what a crouching one does:
        // cover goes where someone crouched has nothing to hide behind, so a crate breaks a
        // crouched firing lane though a standing eye sees over it.
        private readonly SightlineMap _sight;
        private readonly SightlineMap _coverSight;
        // What can be seen from each tile, in every direction, standing and crouched; only for the
        // readers that asked for it (IsovistUse).
        private readonly RoomVisibility _visibility;
        private readonly RoomVisibility _coverVisibility;
        private readonly bool _traitsUseIsovist;
        private readonly RouteMap _routes;
        private readonly PropPlacementRules _rules;
        private readonly PropSelector _selector;
        private readonly List<PlannedProp> _planned = new();
        private readonly List<PropPlacementOutcome> _outcomes = new();
        // What each keyed placement put down (RoomPropPlacement.Key), for the ones Near it.
        private readonly Dictionary<string, List<RectInt>> _keyed = new();
        // The props that may be moved (placements added refinable, RoomPropPlacement.CanRelocate), in the order they went down.
        private readonly List<PlacedPiece> _pieces = new();
        private int _claimed;
        private bool _hitFillCap;
        // Whether the placement being added records its props as movable pieces.
        private bool _recordsPieces;

        // fillShare: the part of the floor props may take (a clean hall less, a crowded office
        // more). countScale multiplies every placement's count, required ones excepted;
        // concealmentScale multiplies it again for placements of concealment props. `isovist`: which
        // readings of the room see in every direction rather than along rows and columns.
        public RoomPropPlanner(RoomStructure structure, Vector2 anchor, RandomStream random, float fillShare = DefaultFillShare,
                               float countScale = 1f, float concealmentScale = 1f, IsovistUse isovist = IsovistUse.None)
        {
            _structure  = structure;
            _random     = random;
            _fillCap    = Mathf.FloorToInt(structure.Footprint.Count * Mathf.Clamp01(fillShare));
            _countScale = Mathf.Max(0f, countScale);
            _concealmentScale = Mathf.Max(0f, concealmentScale);

            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (structure.Has(tile, RoomTileTags.Structure | RoomTileTags.Prop)) _claimed++;

            _bounds   = structure.Footprint.Bounds;
            _sight      = new SightlineMap(structure);
            _coverSight = new SightlineMap(structure, new SightBlocking(structure, SightBlocking.CrouchingEyeHeight));
            _traitsUseIsovist = (isovist & IsovistUse.Traits) != 0;
            if (_traitsUseIsovist) _visibility = new RoomVisibility(structure, _sight.Blocking);
            if ((isovist & IsovistUse.Cover) != 0) _coverVisibility = new RoomVisibility(structure, _coverSight.Blocking);
            _routes   = new RouteMap(structure);
            _rules    = new PropPlacementRules(structure, anchor, _coverSight, _routes, _keyed);
            _selector = new PropSelector(_rules, _sight, _coverSight, random, structure, _coverVisibility);
        }

        public IReadOnlyList<PlannedProp> Planned => _planned;
        // How the furnished room reads on each trait, from the sightlines kept while placing.
        public RoomTraitReading Traits => RoomTraitMeasure.Measure(_structure, _planned, _sight, _traitsUseIsovist ? _visibility : null);
        // One per placement added, in order: what it asked for and what it got.
        public IReadOnlyList<PropPlacementOutcome> Outcomes => _outcomes;

        // `exactConcealment`: concealment placements keep their counts whatever the
        // concealment scale (what a room type needs whoever holds it). `zones`: only in these
        // zones of the room (one faction's side of a Breach room), with counts that scale by
        // room size read from their floor alone; null = the whole room. `countScale`: these
        // placements' counts only, on top of the room's (a frontier faction's thinner props).
        // `refinable`: the props may be moved afterwards, where their placement allows it.
        public void Add(IEnumerable<RoomPropPlacement> placements, bool exactConcealment = false, ICollection<int> zones = null,
                        float countScale = 1f, bool refinable = false)
        {
            foreach (RoomPropPlacement placement in placements) Add(placement, exactConcealment, zones, countScale, refinable);
        }

        // Places one kind of furnishing; returns how many it placed.
        public int Add(RoomPropPlacement placement, bool exactConcealment = false, ICollection<int> zones = null, float countScale = 1f,
                       bool refinable = false)
        {
            if (placement == null || !placement.HasPieces) return 0;
            if (zones != null && zones.Count == 0) return 0;
            // A piece for bigger rooms is left out of a small one: not a shortfall.
            if (placement.MinRoomTiles > 0 && FloorTiles(zones) < placement.MinRoomTiles) return 0;

            _recordsPieces = refinable && placement.CanRelocate;

            int asked = 0, placed = 0;
            bool hitFillCap = false, relaxed = false;
            foreach (ICollection<int> area in Areas(placement, zones))
            {
                int count = Scaled(placement, RollCount(placement, area), exactConcealment, countScale);
                if (count <= 0) continue;

                asked += count;
                placed += PlaceIn(placement, count, area, ref hitFillCap, ref relaxed);
            }
            if (asked > 0) _outcomes.Add(new PropPlacementOutcome(placement, asked, placed, hitFillCap, relaxed));
            return placed;
        }

        // Where a placement's count applies: the zones it was given as one area, or with Each
        // Zone, every zone it may use on its own (each cabin of a row, each bay of a ward).
        private IEnumerable<ICollection<int>> Areas(RoomPropPlacement placement, ICollection<int> zones)
        {
            if (!placement.EachZone)
            {
                yield return zones;
                yield break;
            }
            for (int zone = 0; zone < _structure.ZoneCount; zone++)
            {
                if (zones != null && !zones.Contains(zone)) continue;
                if (placement.Zone == PropZone.Largest && zone != _structure.LargestZone) continue;
                if (placement.Zone == PropZone.Others && zone == _structure.LargestZone) continue;
                yield return new[] { zone };
            }
        }

        // Puts down up to `count` in one area; a Required placement that finds no spot is
        // loosened until it does.
        private int PlaceIn(RoomPropPlacement placement, int count, ICollection<int> zones, ref bool hitFillCap, ref bool relaxed)
        {
            _hitFillCap = false;
            var own = new List<RectInt>();
            int placed = Fill(placement, count, required: false, zones, own);
            hitFillCap |= _hitFillCap;
            for (int level = 1; placed == 0 && placement.Required && level <= 2; level++)
            {
                placed  = Fill(placement.Relaxed(level), 1, required: true, zones, own);
                relaxed |= placed > 0;
            }
            Remember(placement.Key, own);
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
                if (!TryPlace(chosen, placement, size, own, zones)) continue;

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
            return _rules.Suits(twin, placement, size, checkLattice: false, zones) && _rules.Placeable(twin, placement, own) && TryPlace(twin, placement, size, own, zones);
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

        private bool TryPlace(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, List<RectInt> own, ICollection<int> zones)
        {
            int firstPlanned = _planned.Count;
            if (placement.Decor)
            {
                Emit(candidate, placement, size);
                own.Add(candidate.Rect);
                Record(candidate, placement, size, zones, firstPlanned);
                return true;
            }

            List<Vector2Int> tiles = TilesOf(candidate.Rect);
            if (!_structure.TryClaim(tiles, RoomTileTags.Prop)) return false;

            ReserveAccess(candidate, placement.Clearance);
            if (placement.MarksRoute) MarkRoute(candidate);
            Emit(candidate, placement, size);
            SetHeight(candidate.Rect, TallestSince(firstPlanned));
            Changed(candidate.Rect);
            own.Add(candidate.Rect);
            _selector.NotePlaced(candidate.Rect.center);
            if (UsesFill(placement)) _claimed += tiles.Count;
            Record(candidate, placement, size, zones, firstPlanned);
            return true;
        }

        // A prop that went down as one entry of the plan, from a placement that allows it, may be moved later.
        private void Record(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, ICollection<int> zones, int firstPlanned)
        {
            if (_recordsPieces && _planned.Count - firstPlanned == 1)
                _pieces.Add(new PlacedPiece(placement, size, zones, candidate, firstPlanned));
        }

        private static List<Vector2Int> TilesOf(RectInt rect)
        {
            var tiles = new List<Vector2Int>(rect.width * rect.height);
            foreach (Vector2Int tile in rect.allPositionsWithin) tiles.Add(tile);
            return tiles;
        }

        // The tallest of the props planned from `first` on: the pieces one placement just put on its
        // tiles (a pile, a group and its members). Infinite when it put none down.
        private float TallestSince(int first)
        {
            if (first >= _planned.Count) return float.PositiveInfinity;

            float tallest = 0f;
            for (int i = first; i < _planned.Count; i++) tallest = Mathf.Max(tallest, PropHeights.Of(_planned[i].Prefab));
            return tallest;
        }

        // How tall what stands on a prop's tiles is, before what can be seen across them is measured again.
        private void SetHeight(RectInt rect, float height)
        {
            foreach (Vector2Int tile in rect.allPositionsWithin) _structure.SetPropHeight(tile, height);
        }

        // Floor props stand on changed: what can be seen across it is measured again.
        private void Changed(RectInt rect)
        {
            _sight.Refresh(rect);
            _coverSight.Refresh(rect);
            _visibility?.Invalidate(rect);
            _coverVisibility?.Invalidate(rect);
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
            float yaw = YawFor(candidate, placement, size, _random);

            int pile = single && !placement.HasMembers ? Mathf.Clamp(placement.Pile.Evaluate(_random), 1, MaxPile) : 1;
            Vector2 position = PositionOf(candidate, placement, size, pile, _random);

            if (pile == 1)
            {
                if (placement.Prefabs != null && placement.Prefabs.Length > 0)
                    _planned.Add(Single(_random.Pick(placement.Prefabs), position, yaw, candidate, placement, size));
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

        // Loose clutter takes any angle; everything else its candidate's quarter turn.
        private static float YawFor(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, RandomStream random) =>
            PropPlacementRules.HasFreeYaw(placement, size) ? random.Range(0f, 360f) : candidate.Rotation * 90f;

        // Where a prop of a placement stands on its tiles: the middle, set back against the wall
        // for a wall prop one tile deep, and jittered where the placement asks.
        private static Vector2 PositionOf(PropCandidate candidate, RoomPropPlacement placement, Vector2Int size, int pile, RandomStream random)
        {
            Vector2 forward = candidate.Forward;
            Vector2 position = candidate.Rect.center;
            if (placement.Facing == PropFacing.AgainstWall && size.y == 1 && pile == 1) position -= forward * SetBack(placement);
            if (placement.Jitter > 0f && size == Vector2Int.one && !placement.Symmetric) position += Jitter(placement, forward, random);
            return position;
        }

        // How far a wall prop sits from its tile's middle toward the wall: the usual set-back, less
        // for a deep prop so its back (the deepest of its prefabs) stays inside the tile.
        private static float SetBack(RoomPropPlacement placement)
        {
            float behind = 0f;
            if (placement.Prefabs != null)
                foreach (GameObject prefab in placement.Prefabs)
                    behind = Mathf.Max(behind, PropDepth.BehindOrigin(prefab));
            return Mathf.Clamp(0.5f - behind / TileMetres - WallClearance, 0f, WallOffset);
        }

        // Along the wall for wall props, any way for the rest.
        private static Vector2 Jitter(RoomPropPlacement placement, Vector2 forward, RandomStream random)
        {
            float reach = placement.Jitter * JitterReach;
            if (placement.Facing == PropFacing.AgainstWall)
                return new Vector2(Mathf.Abs(forward.y), Mathf.Abs(forward.x)) * random.Range(-reach, reach);
            return new Vector2(random.Range(-reach, reach), random.Range(-reach, reach));
        }

        // One prop of a placement, on the tiles of its candidate.
        private static PlannedProp Single(GameObject prefab, Vector2 position, float yaw, PropCandidate candidate, RoomPropPlacement placement, Vector2Int size)
        {
            Vector2 stretch = placement.Stretch ? (Vector2)size : Vector2.one;
            return new PlannedProp(prefab, position, yaw, candidate.Rect, placement.Decor, stretch, placement.Decor ? null : placement.Activity);
        }

        // --- Refining -------------------------------------------------------------------

        public int MovableCount => _pieces.Count;

        // Moves one piece to a random other spot that suits its placement (its rules, spacing
        // from the others of its kind and a room that stays whole), keeping its prefab. The
        // rest of the plan is untouched, and `undo` puts it all back.
        public bool TryMove(int index, RandomStream random, out Action undo)
        {
            undo = null;
            PlacedPiece piece = _pieces[index];
            RoomPropPlacement placement = piece.Placement;
            PropCandidate origin = piece.Spot;
            PlannedProp before = _planned[piece.PlanIndex];

            // Its own floor is free to move to, so it is let go before a spot is looked for.
            Vacate(placement, origin.Rect);
            if (!TryRelocate(piece, origin, random))
            {
                Reoccupy(placement, origin.Rect, before.Prefab);
                return false;
            }

            PropCandidate destination = piece.Spot;
            undo = () =>
            {
                Vacate(placement, destination.Rect);
                Reoccupy(placement, origin.Rect, before.Prefab);
                piece.Spot = origin;
                _planned[piece.PlanIndex] = before;
            };
            return true;
        }

        private bool TryRelocate(PlacedPiece piece, PropCandidate origin, RandomStream random)
        {
            RoomPropPlacement placement = piece.Placement;
            List<PropCandidate> pool = _rules.Candidates(placement, piece.Size, piece.Zones);
            List<RectInt> others = RectsOfOthers(piece);
            while (pool.Count > 0)
            {
                int pick = random.Range(0, pool.Count);
                PropCandidate spot = pool[pick];
                pool[pick] = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);

                if (spot.Rect.Equals(origin.Rect) && spot.Rotation == origin.Rotation) continue;
                if (!_rules.Placeable(spot, placement, others) || !_rules.Eligible(spot, placement)) continue;
                if (!placement.Decor && !_structure.TryClaim(TilesOf(spot.Rect), RoomTileTags.Prop)) continue;

                Settle(piece, spot, random);
                return true;
            }
            return false;
        }

        // The tiles of the other pieces from the same placement: what its spacing keeps clear of.
        private List<RectInt> RectsOfOthers(PlacedPiece piece)
        {
            var rects = new List<RectInt>();
            foreach (PlacedPiece other in _pieces)
                if (other != piece && other.Placement == piece.Placement) rects.Add(other.Spot.Rect);
            return rects;
        }

        // The piece stands on its new spot, turned and set on it as a freshly placed one would be.
        private void Settle(PlacedPiece piece, PropCandidate spot, RandomStream random)
        {
            RoomPropPlacement placement = piece.Placement;
            if (!placement.Decor)
            {
                if (UsesFill(placement)) _claimed += spot.Rect.width * spot.Rect.height;
                SetHeight(spot.Rect, PropHeights.Of(_planned[piece.PlanIndex].Prefab));
                Changed(spot.Rect);
            }
            float yaw = YawFor(spot, placement, piece.Size, random);
            Vector2 position = PositionOf(spot, placement, piece.Size, 1, random);
            piece.Spot = spot;
            _planned[piece.PlanIndex] = Single(_planned[piece.PlanIndex].Prefab, position, yaw, spot, placement, piece.Size);
        }

        // Lets go of a piece's floor, as when it moves away (a decoration takes none).
        private void Vacate(RoomPropPlacement placement, RectInt rect)
        {
            if (placement.Decor) return;

            _structure.Release(TilesOf(rect), RoomTileTags.Prop);
            if (UsesFill(placement)) _claimed -= rect.width * rect.height;
            Changed(rect);
        }

        // Takes a piece's floor back, known to be free and to keep the room whole, as it was before it moved.
        private void Reoccupy(RoomPropPlacement placement, RectInt rect, GameObject prefab)
        {
            if (placement.Decor) return;

            foreach (Vector2Int tile in rect.allPositionsWithin) _structure.Tag(tile, RoomTileTags.Prop);
            SetHeight(rect, PropHeights.Of(prefab));
            if (UsesFill(placement)) _claimed += rect.width * rect.height;
            Changed(rect);
        }
    }
}
