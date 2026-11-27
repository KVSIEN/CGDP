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
                bool angled = IsAngled(part.Kind);
                // A 45° shape is centred on a tile's middle, so its points are one tile and every
                // step of its slanted edges is one tile: one straight diagonal wall, no kinks.
                Vector2 centre = new(part.Center.x * width, part.Center.y * depth);
                Vector2 snapped = new(Mathf.Floor(centre.x) + 0.5f, Mathf.Floor(centre.y) + 0.5f);

                for (int x = 0; x < width; x++)
                    for (int y = 0; y < depth; y++)
                    {
                        bool covers = angled
                            ? CoversAngled(part.Kind, new Vector2(x + 0.5f, y + 0.5f) - snapped, new Vector2(size.x * width, size.y * depth) * 0.5f)
                            : Covers(part.Kind, new Vector2((x + 0.5f) / width, (y + 0.5f) / depth) - part.Center, size);
                        if (covers) floor[x, y] = add;
                    }
            }

            // Turning only by 180° keeps a non-square area's proportions.
            int turns  = !shape.AllowRotation ? 0 : width == depth ? random.Range(0, 4) : random.Range(0, 2) * 2;
            bool mirror = shape.AllowMirror && random.Chance(0.5f);

            var tiles = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < depth; y++)
                    if (floor[x, y]) tiles.Add(area.min + Orient(new Vector2Int(x, y), width, depth, turns, mirror));
            return new RoomFootprint(tiles, shape.CurvedWalls || shape.AngledWalls, shape.AngledWalls);
        }

        private static bool IsAngled(RoomShapeKind kind) =>
            kind is RoomShapeKind.Octagon or RoomShapeKind.Diamond or RoomShapeKind.PointedEnds;

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

        // In tiles from the (snapped) centre, measured in tiles so a slanted edge is a 45° line
        // whatever the room's proportions: |x| + |y| = constant.
        private static bool CoversAngled(RoomShapeKind kind, Vector2 offset, Vector2 half)
        {
            const float Tolerance = 1e-4f;
            float x = Mathf.Abs(offset.x), y = Mathf.Abs(offset.y), shorter = Mathf.Min(half.x, half.y);
            switch (kind)
            {
                case RoomShapeKind.Octagon:
                    return x <= half.x + Tolerance && y <= half.y + Tolerance && x + y <= half.x + half.y - shorter * 0.5f + Tolerance;
                case RoomShapeKind.Diamond:
                    return x + y <= shorter + Tolerance;
                default:
                    // Points along the longer axis.
                    return half.x >= half.y
                        ? y <= half.y + Tolerance && x + y <= half.x + Tolerance
                        : x <= half.x + Tolerance && x + y <= half.y + Tolerance;
            }
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
