using System.Collections.Generic;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Side panel of the Map Graph window: the selected node or connection, then a
    // summary of the whole graph with rule counts, validation issues and generator
    // warnings.
    public class MapGraphInspectorPanel
    {
        private readonly List<int> _neighbors = new();
        private Vector2 _scroll;

        public void Draw(Rect rect, MapGraphEditorSession session)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (session.SelectedNode is { } node)
                DrawNode(node, session);
            else if (session.SelectedConnection is { } connection)
                DrawConnection(connection, session);
            else
                EditorGUILayout.HelpBox(
                    "Click a node or connection to inspect it.\n" +
                    "Shift-drag between nodes to connect them. Right-click for more.",
                    MessageType.None);

            EditorGUILayout.Space();
            DrawSummary(session);

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // --- Node ------------------------------------------------------------------

        private void DrawNode(MapNode node, MapGraphEditorSession session)
        {
            EditorGUILayout.LabelField($"Node #{node.Id}", EditorStyles.boldLabel);
            if (session.IsEdited(node))
                EditorGUILayout.LabelField("Edited since generation", EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            var type = (MapNodeType)EditorGUILayout.EnumPopup("Type", node.Type);
            if (EditorGUI.EndChangeCheck()) session.SetType(node, type);

            EditorGUI.BeginChangeCheck();
            bool locked = EditorGUILayout.Toggle(
                new GUIContent("Locked", "Kept (type, depth and intensity) when the map is regenerated"),
                session.Asset.IsLocked(node.Id));
            if (EditorGUI.EndChangeCheck()) session.SetLocked(node, locked);

            EditorGUI.BeginChangeCheck();
            float intensity = EditorGUILayout.Slider("Intensity", node.Intensity, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) session.SetIntensity(node, intensity);

            DrawFaction(node, session);

            EditorGUI.BeginChangeCheck();
            Vector2 position = EditorGUILayout.Vector2Field("Position", node.Position);
            if (EditorGUI.EndChangeCheck())
            {
                session.BeginMove();
                session.MoveNode(node, position);
            }

            DrawNodeAnalysis(node, session.Analysis);
            DrawNodeConnections(node, session);
        }

        private static void DrawFaction(MapNode node, MapGraphEditorSession session)
        {
            MapGenerationSettings settings = session.Asset.Settings;
            int factionCount = settings != null ? settings.Factions.Count : 0;

            var options = new string[factionCount + 1];
            options[0] = "None";
            for (int i = 0; i < factionCount; i++)
                options[i + 1] = settings.Factions[i];

            EditorGUI.BeginChangeCheck();
            int faction = EditorGUILayout.Popup("Faction", node.Faction + 1, options) - 1;
            float influence = node.HasFaction
                ? EditorGUILayout.Slider("Influence", node.FactionInfluence, 0f, 1f)
                : 1f;
            if (EditorGUI.EndChangeCheck()) session.SetFaction(node, faction, influence);
        }

        private static void DrawNodeAnalysis(MapNode node, MapGraphAnalysis analysis)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Analysis", EditorStyles.miniBoldLabel);

            if (!analysis.IsReachable(node.Id))
            {
                EditorGUILayout.LabelField("Unreachable from Start");
                return;
            }

            int branch = analysis.BranchOf(node.Id);
            EditorGUILayout.LabelField("Depth", $"{analysis.Depth(node.Id)}  ({analysis.Progress(node.Id):0%})");
            EditorGUILayout.LabelField("Route", analysis.IsRequired(node.Id) ? "Required" : "Optional");
            EditorGUILayout.LabelField("Placement", branch == MapGraphAnalysis.MainPathBranch ? "Main path" : $"Branch {branch + 1}");
        }

        private void DrawNodeConnections(MapNode node, MapGraphEditorSession session)
        {
            session.Graph.GetNeighbors(node.Id, _neighbors);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Connections ({_neighbors.Count})", EditorStyles.miniBoldLabel);

            foreach (int neighborId in _neighbors)
            {
                MapConnection connection = session.Graph.GetConnection(node.Id, neighborId);
                string label = session.Graph.TryGetNode(neighborId, out MapNode neighbor)
                    ? $"#{neighborId} {neighbor.Type}"
                    : $"#{neighborId}";

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(label, EditorStyles.linkLabel, GUILayout.Width(110f)))
                        session.SelectNode(neighborId);

                    DrawConnectionTypePopup(connection, session);

                    if (GUILayout.Button("×", GUILayout.Width(20f)))
                    {
                        session.Disconnect(node.Id, neighborId);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        // --- Connection ------------------------------------------------------------

        private static void DrawConnection(MapConnection connection, MapGraphEditorSession session)
        {
            EditorGUILayout.LabelField($"Connection #{connection.A} ↔ #{connection.B}", EditorStyles.boldLabel);
            DrawConnectionTypePopup(connection, session);

            if (GUILayout.Button("Delete Connection"))
            {
                session.Disconnect(connection.A, connection.B);
                GUIUtility.ExitGUI();
            }
        }

        private static void DrawConnectionTypePopup(MapConnection connection, MapGraphEditorSession session)
        {
            EditorGUI.BeginChangeCheck();
            var type = (ConnectionType)EditorGUILayout.EnumPopup(connection.Type);
            if (EditorGUI.EndChangeCheck()) session.SetConnectionType(connection, type);
        }

        // --- Summary ---------------------------------------------------------------

        private static void DrawSummary(MapGraphEditorSession session)
        {
            MapGraph              graph    = session.Graph;
            MapGraphAnalysis      analysis = session.Analysis;
            MapGenerationSettings settings = session.Asset.Settings;

            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Seed", session.Asset.Seed.ToString());
            EditorGUILayout.LabelField("Nodes / Connections", $"{graph.Nodes.Count} / {graph.Connections.Count}");
            EditorGUILayout.LabelField("Main path", analysis.ExitReachable ? $"{analysis.MainPath.Count} nodes" : "Exit unreachable");
            EditorGUILayout.LabelField("Branches", analysis.BranchCount.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Node types", EditorStyles.miniBoldLabel);
            foreach (MapNodeType type in System.Enum.GetValues(typeof(MapNodeType)))
            {
                MapNodeTypeRule rule = settings != null ? settings.GetRule(type) : null;
                string limits = rule != null ? $"  ({rule.Min}–{rule.Max})" : "";
                EditorGUILayout.LabelField(type.ToString(), $"{graph.CountOf(type)}{limits}");
            }

            DrawMessages("Validation", session.Issues, MessageType.Warning, "No issues.");
            DrawMessages("Last generation", session.Asset.GenerationWarnings, MessageType.Info, null);
        }

        private static void DrawMessages(string title, IReadOnlyList<string> messages, MessageType type, string emptyText)
        {
            if (messages.Count == 0 && emptyText == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);

            if (messages.Count == 0)
            {
                EditorGUILayout.LabelField(emptyText, EditorStyles.miniLabel);
                return;
            }

            foreach (string message in messages)
                EditorGUILayout.HelpBox(message, type);
        }
    }
}
