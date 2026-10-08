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

            EditorGUI.BeginChangeCheck();
            int tier = EditorGUILayout.IntSlider(
                new GUIContent("Tier", "Room tier: tougher enemies, rarer resources and slightly better loot higher up"),
                node.Tier, 1, 3);
            if (EditorGUI.EndChangeCheck()) session.SetTier(node, tier);

            EditorGUI.BeginChangeCheck();
            var category = (RoomCategory)EditorGUILayout.EnumPopup(
                new GUIContent("Category", "What kind of ship space this room is. None = the level builder picks any fitting room function"),
                node.Category);
            if (EditorGUI.EndChangeCheck()) session.SetCategory(node, category);

            DrawFaction(node, session);
            DrawSection(node, session);

            EditorGUI.BeginChangeCheck();
            Vector2 position = EditorGUILayout.Vector2Field("Position", node.Position);
            if (EditorGUI.EndChangeCheck())
            {
                session.BeginMove();
                session.MoveNode(node, position);
            }

            DrawNodeAnalysis(node, session);
            DrawNodeConnections(node, session);
        }

        private static void DrawFaction(MapNode node, MapGraphEditorSession session)
        {
            MapContentSettings content = session.Asset.Content;
            int factionCount = content != null ? content.Factions.Count : 0;

            var options = new string[factionCount + 1];
            options[0] = "None";
            for (int i = 0; i < factionCount; i++)
                options[i + 1] = content.FactionName(i);

            EditorGUI.BeginChangeCheck();
            int faction = EditorGUILayout.Popup("Faction", node.Faction + 1, options) - 1;
            float influence = node.HasFaction
                ? EditorGUILayout.Slider("Influence", node.FactionInfluence, 0f, 1f)
                : 1f;
            if (EditorGUI.EndChangeCheck()) session.SetFaction(node, faction, influence);

            if (!node.Type.HasSecondFaction()) return;
            EditorGUI.BeginChangeCheck();
            int breach = EditorGUILayout.Popup("Breaching faction", node.BreachFaction + 1, options) - 1;
            if (EditorGUI.EndChangeCheck()) session.SetBreachFaction(node, breach);
        }

        private static void DrawSection(MapNode node, MapGraphEditorSession session)
        {
            MapContentSettings content = session.Asset.Content;
            int count = content != null ? content.Sections.Count : 0;
            if (count == 0) return;

            var options = new string[count + 1];
            options[0] = "None";
            for (int i = 0; i < count; i++)
                options[i + 1] = content.SectionName(i);

            EditorGUI.BeginChangeCheck();
            int section = EditorGUILayout.Popup("Section", node.Section + 1, options) - 1;
            if (EditorGUI.EndChangeCheck()) session.SetSection(node, section);
        }

        private static void DrawNodeAnalysis(MapNode node, MapGraphEditorSession session)
        {
            MapGraphAnalysis analysis = session.Analysis;
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
            if (analysis.IsBehindGate(node.Id))
                EditorGUILayout.LabelField("Access", "Behind a Locked or Secret gate");

            foreach (MapConnection connection in session.Graph.Connections)
                if (connection.Type == ConnectionType.Locked && connection.HoldsKey(node.Id))
                    EditorGUILayout.LabelField(connection.UsesSingleKey ? "Holds key for" : "Holds terminal for", $"#{connection.A} ↔ #{connection.B}");
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
            if (connection.Type == ConnectionType.Locked)
                DrawLock(connection, session);

            if (GUILayout.Button("Delete Connection"))
            {
                session.Disconnect(connection.A, connection.B);
                GUIUtility.ExitGUI();
            }
        }

        // A Keycard lock has one key room; a Terminal lock lists a room per terminal.
        private static void DrawLock(MapConnection connection, MapGraphEditorSession session)
        {
            EditorGUI.BeginChangeCheck();
            var kind = (MapLockKind)EditorGUILayout.EnumPopup(new GUIContent("Lock", "What opens this door"), connection.Lock);
            if (EditorGUI.EndChangeCheck()) session.SetLockKind(connection, kind);

            if (connection.UsesSingleKey)
            {
                int key = connection.HasKey ? connection.KeyNodeIds[0] : NoRoom;
                DrawKeyRoomPopup(connection, session, 0, key, new GUIContent("Key room", "Where the key that opens this door is placed"));
                return;
            }

            for (int i = 0; i < connection.KeyNodeIds.Count; i++)
                DrawKeyRoomPopup(connection, session, i, connection.KeyNodeIds[i], new GUIContent($"Terminal {i + 1}", "A room holding one of the terminals; all must be switched on"));
            DrawKeyRoomPopup(connection, session, connection.KeyNodeIds.Count, NoRoom, new GUIContent("Add terminal"));
        }

        private const int NoRoom = -1;

        // Any node can be picked; the validator flags a key or terminal the player can't reach.
        // Picking None removes the room.
        private static void DrawKeyRoomPopup(MapConnection connection, MapGraphEditorSession session, int index, int current, GUIContent label)
        {
            IReadOnlyList<MapNode> nodes = session.Graph.Nodes;
            var labels = new string[nodes.Count + 1];
            labels[0] = "None";
            int selected = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                labels[i + 1] = $"#{nodes[i].Id} {nodes[i].Type}";
                if (nodes[i].Id == current) selected = i + 1;
            }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup(label, selected, labels);
            if (!EditorGUI.EndChangeCheck()) return;

            if (picked == 0)
            {
                if (current != NoRoom) session.RemoveKeyRoom(connection, current);
            }
            else
            {
                session.SetKeyRoom(connection, index, nodes[picked - 1].Id);
            }
        }

        private static void DrawConnectionTypePopup(MapConnection connection, MapGraphEditorSession session)
        {
            EditorGUI.BeginChangeCheck();
            var type = (ConnectionType)EditorGUILayout.EnumPopup(connection.Type);
            if (EditorGUI.EndChangeCheck()) session.SetConnectionType(connection, type);

            EditorGUI.BeginChangeCheck();
            bool direct = EditorGUILayout.Toggle(
                new GUIContent("Direct door", "The two rooms touch with a doorway between them instead of a hallway. Needs rooms in neighbouring grid cells; otherwise the level builds a hallway"),
                connection.Direct);
            if (EditorGUI.EndChangeCheck()) session.SetDirect(connection, direct);

            if (connection.IsGate) return;
            EditorGUI.BeginChangeCheck();
            bool oneWay = EditorGUILayout.Toggle(new GUIContent("One-way", "Opens only from the end further from Start"), connection.OneWay);
            if (EditorGUI.EndChangeCheck()) session.SetOneWay(connection, oneWay);
        }

        // --- Summary ---------------------------------------------------------------

        private static void DrawSummary(MapGraphEditorSession session)
        {
            MapGraph              graph    = session.Graph;
            MapGraphAnalysis      analysis = session.Analysis;
            MapContentSettings    content  = session.Asset.Content;
            MapLayoutSettings     layout   = session.Asset.Layout;

            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Seed", session.Asset.Seed.ToString());
            EditorGUILayout.LabelField("Layout", layout != null
                ? $"{layout.name}  ({layout.MinRoomCount}–{layout.MaxRoomCount} rooms)"
                : "—");
            EditorGUILayout.LabelField("Nodes / Connections", $"{graph.Nodes.Count} / {graph.Connections.Count}");
            EditorGUILayout.LabelField("Main path", analysis.ExitReachable ? $"{analysis.MainPath.Count} nodes" : "Exit unreachable");
            EditorGUILayout.LabelField("Branches", analysis.BranchCount.ToString());
            MapRunTuning tuning = session.Asset.Tuning;
            DrawModifiers(session, tuning);
            MapFactionMix factionMix = session.Asset.FactionMix;
            EditorGUILayout.LabelField("Faction mix", factionMix != null ? factionMix.DisplayName : "—");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Node types", EditorStyles.miniBoldLabel);
            foreach (MapNodeType type in System.Enum.GetValues(typeof(MapNodeType)))
            {
                MapNodeTypeRule rule = content != null ? content.GetRule(type) : null;
                string limits = rule != null ? $"  ({tuning.Min(rule)}–{tuning.Max(rule)})" : "";
                EditorGUILayout.LabelField(type.ToString(), $"{graph.CountOf(type)}{limits}");
            }

            DrawLayers(session);
            DrawIssues(session);
            DrawMessages("Last generation", session.Asset.GenerationWarnings, MessageType.Info, null);
        }

        // Each generation layer can be rerolled on its own; everything else keeps its
        // numbers. Later layers still react to a changed layout.
        private static void DrawLayers(MapGraphEditorSession session)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seed layers", EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(!session.Asset.CanGenerate))
            {
                foreach (string layer in MapGenerator.Layers)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(layer, $"variant {session.Asset.LayerVariant(layer)}");
                        if (!GUILayout.Button("Reroll", GUILayout.Width(60f))) continue;
                        if (session.ConfirmDiscardEdits()) session.RerollLayer(layer);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        // Stored with the map: what the level announces and pays out, and what limits are
        // checked against. Regenerating rolls them afresh from the style.
        private static void DrawModifiers(MapGraphEditorSession session, MapRunTuning tuning)
        {
            var modifiers = new List<MapRunModifier>(tuning.Modifiers);
            EditorGUILayout.LabelField("Run modifiers", modifiers.Count == 0 ? "none" : "");

            for (int i = 0; i < modifiers.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                var picked = (MapRunModifier)EditorGUILayout.ObjectField(modifiers[i], typeof(MapRunModifier), false);
                bool remove = GUILayout.Button("×", GUILayout.Width(20f));
                EditorGUILayout.EndHorizontal();
                if (!EditorGUI.EndChangeCheck() && !remove) continue;

                if (remove || picked == null) modifiers.RemoveAt(i);
                else modifiers[i] = picked;
                session.SetModifiers(modifiers);
                return;
            }

            var added = (MapRunModifier)EditorGUILayout.ObjectField("Add modifier", null, typeof(MapRunModifier), false);
            if (added != null && !modifiers.Contains(added))
            {
                modifiers.Add(added);
                session.SetModifiers(modifiers);
            }
        }

        // Each issue that names a room or connection selects and centres it when clicked.
        private static void DrawIssues(MapGraphEditorSession session)
        {
            IReadOnlyList<string> issues = session.Issues;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.miniBoldLabel);
            if (issues.Count == 0)
            {
                EditorGUILayout.LabelField("No issues.", EditorStyles.miniLabel);
                return;
            }

            foreach (string issue in issues)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
                Rect box = GUILayoutUtility.GetLastRect();
                if (!MapIssueTarget.TryFind(issue, session.Graph, out MapNode node, out MapConnection connection)) continue;

                EditorGUIUtility.AddCursorRect(box, MouseCursor.Link);
                if (Event.current.type != EventType.MouseDown || !box.Contains(Event.current.mousePosition)) continue;

                if (connection != null)
                {
                    session.SelectConnection(connection);
                    session.Focus(SelectionCentre(session.Graph, connection));
                }
                else
                {
                    session.SelectNode(node.Id);
                    session.Focus(node.Position);
                }
                Event.current.Use();
            }
        }

        private static Vector2 SelectionCentre(MapGraph graph, MapConnection connection)
        {
            graph.TryGetNode(connection.A, out MapNode a);
            graph.TryGetNode(connection.B, out MapNode b);
            return (a.Position + b.Position) * 0.5f;
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
