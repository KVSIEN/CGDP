using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Keeps what a placement just put on its tiles out of the walls beside them: a chair at a
    // desk's far side, a crate turned at an angle, a plant in a corner. Whatever reaches into a
    // wall moves the whole group off it, so its pieces keep their places round each other;
    // against open floor nothing moves.
    internal static class PropWallClearance
    {
        // A tile is 3 m in the placeholder kit; a wall's half thickness, in tiles.
        private const float TileMetres = 3f;
        public const float WallHalfThickness = 0.04f;
        // Less than this into a wall, in tiles, is rounding.
        private const float Tolerance = 0.001f;

        // Moves the `count` props from `first` on, which stand on `tiles`.
        public static void KeepOut(RoomStructure structure, RectInt tiles, List<PlannedProp> planned, int first, int count)
        {
            if (!TryMeasure(planned, first, count, out Vector2 min, out Vector2 max)) return;

            var shift = new Vector2(
                Shift(min.x, max.x, tiles.xMin, tiles.xMax,
                      HasWall(structure, tiles, Vector2Int.left), HasWall(structure, tiles, Vector2Int.right)),
                Shift(min.y, max.y, tiles.yMin, tiles.yMax,
                      HasWall(structure, tiles, Vector2Int.down), HasWall(structure, tiles, Vector2Int.up)));
            if (shift == Vector2.zero) return;

            for (int i = first; i < first + count; i++) planned[i] = planned[i].MovedBy(shift);
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
        // [low, high]. Too wide to clear a wall on both sides: centred between them.
        private static float Shift(float min, float max, float low, float high, bool wallLow, bool wallHigh)
        {
            if (wallLow) low += WallHalfThickness;
            if (wallHigh) high -= WallHalfThickness;
            float off = wallLow ? Mathf.Max(0f, low - min) : 0f;
            float back = wallHigh ? Mathf.Max(0f, max - high) : 0f;
            if (off <= Tolerance && back <= Tolerance) return 0f;
            if (wallLow && wallHigh && max - min > high - low) return (low + high - min - max) * 0.5f;
            return off - back;
        }

        private static bool HasWall(RoomStructure structure, RectInt tiles, Vector2Int side)
        {
            foreach (Vector2Int tile in tiles.allPositionsWithin)
                if (!tiles.Contains(tile + side) && structure.IsWall(tile, side)) return true;
            return false;
        }
    }
}
