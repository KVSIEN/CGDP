using System;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A top-down floor plan of a LevelLayout, drawn blueprint-style: rooms filled in their
    // room type's or faction's colour, corridors, walls (LevelWallPlan — the same lines the
    // level is built from, chamfers and curves included) and inner walls as light lines,
    // pillars and landmark furniture as dark blocks, and a marker on every gated doorway —
    // so the level a graph becomes can be read without building it.
    //
    // PixelsPerTile pixels per tile; row 0 is the south edge, so drawn as a texture it
    // shows north up, matching the level seen from above.
    public static class LevelBlueprint
    {
        public const int PixelsPerTile = 6;
        private const int MarginTiles  = 2;

        public static readonly Color Background = new(0.07f, 0.13f, 0.22f);
        private static readonly Color32 Corridor  = new(36, 58, 92, 255);
        private static readonly Color32 Wall      = new(214, 230, 245, 255);
        private static readonly Color32 Structure = new(14, 24, 40, 255);
        private static readonly Color32 Locked    = new(242, 191, 51, 255);
        private static readonly Color32 Secret    = new(179, 102, 230, 255);
        private static readonly Color32 OneWay    = new(77, 204, 217, 255);

        public enum Fill { RoomType, Faction }

        // `tileOrigin` is the tile at pixel (0, 0); pixel = (tile - origin) * PixelsPerTile.
        public static Color32[] Render(LevelLayout layout, LevelBuildSettings settings, Fill fill,
                                       out int width, out int height, out Vector2Int tileOrigin)
        {
            RectInt bounds = TileBounds(layout);
            tileOrigin = bounds.min - Vector2Int.one * MarginTiles;
            width  = (bounds.width  + MarginTiles * 2) * PixelsPerTile;
            height = (bounds.height + MarginTiles * 2) * PixelsPerTile;

            var canvas = new Canvas(width, height, tileOrigin);
            canvas.Clear(Background);

            foreach (Vector2Int tile in layout.WalkableTiles)
            {
                LevelRoom room = layout.RoomAt(tile);
                canvas.FillTile(tile, room != null ? FloorColor(room, fill) : Corridor);
                if (room?.Structure != null && room.Structure.Has(tile, RoomTileTags.Structure))
                    canvas.FillTile(tile, Structure, inset: 1);
            }

            LevelWallPlan walls = LevelWallPlan.Create(layout, settings);
            foreach (var (a, corner, b) in walls.Cuts)
                canvas.FillTriangle(a, corner, b, Background);
            foreach (LevelWallPlan.Segment segment in walls.Segments)
                canvas.Line(segment.From, segment.To, Wall);
            foreach (LevelRoom room in layout.Rooms.Values)
                DrawPartitions(room, canvas);

            foreach (LevelDoorway doorway in layout.Doorways)
                if (doorway.HasGate && GateColor(doorway.Connection, out Color32 color))
                    canvas.FillEdge(doorway.RoomTile, doorway.Outward, color, thickness: 2);

            return canvas.Pixels;
        }

        // The room under a pixel of a rendered blueprint, if any.
        public static LevelRoom RoomAtPixel(LevelLayout layout, Vector2Int tileOrigin, Vector2 pixel) =>
            layout.RoomAt(tileOrigin + new Vector2Int(Mathf.FloorToInt(pixel.x / PixelsPerTile), Mathf.FloorToInt(pixel.y / PixelsPerTile)));

        // Room floors are the room's colour dimmed toward the background, so walls and
        // markers stand out on top.
        private static Color32 FloorColor(LevelRoom room, Fill fill)
        {
            Color color = fill == Fill.Faction
                ? room.Faction != null ? room.Faction.Color : Color.gray
                : MapNodeColors.Of(room.Node.Type);
            return Color.Lerp(Background, color, 0.6f);
        }

        private static bool GateColor(MapConnection connection, out Color32 color)
        {
            color = connection.Type switch
            {
                ConnectionType.Locked => Locked,
                ConnectionType.Secret => Secret,
                _                     => OneWay,
            };
            return connection.IsGate || connection.OneWay;
        }

        private static void DrawPartitions(LevelRoom room, Canvas canvas)
        {
            if (room.Structure == null) return;
            foreach (Vector2Int tile in room.Footprint.Tiles)
                foreach (Vector2Int side in Sides)
                    if (room.Structure.IsPartitioned(tile, tile + side))
                        canvas.FillEdge(tile, side, Wall, thickness: 1);
        }

        private static RectInt TileBounds(LevelLayout layout)
        {
            Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
            foreach (Vector2Int tile in layout.WalkableTiles)
            {
                min = Vector2Int.Min(min, tile);
                max = Vector2Int.Max(max, tile);
            }
            return min.x > max.x ? new RectInt(0, 0, 1, 1) : new RectInt(min, max - min + Vector2Int.one);
        }

        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private sealed class Canvas
        {
            private readonly int _width, _height;
            private readonly Vector2Int _origin;

            public Canvas(int width, int height, Vector2Int origin)
            {
                _width  = width;
                _height = height;
                _origin = origin;
                Pixels  = new Color32[width * height];
            }

            public Color32[] Pixels { get; }

            public void Clear(Color32 color) => Array.Fill(Pixels, color);

            public void FillTile(Vector2Int tile, Color32 color, int inset = 0)
            {
                Vector2Int p = (tile - _origin) * PixelsPerTile;
                Fill(p.x + inset, p.y + inset, PixelsPerTile - inset * 2, PixelsPerTile - inset * 2, color);
            }

            // A strip along one side of a tile, inside it.
            public void FillEdge(Vector2Int tile, Vector2Int side, Color32 color, int thickness)
            {
                Vector2Int p = (tile - _origin) * PixelsPerTile;
                int far = PixelsPerTile - thickness;
                if (side.x != 0) Fill(p.x + (side.x > 0 ? far : 0), p.y, thickness, PixelsPerTile, color);
                else             Fill(p.x, p.y + (side.y > 0 ? far : 0), PixelsPerTile, thickness, color);
            }

            // A 1-pixel line between two points in tile units, nudged half a pixel to the
            // right of its direction — the outside, where the built wall stands.
            public void Line(Vector2 from, Vector2 to, Color32 color)
            {
                Vector2 a = ToPixel(from), b = ToPixel(to);
                Vector2 dir = (b - a).normalized;
                Vector2 outward = new Vector2(dir.y, -dir.x) * 0.5f;
                int steps = Mathf.CeilToInt((b - a).magnitude * 2f);
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 p = Vector2.Lerp(a, b, steps == 0 ? 0f : (float)i / steps) + outward - Vector2.one * 0.5f;
                    Set(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), color);
                }
            }

            public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color32 color)
            {
                Vector2 pa = ToPixel(a), pb = ToPixel(b), pc = ToPixel(c);
                int minX = Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x))), maxX = Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x)));
                int minY = Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y))), maxY = Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y)));
                for (int y = minY; y < maxY; y++)
                    for (int x = minX; x < maxX; x++)
                        if (Inside(new Vector2(x + 0.5f, y + 0.5f), pa, pb, pc)) Set(x, y, color);
            }

            private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                float d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
                bool negative = d1 < 0f || d2 < 0f || d3 < 0f, positive = d1 > 0f || d2 > 0f || d3 > 0f;
                return !(negative && positive);
            }

            private static float Cross(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

            private Vector2 ToPixel(Vector2 tilePoint) => (tilePoint - _origin) * PixelsPerTile;

            private void Set(int x, int y, Color32 color)
            {
                if (x >= 0 && x < _width && y >= 0 && y < _height) Pixels[y * _width + x] = color;
            }

            private void Fill(int x, int y, int w, int h, Color32 color)
            {
                for (int py = Mathf.Max(0, y); py < Mathf.Min(_height, y + h); py++)
                    for (int px = Mathf.Max(0, x); px < Mathf.Min(_width, x + w); px++)
                        Pixels[py * _width + px] = color;
            }
        }
    }
}
