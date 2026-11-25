using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Raises or sinks a feature one level from the doorways' floor, placed so it reads as meant:
    //   - Platform: a block with its back to the room's wall, stairs on its open sides facing into
    //     the room (a stage, a loading deck, a mezzanine);
    //   - Sunken Floor: a block with floor all round it, stairs down from its rim (a lounge pit),
    //     optionally crossed by a bridge of floor level with the room, splitting it in two;
    //   - Mound: a round rise of ground sloping down on every side, its corners filled (a hillock);
    //   - Crater: a round hollow sloping down from every side, its corners filled (a blast crater).
    // None covers a doorway's front, a route from a doorway or the centre spot, so everyone
    // arrives and crosses the room on the doorways' level; every piece has a way up and down, with
    // landings clear at both ends, and every tile stays reachable both ways (RoomStructure.TryAddElevations
    // holds the room to that). Does nothing in a room whose levels don't take its kind
    // (LevelBuildSettings › Lowest and Highest Level), and then draws no random numbers, so the
    // rest of the room is unchanged.
    [CreateAssetMenu(fileName = "ElevationStructureRule", menuName = "CGD/Level/Structure/Elevation")]
    public class ElevationStructureRule : RoomStructureRule
    {
        private const int Attempts = 16;
        // Flights of one area keep at least this many tiles apart, so two don't stand side by side.
        private const int StairSpacing = 3;
        // How far in from the walls a sunken floor's tiles keep: a ring of floor round it, off the room's edge tiles.
        private const int SunkenMargin = 2;
        // A bridge needs this many tiles of sunken floor along its length: two either side of it.
        private const int BridgedLength = 5;
        // A round feature's tiles lie within its radius and this much more, so a radius of 1 is a plus and 2 a round blob.
        private const float RoundSlack = 0.3f;

        [SerializeField] private ElevationKind _kind = ElevationKind.Platform;
        [Tooltip("How many to put in the room")]
        [SerializeField] private IntRange _count = new(1, 1);
        [Tooltip("Platform and Sunken Floor: tiles across (out from the wall for a platform, the short side for a sunken floor)")]
        [SerializeField] private IntRange _depth = new(2, 3);
        [Tooltip("Platform and Sunken Floor: tiles long (along the wall for a platform, the long side for a sunken floor)")]
        [SerializeField] private IntRange _length = new(3, 5);
        [Tooltip("Mound and Crater: tiles from the middle to the edge of the top (a mound's) or the floor (a crater's), slopes not counted. A crater needs 2 at least")]
        [SerializeField] private IntRange _radius = new(1, 2);
        [Tooltip("Platform and Sunken Floor: flights of stairs to each one; fewer are built where there is no room for them, never none (and one at least to each half of a bridged floor). Mounds and craters slope on every side instead")]
        [SerializeField] private IntRange _stairs = new(1, 2);
        [Tooltip("Sunken Floor: a strip of floor level with the room across its middle, splitting it in two (five tiles long at least)")]
        [SerializeField] private bool _bridge;
        [Tooltip("Most of the room's floor these may take together, slopes and corners counted")]
        [SerializeField, Range(0.05f, 0.5f)] private float _maxShare = 0.25f;
        [Tooltip("A railing along the open edges: nobody drops off a platform or into a sunken floor, only takes the stairs. Mounds and craters have no open edge")]
        [SerializeField] private bool _railed;

        public ElevationKind Kind => _kind;
        public int Level => _kind == ElevationKind.Platform || _kind == ElevationKind.Mound ? 1 : -1;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            if (Level < structure.LowestLevel || Level > structure.HighestLevel) return;

            int budget = Mathf.FloorToInt(structure.Footprint.Count * _maxShare);
            int count = _count.Evaluate(random);
            for (int i = 0; i < count; i++)
                for (int attempt = 0; attempt < Attempts; attempt++)
                    if (TryPlaceOne(structure, random, ref budget)) break;
        }

        private bool TryPlaceOne(RoomStructure structure, RandomStream random, ref int budget)
        {
            ElevatedArea area = _kind switch
            {
                ElevationKind.Mound  => RoundFeature(structure, random, raised: true),
                ElevationKind.Crater => RoundFeature(structure, random, raised: false),
                _                    => Block(structure, random),
            };
            if (area == null) return false;

            int size = area.Tiles.Count + area.Corners.Count + (Level > 0 ? area.Stairs.Count : 0);
            if (size > budget || !structure.TryAddElevation(area)) return false;
            budget -= size;
            return true;
        }

        // --- Platforms and sunken floors ----------------------------------------------------

        private ElevatedArea Block(RoomStructure structure, RandomStream random)
        {
            int depth  = Mathf.Max(1, _depth.Evaluate(random));
            int length = Mathf.Max(depth, _length.Evaluate(random));

            List<Vector2Int> tiles = _kind == ElevationKind.Platform
                ? AgainstAWall(structure, random, depth, length, out Vector2Int back)
                : OutInTheRoom(structure, random, depth, length, _bridge, out back);
            if (tiles == null) return null;

            List<StairFlight> stairs = ChooseStairs(structure, tiles, back, random);
            return stairs.Count == 0 ? null : new ElevatedArea(_kind, Level, tiles, stairs, _railed);
        }

        // `depth` rows of `length` tiles standing out from a stretch of the room's wall, all of it
        // free floor nothing keeps clear. `back` is the side the wall is on.
        private static List<Vector2Int> AgainstAWall(RoomStructure structure, RandomStream random, int depth, int length, out Vector2Int back)
        {
            back = GridSides.All[random.Range(0, GridSides.All.Length)];
            RoomFootprint footprint = structure.Footprint;
            var starts = new List<Vector2Int>();
            foreach (Vector2Int tile in footprint.Tiles)
                if (!footprint.Contains(tile + back)) starts.Add(tile);
            if (starts.Count == 0) return null;

            Vector2Int start = starts[random.Range(0, starts.Count)];
            var along = new Vector2Int(back.y, back.x);
            if (random.Chance(0.5f)) along = -along;

            var tiles = new List<Vector2Int>(depth * length);
            for (int i = 0; i < length; i++)
            {
                if (footprint.Contains(start + along * i + back)) return null;   // the back row stays against the wall
                for (int j = 0; j < depth; j++)
                {
                    Vector2Int tile = start + along * i - back * j;
                    if (!Usable(structure, tile)) return null;
                    tiles.Add(tile);
                }
            }
            return tiles;
        }

        // A `depth` by `length` block with floor all round it, away from the walls. No side is a
        // back, so `back` is zero. Bridged: the row across the middle of its length stays level
        // with the room (still free floor, so nothing stands on the bridge).
        private static List<Vector2Int> OutInTheRoom(RoomStructure structure, RandomStream random, int depth, int length, bool bridged, out Vector2Int back)
        {
            back = Vector2Int.zero;
            RoomFootprint footprint = structure.Footprint;
            Vector2Int corner = footprint.Tiles[random.Range(0, footprint.Tiles.Count)];
            bool longAlongX = random.Chance(0.5f);
            Vector2Int size = longAlongX ? new Vector2Int(length, depth) : new Vector2Int(depth, length);
            int bridgeAt = bridged && length >= BridgedLength ? length / 2 : -1;

            var tiles = new List<Vector2Int>(depth * length);
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int tile = corner + new Vector2Int(x, y);
                    if (!footprint.IsInterior(tile, SunkenMargin) || !Usable(structure, tile)) return null;
                    if ((longAlongX ? x : y) != bridgeAt) tiles.Add(tile);
                }
            return tiles;
        }

        // Flights onto a platform from the floor in front of it, or down into a sunken floor from
        // the floor round it, nearest the room's centre first (so they face into the room), kept
        // apart from each other: one to each separate piece of it first, then up to Stairs.
        private List<StairFlight> ChooseStairs(RoomStructure structure, List<Vector2Int> tiles, Vector2Int back, RandomStream random)
        {
            var inside = new HashSet<Vector2Int>(tiles);
            Vector2 centre = structure.Footprint.Anchor;
            var candidates = new List<(StairFlight stairs, float score, int order)>();
            foreach (Vector2Int tile in tiles)
                foreach (Vector2Int side in GridSides.All)
                {
                    if (inside.Contains(tile + side) || side == back) continue;

                    // A platform's flight stands in front of it and climbs onto it; a sunken floor's
                    // stands just inside its rim and climbs out of it.
                    StairFlight stairs = _kind == ElevationKind.Platform ? new StairFlight(tile + side, -side) : new StairFlight(tile, side);
                    if (!CanStand(structure, stairs, inside)) continue;

                    float distance = Vector2.Distance(stairs.Tile + Vector2.one * 0.5f, centre);
                    candidates.Add((stairs, distance + random.Value, candidates.Count));
                }
            // List.Sort is unstable: ties fall back to the order the candidates were found in, so the choice is seeded.
            candidates.Sort((a, b) => a.score != b.score ? a.score.CompareTo(b.score) : a.order.CompareTo(b.order));

            List<HashSet<Vector2Int>> pieces = Pieces(tiles);
            int wanted = Mathf.Max(pieces.Count, _stairs.Evaluate(random));
            var chosen = new List<StairFlight>(wanted);
            foreach (HashSet<Vector2Int> piece in pieces)
                foreach (var candidate in candidates)
                    if (Reaches(candidate.stairs, piece) && FarFromAll(candidate.stairs, chosen))
                    {
                        chosen.Add(candidate.stairs);
                        break;
                    }
            foreach (var candidate in candidates)
            {
                if (chosen.Count >= wanted) break;
                if (FarFromAll(candidate.stairs, chosen)) chosen.Add(candidate.stairs);
            }
            return chosen;
        }

        // Whether the flight's three tiles are free floor of the right kind before the area is
        // raised: its foot and the tile it stands on on one level, its head the area (a platform)
        // or the floor round it (a sunken floor). RoomStructure.TryAddElevations checks the rest.
        private static bool CanStand(RoomStructure structure, StairFlight stairs, HashSet<Vector2Int> inside)
        {
            if (!structure.IsOpen(stairs.Tile) || !structure.IsFree(stairs.Foot) || !structure.IsFree(stairs.Head)) return false;
            if (structure.IsPartitioned(stairs.Foot, stairs.Tile) || structure.IsPartitioned(stairs.Tile, stairs.Head)) return false;
            bool sameSide = inside.Contains(stairs.Foot) == inside.Contains(stairs.Tile);
            return sameSide && inside.Contains(stairs.Tile) != inside.Contains(stairs.Head)
                && structure.LevelOf(stairs.Foot) == 0 && structure.LevelOf(stairs.Head) == 0;
        }

        private static bool Reaches(StairFlight stairs, HashSet<Vector2Int> piece) => piece.Contains(stairs.Tile) || piece.Contains(stairs.Head);

        private static bool FarFromAll(StairFlight stairs, List<StairFlight> chosen)
        {
            foreach (StairFlight other in chosen)
            {
                Vector2Int apart = stairs.Tile - other.Tile;
                if (Mathf.Max(Mathf.Abs(apart.x), Mathf.Abs(apart.y)) < StairSpacing) return false;
            }
            return true;
        }

        // The tiles split into pieces joined side to side (a bridged floor's two halves).
        private static List<HashSet<Vector2Int>> Pieces(List<Vector2Int> tiles)
        {
            var left = new HashSet<Vector2Int>(tiles);
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
                    foreach (Vector2Int side in GridSides.All)
                        if (left.Remove(tile + side))
                        {
                            piece.Add(tile + side);
                            open.Push(tile + side);
                        }
                }
                pieces.Add(piece);
            }
            return pieces;
        }

        // --- Mounds and craters -------------------------------------------------------------

        // A round top (a mound's, raised) or floor (a crater's, sunk), sloping one level on every
        // side: a slope wherever its edge runs straight, a solid corner piece where two slopes meet.
        // A mound's slopes stand round its top, on the room's floor; a crater's just inside its rim.
        private ElevatedArea RoundFeature(RoomStructure structure, RandomStream random, bool raised)
        {
            int radius = Mathf.Max(raised ? 1 : 2, _radius.Evaluate(random));
            RoomFootprint footprint = structure.Footprint;
            Vector2Int centre = footprint.Tiles[random.Range(0, footprint.Tiles.Count)];
            if (!footprint.IsInterior(centre, radius + 2)) return null;

            var body = new HashSet<Vector2Int>();
            float reach = (radius + RoundSlack) * (radius + RoundSlack);
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                    if (dx * dx + dy * dy <= reach) body.Add(centre + new Vector2Int(dx, dy));

            var stairs  = new List<StairFlight>();
            var corners = new List<SlopeCorner>();
            if (!(raised ? RingAround(body, stairs, corners) : RingInside(body, stairs, corners))) return null;

            foreach (Vector2Int tile in body)
                if (!Usable(structure, tile)) return null;
            foreach (StairFlight slope in stairs)
                if (!Usable(structure, slope.Tile) || !structure.IsFree(slope.Foot) || !structure.IsFree(slope.Head) || structure.LevelOf(slope.Foot) != 0) return null;
            foreach (SlopeCorner corner in corners)
                if (!Usable(structure, corner.Tile)) return null;

            return new ElevatedArea(_kind, Level, new List<Vector2Int>(body), stairs, railed: false, corners);
        }

        // A mound's slopes and corners on the floor round its top. A tile touching the top on two
        // opposite sides, or three, joins the top instead, and the ring is worked out again.
        private static bool RingAround(HashSet<Vector2Int> top, List<StairFlight> stairs, List<SlopeCorner> corners)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                stairs.Clear();
                corners.Clear();
                bool grew = false;
                foreach (Vector2Int tile in Around(top))
                {
                    Vector2Int toward = Vector2Int.zero;
                    int sides = 0;
                    foreach (Vector2Int side in GridSides.All)
                        if (top.Contains(tile + side)) { toward += side; sides++; }

                    if (sides == 0) continue;   // beside the top corner to corner only: plain floor
                    if (sides == 1) stairs.Add(new StairFlight(tile, toward, slope: true));
                    else if (sides == 2 && toward != Vector2Int.zero) corners.Add(new SlopeCorner(tile, toward));
                    else { top.Add(tile); grew = true; }
                }
                if (!grew) return true;
            }
            return false;
        }

        // A crater's slopes and corners along the inside of its rim, climbing out. A rim tile open
        // to the room on two opposite sides, or three, is dropped from the crater, and the rim is
        // worked out again; a crater left with no floor inside its slopes is no crater.
        private static bool RingInside(HashSet<Vector2Int> hollow, List<StairFlight> stairs, List<SlopeCorner> corners)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                stairs.Clear();
                corners.Clear();
                var dropped = new List<Vector2Int>();
                foreach (Vector2Int tile in hollow)
                {
                    Vector2Int outward = Vector2Int.zero;
                    int sides = 0;
                    foreach (Vector2Int side in GridSides.All)
                        if (!hollow.Contains(tile + side)) { outward += side; sides++; }

                    if (sides == 0) continue;
                    if (sides == 1) stairs.Add(new StairFlight(tile, outward, slope: true));
                    else if (sides == 2 && outward != Vector2Int.zero) corners.Add(new SlopeCorner(tile, outward));
                    else dropped.Add(tile);
                }
                foreach (Vector2Int tile in dropped) hollow.Remove(tile);
                if (dropped.Count > 0) continue;

                // Every slope's foot must be the crater's floor, not another slope or a corner.
                var rim = new HashSet<Vector2Int>();
                foreach (StairFlight slope in stairs) rim.Add(slope.Tile);
                foreach (SlopeCorner corner in corners) rim.Add(corner.Tile);
                foreach (StairFlight slope in stairs)
                    if (!hollow.Contains(slope.Foot) || rim.Contains(slope.Foot)) return false;
                return stairs.Count > 0;
            }
            return false;
        }

        // Every tile beside the set, side to side or corner to corner, not in it, in a fixed order.
        private static List<Vector2Int> Around(HashSet<Vector2Int> tiles)
        {
            var seen = new HashSet<Vector2Int>();
            var around = new List<Vector2Int>();
            var sorted = new List<Vector2Int>(tiles);
            sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            foreach (Vector2Int tile in sorted)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        Vector2Int next = tile + new Vector2Int(dx, dy);
                        if (!tiles.Contains(next) && seen.Add(next)) around.Add(next);
                    }
            return around;
        }

        // Free floor nothing keeps clear, still on the doorways' level.
        private static bool Usable(RoomStructure structure, Vector2Int tile) =>
            structure.IsOpen(tile) && structure.IsFree(tile) && structure.LevelOf(tile) == 0;
    }
}
