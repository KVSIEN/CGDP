using System;
using System.Collections.Generic;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Right-click menu contents for the Map Graph canvas. Every item edits through the
    // session, so each is a single undo step.
    public static class MapGraphContextMenu
    {
        public static void AddCanvasItems(GenericMenu menu, Vector2 worldPosition, MapGraphEditorSession session)
        {
            foreach (MapNodeType type in Enum.GetValues(typeof(MapNodeType)))
                menu.AddItem(new GUIContent($"Add Node/{type}"), false, () => session.AddNode(type, worldPosition));
        }

        public static void AddNodeItems(GenericMenu menu, MapNode node, MapGraphEditorSession session)
        {
            foreach (MapNodeType type in Enum.GetValues(typeof(MapNodeType)))
                menu.AddItem(new GUIContent($"Type/{type}"), node.Type == type, () => session.SetType(node, type));

            bool locked = session.Asset.IsLocked(node.Id);
            menu.AddItem(new GUIContent(locked ? "Unlock" : "Lock"), false, () => session.SetLocked(node, !locked));

            var neighbors = new List<int>();
            session.Graph.GetNeighbors(node.Id, neighbors);
            foreach (int neighbor in neighbors)
                menu.AddItem(new GUIContent($"Disconnect/#{neighbor}"), false, () => session.Disconnect(node.Id, neighbor));

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Node"), false, () => session.DeleteNode(node.Id));
        }

        public static void AddConnectionItems(GenericMenu menu, MapConnection connection, MapGraphEditorSession session)
        {
            foreach (ConnectionType type in Enum.GetValues(typeof(ConnectionType)))
                menu.AddItem(new GUIContent($"Type/{type}"), connection.Type == type,
                             () => session.SetConnectionType(connection, type));

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Connection"), false, () => session.Disconnect(connection.A, connection.B));
        }
    }
}
