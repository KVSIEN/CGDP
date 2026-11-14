using System.Collections.Generic;
using UnityEngine;
using CGD.Stealth;

namespace CGD.Level
{
    // Scores a furnished room on each RoomTrait, from its floor alone:
    //   - Concealment: the share of walkable floor under concealment (props with a ConcealmentZone)
    //   - Walls: the share of walkable floor that is narrow — at most NarrowTiles of open floor
    //     across it one way or the other, like a corridor, an aisle or a gap between pillars
    //   - Lines of fire: the longest unbroken row or column of see-through floor, and whether
    //     hard cover (a pillar or furniture) stands beside it to shoot from. An open lane with
    //     nothing beside it counts a level lower: it is a killing field, not a firing line
    // The bands below turn each score into Low / Medium / High, to compare with what the
    // holding faction's design asks for (RoomTraitPriorities).
    public static class RoomTraitMeasure
    {
        public const int   NarrowTiles          = 2;
        public const float ConcealmentHighShare = 0.06f;   // above none = Medium
        public const float WallsMediumShare     = 0.15f;
        public const float WallsHighShare       = 0.35f;
        public const int   LaneMediumTiles      = 6;
        public const int   LaneHighTiles        = 9;

        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;

        public static RoomTraitReading Measure(RoomStructure structure, IReadOnlyList<PlannedProp> props)
        {
            var sight = new SightlineMap(structure);
            int walkable = 0, narrow = 0;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (structure.Has(tile, Solid)) continue;
                walkable++;
                (int alongX, int alongY) = sight.Runs(tile);
                if (Mathf.Min(alongX, alongY) <= NarrowTiles) narrow++;
            }

            RectInt lane = LongestLane(structure, sight, out int length);
            float share = walkable > 0 ? 1f / walkable : 0f;
            return new RoomTraitReading(ConcealedTiles(structure, props) * share, narrow * share, length, lane, HasCoverBeside(structure, lane));
        }

        // Walkable tiles some concealment prop lies on (patches are decorations: they take no floor).
        private static int ConcealedTiles(RoomStructure structure, IReadOnlyList<PlannedProp> props)
        {
            var conceals = new Dictionary<GameObject, bool>();
            var tiles = new HashSet<Vector2Int>();
            foreach (PlannedProp prop in props)
            {
                if (prop.Prefab == null) continue;
                if (!conceals.TryGetValue(prop.Prefab, out bool hides))
                    conceals[prop.Prefab] = hides = prop.Prefab.GetComponentInChildren<ConcealmentZone>(true) != null;
                if (!hides) continue;

                foreach (Vector2Int tile in prop.Tiles.allPositionsWithin)
                    if (structure.Footprint.Contains(tile) && !structure.Has(tile, Solid)) tiles.Add(tile);
            }
            return tiles.Count;
        }

        // The longest unbroken row or column of see-through floor, as a strip of tiles — of
        // several as long, one with cover beside it.
        private static RectInt LongestLane(RoomStructure structure, SightlineMap sight, out int length)
        {
            length = 0;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                length = Mathf.Max(length, sight.LongestRun(tile));
            if (length == 0) return default;

            RectInt first = default;
            bool found = false;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                (int x, int y) = sight.Runs(tile);
                for (int axis = 0; axis < 2; axis++)
                {
                    if ((axis == 0 ? x : y) != length) continue;
                    RectInt lane = LaneThrough(structure, tile, axis == 0, length);
                    if (HasCoverBeside(structure, lane)) return lane;
                    if (!found) { first = lane; found = true; }
                }
            }
            return first;
        }

        private static RectInt LaneThrough(RoomStructure structure, Vector2Int tile, bool alongX, int length)
        {
            Vector2Int step = alongX ? Vector2Int.right : Vector2Int.up;
            Vector2Int start = tile;
            while (structure.Footprint.Contains(start - step) && !structure.Has(start - step, SightlineMap.BlocksSight)
                   && !structure.IsPartitioned(start - step, start))
                start -= step;
            return alongX ? new RectInt(start, new Vector2Int(length, 1)) : new RectInt(start, new Vector2Int(1, length));
        }

        // A pillar or piece of furniture right beside the lane, away from its two ends (cover at
        // the very end of a lane is where it stops, not somewhere to shoot down it from).
        private static bool HasCoverBeside(RoomStructure structure, RectInt lane)
        {
            if (lane.width <= 0 || lane.height <= 0) return false;

            bool alongX = lane.height == 1;
            Vector2Int step = alongX ? Vector2Int.right : Vector2Int.up;
            Vector2Int side = alongX ? Vector2Int.up : Vector2Int.right;
            int length = alongX ? lane.width : lane.height;
            for (int i = 1; i < length - 1; i++)
            {
                Vector2Int tile = lane.min + step * i;
                if (IsCover(structure, tile + side) || IsCover(structure, tile - side)) return true;
            }
            return false;
        }

        private static bool IsCover(RoomStructure structure, Vector2Int tile) =>
            structure.Footprint.Contains(tile) && structure.Has(tile, Solid);
    }
}
