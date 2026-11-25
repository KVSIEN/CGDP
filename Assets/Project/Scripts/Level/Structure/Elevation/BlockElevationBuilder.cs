using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Platforms and sunken floors for ElevationStructureRule, in as many tiers as asked for and
    // as fit: a block of floor a level up (a platform, its back to a wall) or down (a sunken floor,
    // floor all round it), then each further tier a smaller block on the one before, a level
    // further up or down, so a stage can climb in steps or a pit deepen like an amphitheatre.
    // Every tier is reached by its own flights from the tier below it (the room's floor for the
    // first). An inner tier keeps TierInset tiles in from the one below on its open sides, room
    // for a flight and the flight's landing, and a platform's one tile in from either end. One
    // tier is the plain feature, built exactly as it always was.
    internal sealed class BlockElevationBuilder
    {
        // Flights of one tier keep at least this many tiles apart, so two don't stand side by side.
        private const int StairSpacing = 3;
        // How far in from the walls a sunken floor's tiles keep: a ring of floor round it, off the room's edge tiles.
        private const int SunkenMargin = 2;
        // A bridge needs this many tiles of sunken floor along its length: two either side of it.
        private const int BridgedLength = 5;
        // Tiles each tier keeps in from the open sides of the one below it: a flight and its landing.
        private const int TierInset = 2;

        private readonly ElevationKind _kind;
        private readonly IntRange _depth;
        private readonly IntRange _length;
        private readonly IntRange _stairs;
        private readonly bool _bridge;
        private readonly bool _railed;

        public BlockElevationBuilder(ElevationKind kind, IntRange depth, IntRange length, IntRange stairs, bool bridge, bool railed)
        {
            _kind   = kind;
            _depth  = depth;
            _length = length;
            _stairs = stairs;
            _bridge = bridge;
            _railed = railed;
        }

        private int Direction => _kind == ElevationKind.Platform ? 1 : -1;

        // One area per tier, lowest step first, up to `tiers` of them (fewer where the block is too
        // small for more); null when not even the first fits.
        public List<ElevatedArea> Build(RoomStructure structure, RandomStream random, int tiers)
        {
            int depth  = Mathf.Max(1, _depth.Evaluate(random));
            int length = Mathf.Max(depth, _length.Evaluate(random));

            List<(Vector2Int tile, int rank)> block = _kind == ElevationKind.Platform
                ? AgainstAWall(structure, random, depth, length, out Vector2Int back)
                : OutInTheRoom(structure, random, depth, length, _bridge, out back);
            if (block == null) return null;

            for (int count = Mathf.Min(tiers, HighestRank(block)); count >= 1; count--)
            {
                List<ElevatedArea> areas = Tiers(structure, random, block, back, count);
                if (areas != null) return areas;
            }
            return null;
        }

        // `count` tiers of the block: a tile is on the highest tier its rank reaches. Each tier's
        // flights stand on the tier below it; a tier left without a flight sinks the whole try.
        private List<ElevatedArea> Tiers(RoomStructure structure, RandomStream random, List<(Vector2Int tile, int rank)> block,
                                         Vector2Int back, int count)
        {
            var areas = new List<ElevatedArea>(count);
            HashSet<Vector2Int> below = null;
            for (int tier = 1; tier <= count; tier++)
            {
                var region = new List<Vector2Int>();
                var own = new List<Vector2Int>();
                foreach ((Vector2Int tile, int rank) in block)
                {
                    int top = Mathf.Min(rank, count);
                    if (top >= tier) region.Add(tile);
                    if (top == tier) own.Add(tile);
                }

                List<StairFlight> stairs = ChooseStairs(structure, region, back, below, random);
                if (stairs.Count == 0) return null;
                areas.Add(new ElevatedArea(_kind, Direction * tier, own, stairs, _railed));
                below = new HashSet<Vector2Int>(own);
            }
            return areas;
        }

        private static int HighestRank(List<(Vector2Int tile, int rank)> block)
        {
            int highest = 0;
            foreach (var (_, rank) in block) highest = Mathf.Max(highest, rank);
            return highest;
        }

        // `depth` rows of `length` tiles standing out from a stretch of the room's wall, all of it
        // free floor nothing keeps clear. `back` is the side the wall is on. A tile's rank is the
        // highest tier that reaches it: inner tiers keep in from the front and the ends.
        private static List<(Vector2Int, int)> AgainstAWall(RoomStructure structure, RandomStream random, int depth, int length, out Vector2Int back)
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

            var tiles = new List<(Vector2Int, int)>(depth * length);
            for (int i = 0; i < length; i++)
            {
                if (footprint.Contains(start + along * i + back)) return null;   // the back row stays against the wall
                for (int j = 0; j < depth; j++)
                {
                    Vector2Int tile = start + along * i - back * j;
                    if (!ElevationGround.IsBare(structure, tile)) return null;
                    int rank = Mathf.Min(Mathf.Min(i + 1, length - i), (depth - 1 - j) / TierInset + 1);
                    tiles.Add((tile, rank));
                }
            }
            return tiles;
        }

        // A `depth` by `length` block with floor all round it, away from the walls. No side is a
        // back, so `back` is zero. Bridged: the row across the middle of its length stays level
        // with the room (still free floor, so nothing stands on the bridge), and the floor is one
        // tier only: a deeper tier would leave the bridge two levels above its neighbours.
        private static List<(Vector2Int, int)> OutInTheRoom(RoomStructure structure, RandomStream random, int depth, int length, bool bridged, out Vector2Int back)
        {
            back = Vector2Int.zero;
            RoomFootprint footprint = structure.Footprint;
            Vector2Int corner = footprint.Tiles[random.Range(0, footprint.Tiles.Count)];
            bool longAlongX = random.Chance(0.5f);
            Vector2Int size = longAlongX ? new Vector2Int(length, depth) : new Vector2Int(depth, length);
            int bridgeAt = bridged && length >= BridgedLength ? length / 2 : -1;

            var tiles = new List<(Vector2Int, int)>(depth * length);
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int tile = corner + new Vector2Int(x, y);
                    if (!footprint.IsInterior(tile, SunkenMargin) || !ElevationGround.IsBare(structure, tile)) return null;
                    if ((longAlongX ? x : y) == bridgeAt) continue;

                    int inset = Mathf.Min(Mathf.Min(x, size.x - 1 - x), Mathf.Min(y, size.y - 1 - y));
                    tiles.Add((tile, bridgeAt >= 0 ? 1 : inset / TierInset + 1));
                }
            return tiles;
        }

        // Flights onto a platform's tier from the floor in front of it, or down into a sunken
        // floor's from the floor round it, nearest the room's centre first (so they face into the
        // room), kept apart from each other: one to each separate piece of it first, then up to
        // Stairs. `region`: the tier and every tier above it. `below`: the tier it stands on, which
        // every tile of a flight outside the region must be on (null for the first tier, on the room's floor).
        private List<StairFlight> ChooseStairs(RoomStructure structure, List<Vector2Int> region, Vector2Int back,
                                               HashSet<Vector2Int> below, RandomStream random)
        {
            var inside = new HashSet<Vector2Int>(region);
            Vector2 centre = structure.Footprint.Anchor;
            var candidates = new List<(StairFlight stairs, float score, int order)>();
            foreach (Vector2Int tile in region)
                foreach (Vector2Int side in GridSides.All)
                {
                    if (inside.Contains(tile + side) || side == back) continue;

                    // A platform's flight stands in front of it and climbs onto it; a sunken floor's
                    // stands just inside its rim and climbs out of it.
                    StairFlight stairs = _kind == ElevationKind.Platform ? new StairFlight(tile + side, -side) : new StairFlight(tile, side);
                    if (!CanStand(structure, stairs, inside, below)) continue;

                    float distance = Vector2.Distance(stairs.Tile + Vector2.one * 0.5f, centre);
                    candidates.Add((stairs, distance + random.Value, candidates.Count));
                }
            // List.Sort is unstable: ties fall back to the order the candidates were found in, so the choice is seeded.
            candidates.Sort((a, b) => a.score != b.score ? a.score.CompareTo(b.score) : a.order.CompareTo(b.order));

            List<HashSet<Vector2Int>> pieces = Pieces(region);
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

        // Whether the flight's three tiles are free floor of the right kind before the block is
        // raised: its foot and the tile it stands on on one level, its head the tier (a platform)
        // or the floor round it (a sunken floor), and whatever lies outside the tier on the tier
        // below. RoomStructure.TryAddElevations checks the rest.
        private static bool CanStand(RoomStructure structure, StairFlight stairs, HashSet<Vector2Int> inside, HashSet<Vector2Int> below)
        {
            if (!structure.IsOpen(stairs.Tile) || !structure.IsFree(stairs.Foot) || !structure.IsFree(stairs.Head)) return false;
            if (structure.IsPartitioned(stairs.Foot, stairs.Tile) || structure.IsPartitioned(stairs.Tile, stairs.Head)) return false;
            bool sameSide = inside.Contains(stairs.Foot) == inside.Contains(stairs.Tile);
            if (!sameSide || inside.Contains(stairs.Tile) == inside.Contains(stairs.Head)) return false;
            if (structure.LevelOf(stairs.Foot) != 0 || structure.LevelOf(stairs.Head) != 0) return false;
            return below == null || (OnTierOrBelow(stairs.Tile, inside, below) && OnTierOrBelow(stairs.Foot, inside, below)
                                     && OnTierOrBelow(stairs.Head, inside, below));
        }

        private static bool OnTierOrBelow(Vector2Int tile, HashSet<Vector2Int> inside, HashSet<Vector2Int> below) =>
            inside.Contains(tile) || below.Contains(tile);

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
    }
}
