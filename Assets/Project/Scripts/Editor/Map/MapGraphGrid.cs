using System.Collections.Generic;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // The cells nodes snap to in the Map Graph window. They are the same cells the level
    // builder turns into rooms (a node's position divided by the style's node spacing), so
    // a snapped graph is laid out exactly as the 3D level will be.
    public static class MapGraphGrid
    {
        // Reused by every call: the canvas asks on each repaint while a node is dragged.
        private static readonly HashSet<Vector2Int> Occupied = new();

        public static Vector2Int CellOf(Vector2 position, Vector2 spacing) =>
            new(Mathf.RoundToInt(position.x / spacing.x), Mathf.RoundToInt(position.y / spacing.y));

        public static Vector2 PositionOf(Vector2Int cell, Vector2 spacing) => new(cell.x * spacing.x, cell.y * spacing.y);

        // The cell nearest `position` that no other node sits in — the one the level builder
        // would give a node dropped there.
        public static Vector2 SnapToFreeCell(MapGraph graph, int nodeId, Vector2 position, Vector2 spacing)
        {
            Occupied.Clear();
            foreach (MapNode other in graph.Nodes)
                if (other.Id != nodeId) Occupied.Add(CellOf(other.Position, spacing));

            Vector2Int preferred = CellOf(position, spacing);
            if (!Occupied.Contains(preferred)) return PositionOf(preferred, spacing);

            // Rings outward; there are only as many occupied cells as nodes, so one is free soon.
            for (int radius = 1; ; radius++)
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                        var candidate = preferred + new Vector2Int(dx, dy);
                        if (!Occupied.Contains(candidate)) return PositionOf(candidate, spacing);
                    }
        }
    }
}
