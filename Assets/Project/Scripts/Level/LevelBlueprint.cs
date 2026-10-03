using System;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A top-down floor plan of a LevelLayout, drawn blueprint-style: rooms filled in their
    // room type's or faction's colour, corridors, walls (and inner walls) as light lines,
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
        public static Color32[] Render(LevelLayout layout, Fill fill, out int width, out int height, out Vector2Int tileOrigin)
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

            foreach (Vector2Int tile in layout.WalkableTiles)
                DrawWalls(layout, canvas, tile);

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

        // A wall runs along every side of a walkable tile that faces solid space, another
        // room, or a corridor it has no doorway into — plus a room's inner walls.
        private static void DrawWalls(LevelLayout layout, Canvas canvas, Vector2Int tile)
        {
            LevelRoom room = layout.RoomAt(tile);
            foreach (Vector2Int side in Sides)
            {
                Vector2Int next = tile + side;
                bool wall = !layout.IsWalkable(next)
                         || (room != null && layout.RoomAt(next) != room && !IsDoorway(layout, tile, next))
                         || (room != null && room.Structure != null && room.Structure.IsPartitioned(tile, next));
                if (room == null && layout.RoomAt(next) != null) wall = false;   // drawn from the room's side
                if (wall) canvas.FillEdge(tile, side, Wall, thickness: 1);
            }
        }

        private static bool IsDoorway(LevelLayout layout, Vector2Int roomTile, Vector2Int outside)
        {
            foreach (LevelDoorway doorway in layout.Doorways)
                if (doorway.RoomTile == roomTile && doorway.OutsideTile == outside) return true;
            return false;
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

            private void Fill(int x, int y, int w, int h, Color32 color)
            {
                for (int py = Mathf.Max(0, y); py < Mathf.Min(_height, y + h); py++)
                    for (int px = Mathf.Max(0, x); px < Mathf.Min(_width, x + w); px++)
                        Pixels[py * _width + px] = color;
            }
        }
    }
}
