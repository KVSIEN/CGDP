using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.WorldMap
{
    // Draws a MapGraph as a flat schematic — connections as lines, rooms as coloured
    // squares — for areas generated from a graph rather than painted by hand. The graph's
    // editor positions are scaled to fit the image with a margin: x → right, and the
    // editor's downward y → down the map.
    public static class MapGraphRasterizer
    {
        public static Color32[] Render(MapGraph graph, int width, int height, Color background, Func<MapNode, bool> isVisible = null)
        {
            var pixels = new Color32[width * height];
            Array.Fill(pixels, (Color32)background);
            if (graph == null || graph.Nodes.Count == 0) return pixels;

            var placed = Place(graph, width, height, out float scale);
            int nodeHalf  = Mathf.Max(2, Mathf.RoundToInt(scale * 45f));
            int lineWidth = Mathf.Max(1, Mathf.RoundToInt(scale * 8f));

            foreach (MapConnection connection in graph.Connections)
            {
                if (!placed.TryGetValue(connection.A, out Vector2Int a) || !placed.TryGetValue(connection.B, out Vector2Int b)) continue;
                if (!IsShown(graph, connection.A, isVisible) || !IsShown(graph, connection.B, isVisible)) continue;

                Color32 color = connection.Type == ConnectionType.Normal ? new Color32(150, 150, 150, 255) : new Color32(90, 190, 200, 255);
                DrawLine(pixels, width, height, a, b, lineWidth, color);
            }

            foreach (MapNode node in graph.Nodes)
                if (IsShown(graph, node.Id, isVisible))
                    FillSquare(pixels, width, height, placed[node.Id], nodeHalf, MapNodeColors.Of(node.Type));

            return pixels;
        }

        // Where each node lands in pixel space (y up), and the editor-units → pixels scale.
        public static Dictionary<int, Vector2Int> Place(MapGraph graph, int width, int height, out float scale)
        {
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (MapNode node in graph.Nodes)
            {
                min = Vector2.Min(min, node.Position);
                max = Vector2.Max(max, node.Position);
            }

            const float Margin = 0.1f;
            Vector2 extent = Vector2.Max(max - min, Vector2.one);
            scale = Mathf.Min(width * (1f - 2f * Margin) / extent.x, height * (1f - 2f * Margin) / extent.y);

            Vector2 offset = new Vector2(width, height) * 0.5f - new Vector2(extent.x, -extent.y) * 0.5f * scale;
            var placed = new Dictionary<int, Vector2Int>();
            foreach (MapNode node in graph.Nodes)
            {
                Vector2 local = node.Position - min;
                placed[node.Id] = Vector2Int.RoundToInt(offset + new Vector2(local.x, -local.y) * scale);
            }
            return placed;
        }

        private static bool IsShown(MapGraph graph, int id, Func<MapNode, bool> isVisible) =>
            isVisible == null || (graph.TryGetNode(id, out MapNode node) && isVisible(node));

        private static void FillSquare(Color32[] pixels, int width, int height, Vector2Int center, int half, Color32 color)
        {
            for (int y = Mathf.Max(0, center.y - half); y <= Mathf.Min(height - 1, center.y + half); y++)
            for (int x = Mathf.Max(0, center.x - half); x <= Mathf.Min(width - 1, center.x + half); x++)
                pixels[y * width + x] = color;
        }

        private static void DrawLine(Color32[] pixels, int width, int height, Vector2Int a, Vector2Int b, int thickness, Color32 color)
        {
            int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), 1);
            int half  = thickness / 2;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                FillSquare(pixels, width, height, Vector2Int.RoundToInt(p), half, color);
            }
        }
    }
}
