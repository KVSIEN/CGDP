using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Keeps what a placement just put on its tiles out of the walls beside them: a chair at a
    // desk's far side, a crate turned at an angle, a plant in a corner. Whatever reaches into a
    // wall moves the whole group off it, so its pieces keep their places round each other;
    // against open floor nothing moves. A ledge counts as a wall too: its guard (standing on the
    // higher side), the face of a floor on another level, and a flight of stairs. The room's slanted walls (a curved or angled room's,
    // which cut across its edge tiles: RoomStructure.SlantedWalls) push it straight off them too.
    internal static class PropWallClearance
    {
        // A tile is 3 m in the placeholder kit; a wall's half thickness, in tiles.
        internal const float TileMetres = 3f;
        public const float WallHalfThickness = 0.04f;
        // A ledge's half wall or railing stands this far into the higher tile, in tiles (0.2 m).
        public const float GuardDepth = 0.2f / TileMetres;
        // Off the edge of a floor on another level or a flight of stairs, in tiles.
        private const float LedgeGap = 0.01f;
        // Less than this into a wall, in tiles, is rounding.
        private const float Tolerance = 0.001f;

        // Moves the `count` props from `first` on, which stand on `tiles`.
        public static void KeepOut(RoomStructure structure, RectInt tiles, List<PlannedProp> planned, int first, int count)
        {
            if (!TryMeasure(planned, first, count, out Vector2 min, out Vector2 max)) return;

            var shift = new Vector2(
                Shift(min.x, max.x, tiles.xMin, tiles.xMax,
                      InsetOf(structure, tiles, Vector2Int.left), InsetOf(structure, tiles, Vector2Int.right)),
                Shift(min.y, max.y, tiles.yMin, tiles.yMax,
                      InsetOf(structure, tiles, Vector2Int.down), InsetOf(structure, tiles, Vector2Int.up)));
            shift += OffSlants(structure.SlantedWalls, min + shift, max + shift);
            if (shift == Vector2.zero) return;

            for (int i = first; i < first + count; i++) planned[i] = planned[i].MovedBy(shift);
        }

        // How far to move the box [min, max] (tiles) straight off each slanted wall it reaches into,
        // one after another: to stand a half wall's thickness clear of it, on its floor side.
        internal static Vector2 OffSlants(IReadOnlyList<(Vector2 from, Vector2 to)> walls, Vector2 min, Vector2 max)
        {
            Vector2 total = Vector2.zero;
            foreach (var (from, to) in walls)
            {
                Vector2 along = to - from;
                float length = along.magnitude;
                Vector2 dir = along / length, inward = new(-dir.y, dir.x);
                Vector2 lo = min + total, hi = max + total;
                // Only a wall the box lies across from: it overlaps the wall's run.
                float near = float.MaxValue, first = float.MaxValue, last = float.MinValue;
                foreach (Vector2 corner in new[] { lo, hi, new Vector2(lo.x, hi.y), new Vector2(hi.x, lo.y) })
                {
                    near  = Mathf.Min(near, Vector2.Dot(corner - from, inward));
                    float position = Vector2.Dot(corner - from, dir);
                    first = Mathf.Min(first, position);
                    last  = Mathf.Max(last, position);
                }
                if (last <= 0f || first >= length || near >= WallHalfThickness - Tolerance) continue;
                // Deeper than the box is wide: it stands beyond the wall, not in it.
                if (near < -(hi - lo).magnitude) continue;
                total += inward * (WallHalfThickness - near);
            }
            return total;
        }

        // The floor the solid parts of the props cover, in tiles.
        private static bool TryMeasure(List<PlannedProp> planned, int first, int count, out Vector2 min, out Vector2 max)
        {
            min = Vector2.positiveInfinity;
            max = Vector2.negativeInfinity;
            bool solid = false;
            for (int i = first; i < first + count; i++)
            {
                PlannedProp prop = planned[i];
                if (!PropReach.TryGetExtent(prop.Prefab, prop.Yaw, prop.Stretch, out Rect extent)) continue;

                min = Vector2.Min(min, prop.Position + extent.min / TileMetres);
                max = Vector2.Max(max, prop.Position + extent.max / TileMetres);
                solid = true;
            }
            return solid;
        }

        // How far to move along one axis so [min, max] clears the walls at the ends of
        // [low, high], each standing `insetLow` / `insetHigh` in from its end (negative: no wall).
        // Too wide to clear a wall on both sides: centred between them.
        private static float Shift(float min, float max, float low, float high, float insetLow, float insetHigh)
        {
            bool wallLow = insetLow >= 0f, wallHigh = insetHigh >= 0f;
            if (wallLow) low += insetLow;
            if (wallHigh) high -= insetHigh;
            float off = wallLow ? Mathf.Max(0f, low - min) : 0f;
            float back = wallHigh ? Mathf.Max(0f, max - high) : 0f;
            if (off <= Tolerance && back <= Tolerance) return 0f;
            if (wallLow && wallHigh && max - min > high - low) return (low + high - min - max) * 0.5f;
            return off - back;
        }

        // How far in from the tiles' `side` the props must keep, the deepest of what stands along
        // it: a wall, a ledge's guard on these tiles' (higher) side, or the edge of another floor
        // or a flight of stairs. Negative when that side is open floor.
        private static float InsetOf(RoomStructure structure, RectInt tiles, Vector2Int side)
        {
            float inset = -1f;
            foreach (Vector2Int tile in tiles.allPositionsWithin)
            {
                Vector2Int next = tile + side;
                if (tiles.Contains(next)) continue;
                if (structure.IsWall(tile, side)) inset = Mathf.Max(inset, WallHalfThickness);
                else if (structure.LevelOf(next) != structure.LevelOf(tile) || structure.TryGetStairs(next, out _))
                    inset = Mathf.Max(inset, structure.LevelOf(tile) > structure.LevelOf(next) && structure.GuardOn(tile, next) != LedgeGuard.None
                                                 ? GuardDepth : LedgeGap);
            }
            return inset;
        }
    }
}
