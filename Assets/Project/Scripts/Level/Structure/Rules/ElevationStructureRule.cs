using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Raises a platform against a wall or sinks a floor out in the room, one level from the
    // doorways' floor, with stairs between: a stage, a loading deck, a mezzanine, a lounge pit.
    // Each is placed so it reads as built on purpose:
    //   - a platform stands with its back to the room's wall, its stairs on its open sides and
    //     facing into the room; a sunken floor has floor all round it and its stairs along its
    //     rim, leading down from the room;
    //   - neither covers a doorway's front, a route from a doorway or the centre spot, so
    //     everyone arrives and crosses the room on the doorways' level;
    //   - each has at least one flight, landings clear at both ends, and every tile stays
    //     reachable both ways (RoomStructure.TryAddElevation holds the room to that).
    // Does nothing in a room whose levels don't take its kind (LevelBuildSettings › Lowest and
    // Highest Level), and then draws no random numbers, so the rest of the room is unchanged.
    [CreateAssetMenu(fileName = "ElevationStructureRule", menuName = "CGD/Level/Structure/Elevation")]
    public class ElevationStructureRule : RoomStructureRule
    {
        private const int Attempts = 16;
        // Flights of one area keep at least this many tiles apart, so two don't stand side by side.
        private const int StairSpacing = 3;
        // How far in from the walls a sunken floor's tiles keep: a ring of floor round it, off the room's edge tiles.
        private const int SunkenMargin = 2;

        [SerializeField] private ElevationKind _kind = ElevationKind.Platform;
        [Tooltip("How many to put in the room")]
        [SerializeField] private IntRange _count = new(1, 1);
        [Tooltip("Tiles across: out from the wall for a platform, the short side for a sunken floor")]
        [SerializeField] private IntRange _depth = new(2, 3);
        [Tooltip("Tiles long: along the wall for a platform, the long side for a sunken floor")]
        [SerializeField] private IntRange _length = new(3, 5);
        [Tooltip("Flights of stairs to each one; fewer are built where there is no room for them, never none")]
        [SerializeField] private IntRange _stairs = new(1, 2);
        [Tooltip("Most of the room's floor these may take together")]
        [SerializeField, Range(0.05f, 0.5f)] private float _maxShare = 0.25f;
        [Tooltip("A railing along the open edges: nobody drops off a platform or into a sunken floor, only takes the stairs")]
        [SerializeField] private bool _railed;

        public ElevationKind Kind => _kind;
        public int Level => _kind == ElevationKind.Platform ? 1 : -1;

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
            int depth  = Mathf.Max(1, _depth.Evaluate(random));
            int length = Mathf.Max(depth, _length.Evaluate(random));
            if (depth * length > budget) return false;

            List<Vector2Int> tiles = _kind == ElevationKind.Platform
                ? AgainstAWall(structure, random, depth, length, out Vector2Int back)
                : OutInTheRoom(structure, random, depth, length, out back);
            if (tiles == null) return false;

            List<StairFlight> stairs = ChooseStairs(structure, tiles, back, random);
            if (stairs.Count == 0) return false;

            if (!structure.TryAddElevation(new ElevatedArea(_kind, Level, tiles, stairs, _railed))) return false;
            budget -= tiles.Count;
            return true;
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
        // back, so `back` is zero.
        private static List<Vector2Int> OutInTheRoom(RoomStructure structure, RandomStream random, int depth, int length, out Vector2Int back)
        {
            back = Vector2Int.zero;
            RoomFootprint footprint = structure.Footprint;
            Vector2Int corner = footprint.Tiles[random.Range(0, footprint.Tiles.Count)];
            bool longAlongX = random.Chance(0.5f);
            Vector2Int size = longAlongX ? new Vector2Int(length, depth) : new Vector2Int(depth, length);

            var tiles = new List<Vector2Int>(depth * length);
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int tile = corner + new Vector2Int(x, y);
                    if (!footprint.IsInterior(tile, SunkenMargin) || !Usable(structure, tile)) return null;
                    tiles.Add(tile);
                }
            return tiles;
        }

        // Free floor nothing keeps clear, still on the doorways' level.
        private static bool Usable(RoomStructure structure, Vector2Int tile) =>
            structure.IsOpen(tile) && structure.IsFree(tile) && structure.LevelOf(tile) == 0;

        // Flights onto a platform from the floor in front of it, or down into a sunken floor from
        // the floor round it, nearest the room's centre first (so they face into the room), kept
        // apart from each other.
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

            int wanted = Mathf.Max(1, _stairs.Evaluate(random));
            var chosen = new List<StairFlight>(wanted);
            foreach (var candidate in candidates)
            {
                if (chosen.Count >= wanted) break;
                if (FarFromAll(candidate.stairs, chosen)) chosen.Add(candidate.stairs);
            }
            return chosen;
        }

        // Whether the flight's three tiles are free floor of the right kind before the area is
        // raised: its foot and the tile it stands on on one level, its head the area (a platform)
        // or the floor round it (a sunken floor). RoomStructure.TryAddElevation checks the rest.
        private static bool CanStand(RoomStructure structure, StairFlight stairs, HashSet<Vector2Int> inside)
        {
            if (!structure.IsOpen(stairs.Tile) || !structure.IsFree(stairs.Foot) || !structure.IsFree(stairs.Head)) return false;
            if (structure.IsPartitioned(stairs.Foot, stairs.Tile) || structure.IsPartitioned(stairs.Tile, stairs.Head)) return false;
            bool sameSide = inside.Contains(stairs.Foot) == inside.Contains(stairs.Tile);
            return sameSide && inside.Contains(stairs.Tile) != inside.Contains(stairs.Head)
                && structure.LevelOf(stairs.Foot) == 0 && structure.LevelOf(stairs.Head) == 0;
        }

        private static bool FarFromAll(StairFlight stairs, List<StairFlight> chosen)
        {
            foreach (StairFlight other in chosen)
            {
                Vector2Int apart = stairs.Tile - other.Tile;
                if (Mathf.Max(Mathf.Abs(apart.x), Mathf.Abs(apart.y)) < StairSpacing) return false;
            }
            return true;
        }
    }
}
