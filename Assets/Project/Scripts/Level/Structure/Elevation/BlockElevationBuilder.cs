using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Platforms, islands and sunken floors for ElevationStructureRule, in as many tiers as asked
    // for and as fit: a block of floor a level up (a platform, its back to a wall; an island, floor
    // all round it) or down (a sunken floor, floor all round it), then each further tier a smaller block on the one before, a level
    // further up or down, so a stage can climb in steps or a pit deepen like an amphitheatre.
    // Every tier is reached by its own flights from the tier below it (the room's floor for the
    // first). An inner tier keeps TierInset tiles in from the one below on its open sides, room
    // for a flight and the flight's landing, and a platform's one tile in from either end. One
    // tier is the plain feature. Each tier is guarded and reached as its role asks (ElevationAccess).
    // A raised block may have its corners cut at 45° (GroundCorner.Sheer): a platform's two front
    // corners, an island's four; the corner tiles turn solid, half up and half down.
    internal sealed class BlockElevationBuilder
    {
        // Flights of one tier keep at least this many tiles apart, so two don't stand side by side.
        private const int StairSpacing = 3;
        // How far in from the walls a sunken floor's tiles keep: a ring of floor round it, off the room's edge tiles.
        private const int SunkenMargin = 2;
        // A bridge needs this many tiles of sunken floor along its length: two either side of it.
        private const int BridgedLength = 5;
        // Shrinking never takes a side below this: a flight's landing and something beside it.
        private const int SmallestSide = 2;
        // Tiles each tier keeps in from the open sides of the one below it: a flight and its landing.
        private const int TierInset = 2;
        // The chance a stage backs onto the wall across from the way in; the rest take any wall, so
        // a doorway in that wall doesn't leave the room without one. Either way it is centred on it.
        private const float StageOnAxis = 0.8f;
        // An island takes one of this many spots nearest the room's middle, so it varies from room to room.
        private const int IslandChoices = 3;
        // The chance a stage is climbed by one wide flight up its middle rather than a pair at its ends.
        private const float WideStageStairs = 0.5f;
        // A block's corners are cut only when it is at least this many tiles along its long side
        // and CutShortSide along its short one, so it keeps a flat top worth standing on.
        private const int CutLongSide  = 4;
        private const int CutShortSide = 2;

        private readonly ElevationKind _kind;
        private readonly IntRange _depth;
        private readonly IntRange _length;
        private readonly IntRange _stairs;
        private readonly bool _bridge;
        private readonly ElevationRole _role;
        private readonly LedgeGuard _wall;
        private readonly float _cutChance;

        // `wall`: what a perch's front or a bastion's ledges carry. `cutChance`: the chance a raised
        // block's corners are cut at 45°, drawn only when above 0.
        public BlockElevationBuilder(ElevationKind kind, IntRange depth, IntRange length, IntRange stairs, bool bridge,
                                     ElevationRole role = ElevationRole.SplitLevel, LedgeGuard wall = LedgeGuard.HalfWall, float cutChance = 0f)
        {
            _cutChance = cutChance;
            _kind   = kind;
            _depth  = depth;
            _length = length;
            _stairs = stairs;
            _bridge = bridge;
            _role   = role;
            _wall   = wall;
        }

        private int Direction => IsRaised ? 1 : -1;

        // Raised floor is climbed onto from the floor round it; a sunken floor is climbed out of.
        private bool IsRaised => _kind == ElevationKind.Platform || _kind == ElevationKind.Island;

        private bool IsStage => _role == ElevationRole.Stage && _kind == ElevationKind.Platform;

        // One area per tier, lowest step first, up to `tiers` of them (fewer where the block is too
        // small for more); null when not even the first fits. `shrink`: tiles taken off the size
        // drawn, off the length first and off the depth half as fast. `open`: the guard on ledges
        // the role leaves open (a split level's or a pit's: None or Railing).
        public List<ElevatedArea> Build(RoomStructure structure, RandomStream random, int tiers, LedgeGuard open, int shrink = 0)
        {
            int depth  = Shrunk(Mathf.Max(1, _depth.Evaluate(random)), shrink / 2);
            int length = Mathf.Max(depth, Shrunk(_length.Evaluate(random), shrink));

            List<(Vector2Int tile, int rank)> block = _kind == ElevationKind.Platform
                ? AgainstAWall(structure, random, depth, length, out Vector2Int back)
                : OutInTheRoom(structure, random, depth, length, _bridge && _kind == ElevationKind.SunkenFloor, out back);
            if (block == null) return null;

            HashSet<Vector2Int> cuts = IsRaised && _cutChance > 0f && random.Chance(_cutChance)
                ? CutCorners(structure, block, back)
                : new HashSet<Vector2Int>();
            block.RemoveAll(entry => cuts.Contains(entry.tile));

            for (int count = Mathf.Min(tiers, HighestRank(block)); count >= 1; count--)
            {
                List<ElevatedArea> areas = Tiers(structure, random, block, back, count, open, cuts);
                if (areas != null) return areas;
            }
            return null;
        }

        // `count` tiers of the block: a tile is on the highest tier its rank reaches. Each tier's
        // flights stand on the tier below it; a tier left without the flights its role needs sinks
        // the whole try. `cuts`: the block's cut corners, the first tier's, where no flight stands.
        private List<ElevatedArea> Tiers(RoomStructure structure, RandomStream random, List<(Vector2Int tile, int rank)> block,
                                         Vector2Int back, int count, LedgeGuard open, HashSet<Vector2Int> cuts)
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

                List<StairFlight> stairs = ChooseStairs(structure, region, back, below, cuts, random);
                LedgeGuards? guards = ElevationAccess.Fit(_role, ElevationAccess.Guards(_role, structure, own, open, _wall), stairs.Count);
                if (guards == null) return null;
                areas.Add(new ElevatedArea(_kind, Direction * tier, own, stairs, guards.Value, tier == 1 ? Sheer(cuts, region) : null));
                below = new HashSet<Vector2Int>(own);
            }
            return areas;
        }

        // The block's corner tiles to cut: those of its first tier with the block on two sides at
        // right angles, but not a platform's back corners (against the wall) nor one in front of a
        // doorway (RoomStructure keeps those free). None when the block is too small to cut.
        private static HashSet<Vector2Int> CutCorners(RoomStructure structure, List<(Vector2Int tile, int rank)> block, Vector2Int back)
        {
            var cuts = new HashSet<Vector2Int>();
            var tiles = new HashSet<Vector2Int>();
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach ((Vector2Int tile, _) in block)
            {
                tiles.Add(tile);
                min = Vector2Int.Min(min, tile);
                max = Vector2Int.Max(max, tile);
            }
            Vector2Int size = max - min + Vector2Int.one;
            if (Mathf.Max(size.x, size.y) < CutLongSide || Mathf.Min(size.x, size.y) < CutShortSide) return cuts;

            foreach ((Vector2Int tile, int rank) in block)
            {
                if (rank != 1 || structure.Has(tile, RoomTileTags.NearDoor)) continue;
                Vector2Int outward = Vector2Int.zero;
                int open = 0;
                bool againstTheWall = false;
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (!tiles.Contains(tile + side))
                    {
                        againstTheWall |= side == back;
                        outward += side;
                        open++;
                    }
                // Two open sides at right angles sum to a diagonal.
                if (open == 2 && !againstTheWall && outward.x != 0 && outward.y != 0) cuts.Add(tile);
            }
            // Two corners side by side (a block two tiles across) would leave each other nothing to rise onto.
            cuts.RemoveWhere(tile =>
            {
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (cuts.Contains(tile + side)) return true;
                return false;
            });
            return cuts;
        }

        // The cut corners as sheer corner pieces, each rising toward the block across its inner
        // corner (the two sides it has the block on). `region`: the first tier and every tier above it.
        private static List<GroundCorner> Sheer(HashSet<Vector2Int> cuts, List<Vector2Int> region)
        {
            if (cuts.Count == 0) return null;
            var inside = new HashSet<Vector2Int>(region);
            var corners = new List<GroundCorner>(cuts.Count);
            foreach (Vector2Int tile in cuts)
            {
                Vector2Int up = Vector2Int.zero;
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (inside.Contains(tile + side)) up += side;
                corners.Add(new GroundCorner(tile, up, sheer: true));
            }
            return corners;
        }

        // The side of the room across from its way in: the one facing away from the arrival most.
        private static Vector2Int FarSide(RoomStructure structure)
        {
            Vector2 away = structure.Footprint.Anchor - ((Vector2)structure.Arrival.Value + Vector2.one * 0.5f);
            Vector2Int far = GridNeighbours.Four[0];
            float best = float.NegativeInfinity;
            foreach (Vector2Int side in GridNeighbours.Four)
            {
                float facing = Vector2.Dot(side, away);
                if (facing > best)
                {
                    best = facing;
                    far = side;
                }
            }
            return far;
        }

        // Of the tiles along a wall, the one a block `length` long running `along` from it starts at
        // to stand centred on `middle`. Ties go to the first found, so the choice is seeded.
        private static Vector2Int Centred(List<Vector2Int> starts, Vector2 middle, Vector2Int along, int length)
        {
            Vector2Int best = starts[0];
            float nearest = float.PositiveInfinity;
            foreach (Vector2Int start in starts)
            {
                Vector2 blockMiddle = start + (Vector2)along * ((length - 1) * 0.5f) + Vector2.one * 0.5f;
                float off = Mathf.Abs(Vector2.Dot(blockMiddle - middle, along));
                if (off < nearest)
                {
                    nearest = off;
                    best = start;
                }
            }
            return best;
        }

        private static int Shrunk(int side, int by) => Mathf.Max(Mathf.Min(side, SmallestSide), side - by);

        private static int HighestRank(List<(Vector2Int tile, int rank)> block)
        {
            int highest = 0;
            foreach (var (_, rank) in block) highest = Mathf.Max(highest, rank);
            return highest;
        }

        // `depth` rows of `length` tiles standing out from a stretch of the room's wall, all of it
        // free floor nothing keeps clear. `back` is the side the wall is on. A tile's rank is the
        // highest tier that reaches it: inner tiers keep in from the front and the ends. A stage
        // mostly backs onto the wall across from the way in, and stands centred on the room's middle.
        private List<(Vector2Int, int)> AgainstAWall(RoomStructure structure, RandomStream random, int depth, int length, out Vector2Int back)
        {
            back = GridNeighbours.Four[random.Range(0, GridNeighbours.Four.Length)];
            if (IsStage && random.Chance(StageOnAxis) && structure.Arrival.HasValue) back = FarSide(structure);
            RoomFootprint footprint = structure.Footprint;
            var starts = new List<Vector2Int>();
            foreach (Vector2Int tile in footprint.Tiles)
                if (!footprint.Contains(tile + back)) starts.Add(tile);
            if (starts.Count == 0) return null;

            Vector2Int start = starts[random.Range(0, starts.Count)];
            var along = new Vector2Int(back.y, back.x);
            if (random.Chance(0.5f)) along = -along;
            if (IsStage) start = Centred(starts, footprint.Anchor, along, length);

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
        // An island stands as near the room's middle as its routes and centre spot leave room for:
        // one of the IslandChoices nearest spots that fit.
        private List<(Vector2Int, int)> OutInTheRoom(RoomStructure structure, RandomStream random, int depth, int length, bool bridged, out Vector2Int back)
        {
            back = Vector2Int.zero;
            RoomFootprint footprint = structure.Footprint;
            Vector2Int corner = footprint.Tiles[random.Range(0, footprint.Tiles.Count)];
            bool longAlongX = random.Chance(0.5f);
            Vector2Int size = longAlongX ? new Vector2Int(length, depth) : new Vector2Int(depth, length);
            if (_kind == ElevationKind.Island && !TryNearTheMiddle(structure, size, random, out corner)) return null;
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
        // floor's from the floor round it, nearest the room's centre first (so the first faces into
        // the room), kept apart from each other: one to each separate piece of it, then spread
        // round it up to Stairs, as many as the role asks (ElevationAccess). `region`: the tier and
        // every tier above it. `below`: the tier it stands on, which every tile of a flight outside
        // the region must be on (null for the first tier, on the room's floor).
        private List<StairFlight> ChooseStairs(RoomStructure structure, List<Vector2Int> region, Vector2Int back,
                                               HashSet<Vector2Int> below, HashSet<Vector2Int> cuts, RandomStream random)
        {
            var inside = new HashSet<Vector2Int>(region);
            Vector2 centre = structure.Footprint.Anchor;
            var candidates = new List<(StairFlight stairs, float score, int order)>();
            foreach (Vector2Int tile in region)
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    if (inside.Contains(tile + side) || side == back) continue;

                    // A platform's or an island's flight stands in front of it and climbs onto it; a
                    // sunken floor's stands just inside its rim and climbs out of it.
                    StairFlight stairs = IsRaised ? new StairFlight(tile + side, -side) : new StairFlight(tile, side);
                    if (!CanStand(structure, stairs, inside, below) || cuts.Contains(stairs.Tile) || cuts.Contains(stairs.Foot)) continue;

                    float distance = Vector2.Distance(stairs.Tile + Vector2.one * 0.5f, centre);
                    candidates.Add((stairs, distance + random.Value, candidates.Count));
                }
            // List.Sort is unstable: ties fall back to the order the candidates were found in, so the choice is seeded.
            candidates.Sort((a, b) => a.score != b.score ? a.score.CompareTo(b.score) : a.order.CompareTo(b.order));
            var preferred = new List<StairFlight>(candidates.Count);
            foreach (var candidate in candidates) preferred.Add(candidate.stairs);

            List<HashSet<Vector2Int>> pieces = Pieces(region);
            // A stage's stairs are symmetric or it isn't built there: the rule tries it smaller or elsewhere.
            if (IsStage) return StageStairs(preferred, region, back, random.Chance(WideStageStairs));
            int wanted = ElevationAccess.Ways(_role, Mathf.Max(pieces.Count, _stairs.Evaluate(random)), region.Count);
            return ElevationAccess.Pick(preferred, pieces, Reaches, null, wanted, StairSpacing);
        }

        // The corner of a block of `size` that fits (bare floor away from the walls), one of the
        // IslandChoices whose middle is nearest the room's.
        private static bool TryNearTheMiddle(RoomStructure structure, Vector2Int size, RandomStream random, out Vector2Int corner)
        {
            RoomFootprint footprint = structure.Footprint;
            Vector2 middle = footprint.Anchor;
            var fits = new List<(Vector2Int corner, float distance)>();
            foreach (Vector2Int start in footprint.Tiles)
            {
                if (!Fits(structure, start, size)) continue;
                fits.Add((start, (start + (Vector2)size * 0.5f - middle).sqrMagnitude));
            }
            corner = default;
            if (fits.Count == 0) return false;

            // Footprint order breaks ties, so the choice is seeded.
            fits.Sort((a, b) => a.distance.CompareTo(b.distance));
            corner = fits[random.Range(0, Mathf.Min(IslandChoices, fits.Count))].corner;
            return true;
        }

        private static bool Fits(RoomStructure structure, Vector2Int corner, Vector2Int size)
        {
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int tile = corner + new Vector2Int(x, y);
                    if (!structure.Footprint.IsInterior(tile, SunkenMargin) || !ElevationGround.IsBare(structure, tile)) return false;
                }
            return true;
        }

        // A stage's flights up the front of `region`, from the free ones in `preferred` (see ElevationAccess.Stage).
        private static List<StairFlight> StageStairs(List<StairFlight> preferred, List<Vector2Int> region, Vector2Int back, bool wide)
        {
            var front = new List<StairFlight>();
            foreach (StairFlight stairs in preferred)
                if (stairs.Up == back) front.Add(stairs);

            var along = new Vector2Int(Mathf.Abs(back.y), Mathf.Abs(back.x));
            int first = int.MaxValue, last = int.MinValue;
            foreach (Vector2Int tile in region)
            {
                int position = tile.x * along.x + tile.y * along.y;
                first = Mathf.Min(first, position);
                last  = Mathf.Max(last, position);
            }
            return ElevationAccess.Stage(front, first, last, wide);
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
                    foreach (Vector2Int side in GridNeighbours.Four)
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
