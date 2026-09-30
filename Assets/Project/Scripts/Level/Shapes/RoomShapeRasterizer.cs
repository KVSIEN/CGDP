using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Turns a RoomShape into floor tiles: a tile is floor when its centre ends up inside the
    // parts after every Add and Subtract. Part sizes, rotation and mirroring come from the
    // random stream, so the same seed gives the same room.
    public static class RoomShapeRasterizer
    {
        public static RoomFootprint Rasterize(RoomShape shape, RectInt area, RandomStream random)
        {
            int width = area.width, depth = area.height;
            var floor = new bool[width, depth];

            foreach (RoomShapePart part in shape.Parts)
            {
                var size = new Vector2(
                    random.Range(part.SizeMin.x, part.SizeMax.x),
                    random.Range(part.SizeMin.y, part.SizeMax.y));
                bool add = part.Operation == RoomShapeOperation.Add;

                for (int x = 0; x < width; x++)
                    for (int y = 0; y < depth; y++)
                    {
                        var point = new Vector2((x + 0.5f) / width, (y + 0.5f) / depth);
                        if (Covers(part.Kind, point - part.Center, size)) floor[x, y] = add;
                    }
            }

            // Turning only by 180° keeps a non-square area's proportions.
            int turns  = !shape.AllowRotation ? 0 : width == depth ? random.Range(0, 4) : random.Range(0, 2) * 2;
            bool mirror = shape.AllowMirror && random.Chance(0.5f);

            var tiles = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < depth; y++)
                    if (floor[x, y]) tiles.Add(area.min + Orient(new Vector2Int(x, y), width, depth, turns, mirror));
            return new RoomFootprint(tiles, shape.CurvedWalls);
        }

        // A tiny tolerance keeps tiles lying exactly on a part's edge from flickering in and
        // out with float rounding.
        private static bool Covers(RoomShapeKind kind, Vector2 offset, Vector2 size)
        {
            const float Tolerance = 1e-4f;
            Vector2 half = size * 0.5f;
            if (half.x <= 0f || half.y <= 0f) return false;

            if (kind == RoomShapeKind.Rectangle)
                return Mathf.Abs(offset.x) <= half.x + Tolerance && Mathf.Abs(offset.y) <= half.y + Tolerance;

            float nx = offset.x / half.x, ny = offset.y / half.y;
            return nx * nx + ny * ny <= 1f + Tolerance;
        }

        private static Vector2Int Orient(Vector2Int tile, int width, int depth, int turns, bool mirror)
        {
            if (mirror) tile.x = width - 1 - tile.x;
            for (int i = 0; i < turns; i++)
            {
                // Quarter turn; the area's width and depth swap each time.
                tile = new Vector2Int(depth - 1 - tile.y, tile.x);
                (width, depth) = (depth, width);
            }
            return tile;
        }
    }
}
