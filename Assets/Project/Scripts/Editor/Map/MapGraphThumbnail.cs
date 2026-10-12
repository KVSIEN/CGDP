using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A map graph drawn small: links in their connection colour, rooms as dots in their room
    // type's colour (Start, Boss and Exit a little bigger), fitted into a rect. Repaint only.
    public static class MapGraphThumbnail
    {
        private const float Padding  = 8f;
        private const float RoomSize = 6f;
        private const float KeyRoomSize = 10f;

        public static void Draw(Rect rect, MapGraph graph)
        {
            if (Event.current.type != EventType.Repaint || graph == null || graph.Nodes.Count == 0) return;

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            foreach (MapNode node in graph.Nodes)
            {
                min = Vector2.Min(min, node.Position);
                max = Vector2.Max(max, node.Position);
            }

            Vector2 extent = Vector2.Max(max - min, Vector2.one);
            float scale = Mathf.Min((rect.width - Padding * 2f) / extent.x, (rect.height - Padding * 2f) / extent.y);
            Vector2 offset = rect.center - (min + max) * 0.5f * scale;
            Vector2 ToRect(Vector2 position) => offset + position * scale;

            foreach (MapConnection connection in graph.Connections)
            {
                if (!graph.TryGetNode(connection.A, out MapNode a) || !graph.TryGetNode(connection.B, out MapNode b)) continue;
                Handles.color = MapGraphStyle.ConnectionColor(connection.Type);
                Handles.DrawLine(ToRect(a.Position), ToRect(b.Position));
            }
            Handles.color = Color.white;

            foreach (MapNode node in graph.Nodes)
            {
                float size = node.Type is MapNodeType.Start or MapNodeType.Boss or MapNodeType.Exit ? KeyRoomSize : RoomSize;
                Vector2 at = ToRect(node.Position);
                EditorGUI.DrawRect(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), MapNodeColors.Of(node.Type));
            }
        }
    }
}
