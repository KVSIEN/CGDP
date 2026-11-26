using System;
using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Lifts one part of the room a level: half of it against one wall, or a corner. The room is
    // laid out as it would be anyway; the walls, pillars and later the props of that part simply
    // stand a metre higher, with flights of stairs along the step, first where a route from a
    // doorway crosses it. So it goes last in a structure list (ElevationBudget runs it after
    // everything else).
    //
    // Role (ElevationRole) says what the part is for in play: a split level (the usual) has open
    // or railed steps with stairs wherever a route crosses; a perch has Wall along its longest
    // step and its other step open to climb; a bastion has Wall all along, so its stairs, two at
    // least on different stretches of the step, are the only ways up (ElevationAccess).
    //
    // Doorways stay on level 0: a part clear of every doorway is looked for first, and only then
    // one that keeps a landing round a doorway inside it. A part that would cut through a hut or
    // building, or across a doorway in an inner wall, is passed over, as is floor already raised
    // or sunk. Does nothing, and draws no random number, in a room that takes no raised floor.
    [CreateAssetMenu(fileName = "RaisedSectionStructureRule", menuName = "CGD/Level/Structure/Raised Section")]
    public class RaisedSectionStructureRule : RoomStructureRule
    {
        // Places and sizes tried, first keeping clear of doorways, then with landings.
        private const int Attempts = 32;
        // Flights along the step keep at least this many tiles apart.
        private const int StairGap = 3;
        // The narrowest a part may be.
        private const int MinSide = 3;
        // A doorway inside the part keeps the floor this many tiles round it on level 0: its front
        // (RoomTileTags.NearDoor) and a ring beyond it for a flight up to stand on.
        private const int LandingReach = 3;

        [Tooltip("What the part is for in play. Split Level: open or railed step (Rail Chance), stairs where every route crosses it. Perch: Wall along its longest step, the other open to climb, one or two flights. Bastion: Wall along all of it, two flights at least, the only ways up")]
        [SerializeField] private ElevationRole _role = ElevationRole.SplitLevel;
        [Tooltip("Perch and Bastion: what stands along the walled step. Half Wall: solid, low cover. Railing: seen and shot through")]
        [SerializeField] private LedgeGuard _wall = LedgeGuard.HalfWall;
        [Tooltip("The chance the part is a corner of the room; otherwise it is a half, running the length of one wall")]
        [SerializeField, Range(0f, 1f)] private float _cornerChance = 0.5f;
        [Tooltip("A half's depth, as a share of the room's width or length")]
        [SerializeField] private FloatRange _halfDepth = new(0.35f, 0.5f);
        [Tooltip("A corner's width and length, each as a share of the room's")]
        [SerializeField] private FloatRange _cornerSide = new(0.45f, 0.65f);
        [Tooltip("The least and most of the room's floor the part may be")]
        [SerializeField] private FloatRange _floorShare = new(0.18f, 0.55f);
        [Tooltip("Tiles of step for each flight of stairs; two flights at least where there is room")]
        [SerializeField, Min(StairGap)] private int _stairSpacing = 5;
        [Tooltip("Split Level: the chance the open step gets a railing; without one it can be dropped off")]
        [SerializeField, Range(0f, 1f)] private float _railChance = 0.6f;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            if (!structure.AllowsFloor(1)) return;

            foreach (bool landings in new[] { false, true })
                for (int attempt = 0; attempt < Attempts; attempt++)
                    if (TryRaise(structure, random, Pick(structure.Footprint.Bounds, random), landings)) return;
        }

        // A half or a corner of the bounds.
        private RectInt Pick(RectInt bounds, RandomStream random)
        {
            bool corner = random.Chance(_cornerChance);
            bool highX = random.Chance(0.5f), highY = random.Chance(0.5f);
            var size = new Vector2Int(bounds.width, bounds.height);
            if (corner)
                size = new Vector2Int(Side(bounds.width, _cornerSide, random), Side(bounds.height, _cornerSide, random));
            else if (random.Chance(0.5f))
                size.x = Side(bounds.width, _halfDepth, random);
            else
                size.y = Side(bounds.height, _halfDepth, random);

            var at = new Vector2Int(highX ? bounds.xMax - size.x : bounds.xMin, highY ? bounds.yMax - size.y : bounds.yMin);
            return new RectInt(at, size);
        }

        private static int Side(int whole, FloatRange share, RandomStream random) =>
            Mathf.Clamp(Mathf.RoundToInt(whole * share.Evaluate(random)), Mathf.Min(MinSide, whole), whole);

        private bool TryRaise(RoomStructure structure, RandomStream random, RectInt part, bool landings)
        {
            if (CutsASpace(structure, part)) return false;

            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (!part.Contains(tile)) continue;
                if (NearAnEntrance(structure, tile))
                {
                    if (!landings) return false;
                    continue;
                }
                tiles.Add(tile);
            }

            int floor = structure.Footprint.Count;
            if (tiles.Count < floor * _floorShare.Min || tiles.Count > floor * _floorShare.Max) return false;

            var inside = new HashSet<Vector2Int>(tiles);
            foreach (PartitionEdge opening in structure.Openings)
                if (inside.Contains(opening.Tile) != inside.Contains(opening.Other)) return false;

            List<StairFlight> stairs = ChooseStairs(structure, tiles, inside, random);
            if (stairs.Count == 0) return false;

            LedgeGuard open = ElevationAccess.RollsRailing(_role) && random.Chance(_railChance) ? LedgeGuard.Railing : LedgeGuard.None;
            LedgeGuards? guards = ElevationAccess.Fit(_role, ElevationAccess.Guards(_role, structure, inside, open, _wall), stairs.Count);
            return guards != null && structure.TryAddElevation(new ElevatedArea(ElevationKind.Section, 1, tiles, stairs, guards.Value));
        }

        // One flight to every separate piece of the part (inner walls may split it), then, on a
        // split level, one wherever a route crosses the step, then more spread along it until
        // there is one per spacing of step, as many as the role asks (ElevationAccess).
        private List<StairFlight> ChooseStairs(RoomStructure structure, List<Vector2Int> tiles, HashSet<Vector2Int> inside,
                                               RandomStream random)
        {
            int step = 0;
            var candidates = new List<(StairFlight stairs, bool route, float score, int order)>();
            foreach (Vector2Int tile in tiles)
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int below = tile + side;
                    if (!structure.Contains(below) || inside.Contains(below) || structure.IsPartitioned(tile, below)) continue;

                    step++;
                    var stairs = new StairFlight(below, -side);
                    if (!CanStand(structure, stairs, inside)) continue;
                    bool route = structure.Has(stairs.Tile, RoomTileTags.Walkway) && structure.Has(stairs.Head, RoomTileTags.Walkway);
                    candidates.Add((stairs, route, random.Value, candidates.Count));
                }
            // Routes first; List.Sort is unstable, so ties fall back to the order they were found in.
            candidates.Sort((a, b) => a.route != b.route ? b.route.CompareTo(a.route)
                                    : a.score != b.score ? a.score.CompareTo(b.score) : a.order.CompareTo(b.order));
            var preferred = new List<StairFlight>(candidates.Count);
            var routes = new HashSet<Vector2Int>();
            foreach (var candidate in candidates)
            {
                preferred.Add(candidate.stairs);
                if (candidate.route) routes.Add(candidate.stairs.Tile);
            }

            int wanted = ElevationAccess.Ways(_role, Mathf.Max(2, Mathf.RoundToInt(step / (float)_stairSpacing)), tiles.Count);
            // Only a split level is crossed wherever a route meets it; a guarded part is crossed where its role puts the stairs.
            Predicate<StairFlight> always = _role == ElevationRole.SplitLevel ? stairs => routes.Contains(stairs.Tile) : null;
            return ElevationAccess.Pick(preferred, Pieces(structure, tiles), (stairs, piece) => piece.Contains(stairs.Head), always,
                                        wanted, StairGap);
        }

        // Free floor for the flight and both landings, the foot below the step beside it, no wall across.
        private static bool CanStand(RoomStructure structure, StairFlight stairs, HashSet<Vector2Int> inside)
        {
            if (!structure.IsFree(stairs.Tile) || !structure.IsFree(stairs.Foot) || !structure.IsFree(stairs.Head)) return false;
            if (structure.Has(stairs.Tile, RoomTileTags.NearDoor | RoomTileTags.Centre) || inside.Contains(stairs.Foot)) return false;
            return !structure.IsPartitioned(stairs.Foot, stairs.Tile) && !structure.IsPartitioned(stairs.Tile, stairs.Head);
        }

        // The part's walkable tiles split where inner walls or pillars separate them.
        private static List<HashSet<Vector2Int>> Pieces(RoomStructure structure, List<Vector2Int> tiles)
        {
            var left = new HashSet<Vector2Int>();
            foreach (Vector2Int tile in tiles)
                if (structure.IsFree(tile)) left.Add(tile);

            var pieces = new List<HashSet<Vector2Int>>();
            foreach (Vector2Int start in tiles)
            {
                if (!left.Remove(start)) continue;
                var piece = new HashSet<Vector2Int> { start };
                var open = new Stack<Vector2Int>();
                open.Push(start);
                while (open.Count > 0)
                {
                    Vector2Int tile = open.Pop();
                    foreach (Vector2Int side in GridNeighbours.Four)
                        if (!structure.IsPartitioned(tile, tile + side) && left.Remove(tile + side))
                        {
                            piece.Add(tile + side);
                            open.Push(tile + side);
                        }
                }
                pieces.Add(piece);
            }
            return pieces;
        }

        private static bool NearAnEntrance(RoomStructure structure, Vector2Int tile)
        {
            foreach (Vector2Int entrance in structure.Entrances)
                if (Mathf.Abs(tile.x - entrance.x) + Mathf.Abs(tile.y - entrance.y) <= LandingReach) return true;
            return false;
        }

        // Whether the part takes some of a hut or building but not all of it.
        private static bool CutsASpace(RoomStructure structure, RectInt part)
        {
            foreach (StructureSpace space in structure.Spaces)
                if (space.HasArea && part.Overlaps(space.Area)
                    && !(part.Contains(space.Area.min) && part.Contains(space.Area.max - Vector2Int.one))) return true;
            return false;
        }
    }
}
