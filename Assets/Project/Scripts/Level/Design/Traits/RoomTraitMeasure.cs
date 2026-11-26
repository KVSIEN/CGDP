using System.Collections.Generic;
using CGD.Algorithms;
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
    //
    // With IsovistUse.Traits the same traits are read from what can be seen from each tile in
    // every direction (RoomVisibility) instead of along its row and column: walls are the share of
    // floor that sees little (RoomSightReading.EnclosedShare of the open floor or less), and lines of
    // fire the longest line of sight at any angle, with cover beside it. They keep these bands, which
    // were set for the row-and-column reading and want retuning from a sweep.
    public static class RoomTraitMeasure
    {
        public const int   NarrowTiles          = 2;
        public const float ConcealmentHighShare = 0.06f;   // above none = Medium
        public const float WallsMediumShare     = 0.15f;
        public const float WallsHighShare       = 0.35f;
        public const int   LaneMediumTiles      = 6;
        public const int   LaneHighTiles        = 9;

        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;
        // Sightlines this close in length count as equally long.
        private const float SightlineTie = 1e-3f;

        private static readonly Dictionary<GameObject, bool> ConcealsCache = new();

        public static RoomTraitReading Measure(RoomStructure structure, IReadOnlyList<PlannedProp> props, bool isovist = false) =>
            Measure(structure, props, new SightlineMap(structure), isovist ? new RoomVisibility(structure) : null);

        // With the sightlines (and, to read the room in every direction, the visibility) already
        // kept current for this structure (a prop planner's).
        internal static RoomTraitReading Measure(RoomStructure structure, IReadOnlyList<PlannedProp> props, SightlineMap sight,
                                                 RoomVisibility visibility = null)
        {
            if (visibility != null) return MeasureIsovist(structure, props, visibility);

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

        private static RoomTraitReading MeasureIsovist(RoomStructure structure, IReadOnlyList<PlannedProp> props, RoomVisibility visibility)
        {
            int open = 0;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (visibility.Area(tile) > 0) open++;
            if (open == 0) return new RoomTraitReading(0f, 0f, 0, default, false);

            LongestSightline(structure, visibility, out Vector2Int from, out Vector2Int to, out float length);
            float enclosed = RoomSightReading.Of(structure, visibility).Enclosed;
            // A lane of n tiles runs n - 1 tiles from middle to middle, so a straight one reads as long as before.
            int tiles = Mathf.RoundToInt(length) + 1;
            return new RoomTraitReading(ConcealedTiles(structure, props) / (float)open, enclosed, tiles, SpanOf(from, to), HasCoverAlong(structure, from, to));
        }

        // The longest line of sight in the room, and of equally long ones, one with cover beside it.
        private static void LongestSightline(RoomStructure structure, RoomVisibility visibility, out Vector2Int from, out Vector2Int to, out float length)
        {
            from = to = default;
            length = 0f;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                length = Mathf.Max(length, visibility.Reach(tile));
            if (length <= 0f) return;

            bool found = false;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (visibility.Reach(tile) < length - SightlineTie) continue;

                visibility.Farthest(tile, out Vector2Int far);
                if (!found)
                {
                    from  = tile;
                    to    = far;
                    found = true;
                }
                if (!HasCoverAlong(structure, tile, far)) continue;
                from = tile;
                to   = far;
                return;
            }
        }

        private static RectInt SpanOf(Vector2Int a, Vector2Int b) =>
            new(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);

        // A pillar or piece of furniture right beside the line, away from its two ends (cover at the
        // very end of a line is where it stops, not somewhere to shoot down it from). Looks half a tile at a time.
        private static bool HasCoverAlong(RoomStructure structure, Vector2Int from, Vector2Int to)
        {
            Vector2 start = (Vector2)from + Vector2.one * 0.5f, end = (Vector2)to + Vector2.one * 0.5f;
            int samples = Mathf.CeilToInt((end - start).magnitude * 2f);
            Vector2Int previous = from;
            for (int i = 1; i < samples; i++)
            {
                Vector2 point = Vector2.Lerp(start, end, (float)i / samples);
                var tile = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
                if (tile == previous || tile == from || tile == to) continue;

                previous = tile;
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (RoomCover.Shelters(structure, tile, tile + side)) return true;
            }
            return false;
        }

        // Walkable tiles some concealment prop lies on (patches are decorations: they take no floor).
        private static int ConcealedTiles(RoomStructure structure, IReadOnlyList<PlannedProp> props)
        {
            var tiles = new HashSet<Vector2Int>();
            foreach (PlannedProp prop in props)
            {
                if (!Conceals(prop.Prefab)) continue;

                foreach (Vector2Int tile in prop.Tiles.allPositionsWithin)
                    if (structure.Contains(tile) && !structure.Has(tile, Solid)) tiles.Add(tile);
            }
            return tiles.Count;
        }

        // Whether a prop hides whoever stands in it. Asked many times for the same few prefabs
        // while a level is planned, so answered once per prefab while playing; in the editor
        // (Room Lab) prefabs are being changed, so it always looks.
        public static bool Conceals(GameObject prefab)
        {
            if (prefab == null) return false;
            if (!Application.isPlaying) return HasZone(prefab);

            if (!ConcealsCache.TryGetValue(prefab, out bool hides))
                ConcealsCache[prefab] = hides = HasZone(prefab);
            return hides;
        }

        private static bool HasZone(GameObject prefab) => prefab.GetComponentInChildren<ConcealmentZone>(true) != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ConcealsCache.Clear();

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
                    RectInt lane = LaneThrough(sight.Blocking, tile, axis == 0, length);
                    if (HasCoverBeside(structure, lane)) return lane;
                    if (!found) { first = lane; found = true; }
                }
            }
            return first;
        }

        private static RectInt LaneThrough(SightBlocking blocking, Vector2Int tile, bool alongX, int length)
        {
            Vector2Int step = alongX ? Vector2Int.right : Vector2Int.up;
            Vector2Int start = tile;
            while (!blocking.BlocksTile(start - step) && !blocking.BlocksEdge(start - step, start))
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
                if (RoomCover.Shelters(structure, tile, tile + side) || RoomCover.Shelters(structure, tile, tile - side)) return true;
            }
            return false;
        }
    }
}
