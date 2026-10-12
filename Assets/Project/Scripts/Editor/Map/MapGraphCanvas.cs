using System.Collections.Generic;
using CGD.Factions;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Draws the graph and handles direct manipulation:
    //   click            select a node or connection
    //   drag node        move it (with Snap on, it drops into the nearest free grid cell)
    //   shift-drag node  connect it to the node you release over
    //   drag empty / middle-drag / alt-drag   pan
    //   scroll           zoom
    //   right-click      context menu (add, type, lock, disconnect, delete)
    //   Delete           delete selection     F   frame the whole graph
    //   hover node       everything known about it (MapNodeInfo)
    // With Details on, each node card also shows its tier, intensity (bottom bar), the room
    // the level gives it, its route and links, and how many validation issues name it.
    public class MapGraphCanvas
    {
        private const float ConnectionWidth     = 3f;
        private const float HighlightWidth      = 6f;
        private const float ConnectionHitRadius = 6f;
        private const float LabelMinZoom        = 0.6f;
        private const float TitleHeight         = 16f;
        private const float LineHeight          = 12f;
        private const float ChipHeight          = 16f;
        private const float HoverLineHeight     = 14f;

        private enum DragMode { None, Node, Connect, Pan }

        private static readonly List<PlannedObjective> NoObjectives = new();

        private readonly List<(Color color, string label)> _legend = new();
        // Locked connections in graph order; L1 is the first.
        private readonly List<MapConnection> _locks = new();
        // How far along each room's bottom edge the next key badge goes.
        private readonly Dictionary<int, float> _keyBadgeOffsets = new();
        private readonly List<string> _hoverLines = new();

        private DragMode _drag;
        private int      _dragNodeId = MapGraphEditorSession.None;
        private int      _hoverNodeId = MapGraphEditorSession.None;
        private Vector2  _mouse;
        private Vector2  _center;
        private Rect     _bounds;

        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;
        private GUIStyle _linkLabelStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _keyBadgeStyle;
        private GUIStyle _factionChipStyle;
        private GUIStyle _hoverStyle;

        public void Draw(Rect rect, MapGraphEditorSession session)
        {
            EnsureStyles();

            GUI.BeginClip(rect);
            var local = new Rect(Vector2.zero, rect.size);
            _center = local.center;
            _bounds = local;

            EditorGUI.DrawRect(local, MapGraphStyle.Background);
            DrawGrid(local, session);

            if (session.HasGraph)
            {
                HandleInput(session);
                CollectLocks(session.Graph);
                DrawConnections(session);
                DrawObjectiveRoutes(session);
                DrawPendingConnection(session);
                DrawNodes(session);
                DrawKeyBadges(session);
                DrawFactionChips(session);
                DrawTierAndIssueChips(session);
                DrawObjectiveBadges(session);
                DrawSnapTarget(session);
                DrawLegend(local, session);
                DrawHoverPanel(session);
            }

            GUI.EndClip();
        }

        public void Frame(MapGraphEditorSession session, Vector2 viewSize)
        {
            if (!session.HasGraph || session.Graph.Nodes.Count == 0) return;

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            foreach (MapNode node in session.Graph.Nodes)
            {
                min = Vector2.Min(min, node.Position);
                max = Vector2.Max(max, node.Position);
            }

            Vector2 size = max - min + MapGraphStyle.NodeSize * 2f;
            session.Zoom = Mathf.Min(viewSize.x / size.x, viewSize.y / size.y);
            session.Pan  = -(min + max) * 0.5f * session.Zoom;
        }

        // --- Coordinates -----------------------------------------------------------

        private Vector2 ToScreen(Vector2 world, MapGraphEditorSession session) =>
            _center + session.Pan + world * session.Zoom;

        private Vector2 ToWorld(Vector2 screen, MapGraphEditorSession session) =>
            (screen - _center - session.Pan) / session.Zoom;

        private Rect NodeRect(MapNode node, MapGraphEditorSession session)
        {
            Vector2 size = MapGraphStyle.NodeSize * session.Zoom;
            return new Rect(ToScreen(node.Position, session) - size * 0.5f, size);
        }

        // --- Drawing ---------------------------------------------------------------

        // With Snap on, the lines are the edges of the room cells nodes snap into.
        private void DrawGrid(Rect local, MapGraphEditorSession session)
        {
            if (session.HasGraph && session.SnapToGrid)
            {
                DrawCellGrid(local, session);
                return;
            }
            DrawGridLines(local, session, 20f, MapGraphStyle.GridMinor);
            DrawGridLines(local, session, 100f, MapGraphStyle.GridMajor);
        }

        private void DrawCellGrid(Rect local, MapGraphEditorSession session)
        {
            Vector2 spacing = session.CellSpacing * session.Zoom;
            if (spacing.x < 8f || spacing.y < 8f) return;

            // Cells are centred on whole multiples of the spacing, so edges sit half a cell off.
            Vector2 origin = _center + session.Pan + spacing * 0.5f;
            for (float x = Mathf.Repeat(origin.x, spacing.x); x < local.width; x += spacing.x)
                EditorGUI.DrawRect(new Rect(x, 0f, 1f, local.height), MapGraphStyle.GridMajor);
            for (float y = Mathf.Repeat(origin.y, spacing.y); y < local.height; y += spacing.y)
                EditorGUI.DrawRect(new Rect(0f, y, local.width, 1f), MapGraphStyle.GridMajor);
        }

        private void DrawGridLines(Rect local, MapGraphEditorSession session, float worldSpacing, Color color)
        {
            float spacing = worldSpacing * session.Zoom;
            if (spacing < 8f) return;

            Vector2 origin = _center + session.Pan;
            for (float x = Mathf.Repeat(origin.x, spacing); x < local.width; x += spacing)
                EditorGUI.DrawRect(new Rect(x, 0f, 1f, local.height), color);
            for (float y = Mathf.Repeat(origin.y, spacing); y < local.height; y += spacing)
                EditorGUI.DrawRect(new Rect(0f, y, local.width, 1f), color);
        }

        private void DrawConnections(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            MapConnection selected = session.SelectedConnection;

            foreach (MapConnection connection in session.Graph.Connections)
            {
                if (!TryGetCurve(connection, session, out MapGraphCurve curve)) continue;

                bool dimmed = session.IsDimmed(connection.A) && session.IsDimmed(connection.B);
                Color color = MapGraphStyle.ConnectionColor(connection.Type);
                if (dimmed) color.a = 0.25f;

                if (connection == selected)
                    curve.Draw(MapGraphStyle.Selection, HighlightWidth);
                else if (session.IsOnHighlightedPath(connection))
                    curve.Draw(MapGraphStyle.PathHighlight, HighlightWidth);

                curve.Draw(color, ConnectionWidth);

                if ((connection.Type != ConnectionType.Normal || connection.OneWay || connection.Direct) && session.Zoom >= LabelMinZoom)
                    DrawConnectionLabel(curve.Midpoint, connection, session.Analysis);
            }
        }

        // Locked links also name their key or terminal rooms, so a missing one is visible at a glance.
        private void DrawConnectionLabel(Vector2 at, MapConnection connection, MapGraphAnalysis analysis)
        {
            ConnectionType type = connection.Type;
            string text = type != ConnectionType.Locked ? type.ToString()
                        : LockLabel(connection, analysis);
            if (connection.Direct) text = type == ConnectionType.Normal ? "Direct" : text + " · direct";
            if (connection.OneWay) text += " · one-way";
            var content = new GUIContent(text);
            Vector2 size = _linkLabelStyle.CalcSize(content);
            var rect = new Rect(at - size * 0.5f, size);

            EditorGUI.DrawRect(rect, MapGraphStyle.Background);
            _linkLabelStyle.normal.textColor = MapGraphStyle.ConnectionColor(type);
            GUI.Label(rect, content, _linkLabelStyle);
        }

        // "L2 Main lock · key #5": a main lock stands between Start and the Exit.
        private string LockLabel(MapConnection connection, MapGraphAnalysis analysis)
        {
            string name = $"{LockName(connection)} {(analysis.IsRequired(connection) ? "Main lock" : "Locked")}";
            if (connection.UsesSingleKey)
                return connection.HasKey ? $"{name} · key #{connection.KeyNodeIds[0]}" : $"{name} · no key";
            if (!connection.HasKey) return $"{name} · no terminals";
            return $"{name} · terminals #" + string.Join(" #", connection.KeyNodeIds);
        }

        // --- Locks -----------------------------------------------------------------

        private void CollectLocks(MapGraph graph)
        {
            _locks.Clear();
            foreach (MapConnection connection in graph.Connections)
                if (connection.Type == ConnectionType.Locked)
                    _locks.Add(connection);
        }

        private string LockName(MapConnection connection) => $"L{_locks.IndexOf(connection) + 1}";

        // "L1 key" or "L2 T1" (its first terminal) along the bottom of each room holding a
        // part, outlined in the lock colour so they read apart from objective badges.
        private void DrawKeyBadges(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            _keyBadgeOffsets.Clear();
            Color color = MapGraphStyle.ConnectionColor(ConnectionType.Locked);
            _keyBadgeStyle.normal.textColor = color;

            foreach (MapConnection connection in _locks)
            {
                for (int i = 0; i < connection.KeyNodeIds.Count; i++)
                {
                    if (!session.Graph.TryGetNode(connection.KeyNodeIds[i], out MapNode node)) continue;

                    string text = connection.UsesSingleKey ? $"{LockName(connection)} key" : $"{LockName(connection)} T{i + 1}";
                    var content = new GUIContent(text);
                    float width = _keyBadgeStyle.CalcSize(content).x + 6f;

                    Rect rect = NodeRect(node, session);
                    _keyBadgeOffsets.TryGetValue(node.Id, out float offset);
                    var badge = new Rect(rect.x - 6f + offset, rect.yMax - 7f, width, 16f);
                    _keyBadgeOffsets[node.Id] = offset + width + 2f;

                    DrawOutline(badge, color, 1f);
                    EditorGUI.DrawRect(badge, MapGraphStyle.Background);
                    GUI.Label(badge, content, _keyBadgeStyle);
                }
            }
        }

        private void DrawPendingConnection(MapGraphEditorSession session)
        {
            if (_drag != DragMode.Connect || Event.current.type != EventType.Repaint) return;
            if (!session.Graph.TryGetNode(_dragNodeId, out MapNode from)) return;

            Vector2 start = ToScreen(from.Position, session);
            Handles.DrawBezier(start, _mouse, start, _mouse, MapGraphStyle.Selection, null, ConnectionWidth);
        }

        private void DrawNodes(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            foreach (MapNode node in session.Graph.Nodes)
                DrawNode(node, session);
        }

        private void DrawNode(MapNode node, MapGraphEditorSession session)
        {
            Rect rect = NodeRect(node, session);

            if (node.Id == session.SelectedNodeId)
                DrawOutline(rect, MapGraphStyle.Selection, 3f);
            else if (session.IsOnHighlightedPath(node.Id))
                DrawOutline(rect, MapGraphStyle.PathHighlight, 3f);

            EditorGUI.DrawRect(rect, MapGraphStyle.NodeColor(node, session.Analysis, session.ViewMode));
            if (session.ShowDetails) DrawNodeStrips(rect, node, session);
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.5f), 1f);

            string edited = session.IsEdited(node) ? "*" : "";
            if (session.Zoom < LabelMinZoom)
            {
                GUI.Label(rect, $"#{node.Id}{edited}", _titleStyle);
            }
            else if (session.ShowDetails)
            {
                DrawNodeLines(rect, node, session, $"{node.Type} #{node.Id}{edited}");
            }
            else
            {
                var title  = new Rect(rect.x, rect.y + 4f, rect.width, rect.height * 0.5f);
                var detail = new Rect(rect.x, rect.center.y, rect.width, rect.height * 0.5f - 2f);
                GUI.Label(title, $"{node.Type} #{node.Id}{edited}", _titleStyle);
                GUI.Label(detail, Detail(node, session), _detailStyle);
            }

            if (session.Asset.IsLocked(node.Id))
            {
                // With details the top corners hold chips, so the lock moves to the right edge's middle.
                float y = session.ShowDetails ? rect.center.y - 9f : rect.y;
                GUI.Label(new Rect(rect.xMax - 18f, y, 18f, 18f), EditorGUIUtility.IconContent("IN LockButton on"));
            }

            if (session.IsDimmed(node.Id))
                EditorGUI.DrawRect(rect, MapGraphStyle.Dimmed);
        }

        // A stripe in the room type's colour down the left edge (when colour shows something
        // else), and the intensity as a bar along the bottom.
        private static void DrawNodeStrips(Rect rect, MapNode node, MapGraphEditorSession session)
        {
            if (session.ViewMode != MapGraphViewMode.Type)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, Mathf.Max(2f, 5f * session.Zoom), rect.height), MapGraphStyle.TypeColor(node.Type));

            float height = Mathf.Max(2f, 4f * session.Zoom);
            var track = new Rect(rect.x, rect.yMax - height, rect.width, height);
            EditorGUI.DrawRect(track, MapGraphStyle.BarTrack);
            EditorGUI.DrawRect(new Rect(track.x, track.y, track.width * node.Intensity, height), MapGraphStyle.IntensityColor(node.Intensity));
        }

        // The title, then as many detail lines as the card's height allows at this zoom:
        // room, route, then enemies / links / section.
        private void DrawNodeLines(Rect rect, MapNode node, MapGraphEditorSession session, string title)
        {
            LevelBuildSettings build = session.Objectives.BuildSettings;
            float y = rect.y + 2f;
            GUI.Label(new Rect(rect.x + 4f, y, rect.width - 8f, TitleHeight), title, _titleStyle);
            y += TitleHeight;

            float bottom = rect.yMax - Mathf.Max(2f, 4f * session.Zoom) - 1f;
            for (int line = 0; line < 3 && y + LineHeight <= bottom; line++, y += LineHeight)
            {
                string text = line switch
                {
                    0 => MapNodeInfo.RoomLine(node, session, build),
                    1 => MapNodeInfo.RouteLine(node, session),
                    _ => MapNodeInfo.StatsLine(node, session, build)
                };
                GUI.Label(new Rect(rect.x + 4f, y, rect.width - 8f, LineHeight), text, _detailStyle);
            }
        }

        // Second line of a node: whatever the current view mode is about.
        private static string FactionName(MapGraphEditorSession session, int faction) =>
            session.Asset.Content?.FactionName(faction) ?? faction.ToString();

        private static string Detail(MapNode node, MapGraphEditorSession session)
        {
            MapGraphAnalysis analysis = session.Analysis;
            return session.ViewMode switch
            {
                MapGraphViewMode.Intensity        => $"intensity {node.Intensity:0.00}",
                MapGraphViewMode.Faction          => node.HasFaction
                    ? $"{FactionName(session, node.Faction)} {node.FactionInfluence:0.00}"
                      + (node.HasBreachFaction ? $" + {FactionName(session, node.BreachFaction)}" : "")
                    : "no faction",
                MapGraphViewMode.Section          => node.HasSection
                    ? session.Asset.Content?.SectionName(node.Section) ?? node.Section.ToString()
                    : "no section",
                MapGraphViewMode.Zone             => node.Category != RoomCategory.None ? $"{node.Category.Zone()} · {node.Category}" : "no zone",
                MapGraphViewMode.Category         => node.Category != RoomCategory.None ? node.Category.ToString() : "no category",
                MapGraphViewMode.RequiredOptional => !analysis.IsReachable(node.Id) ? "unreachable"
                    : analysis.IsRequired(node.Id) ? "required" : "optional",
                MapGraphViewMode.Branches         => analysis.BranchOf(node.Id) == MapGraphAnalysis.MainPathBranch
                    ? "main path" : $"branch {analysis.BranchOf(node.Id) + 1}",
                _                                 => $"depth {analysis.Depth(node.Id)}"
            };
        }

        // --- Factions --------------------------------------------------------------

        // The faction's short name in its own colour on the room's bottom-right corner, in
        // every view mode; a Breach room's second faction sits beside it.
        private void DrawFactionChips(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            MapContentSettings content = session.Asset.Content;
            foreach (MapNode node in session.Graph.Nodes)
            {
                if (!node.HasFaction) continue;

                Rect  rect  = NodeRect(node, session);
                float right = rect.xMax + 6f;
                if (node.HasBreachFaction)
                    right = DrawFactionChip(content, node.BreachFaction, right, rect.yMax - 7f) - 2f;
                DrawFactionChip(content, node.Faction, right, rect.yMax - 7f);
            }
        }

        // Draws the chip ending at `right`; returns where it starts.
        private float DrawFactionChip(MapContentSettings content, int faction, float right, float y)
        {
            FactionDefinition definition = content != null ? content.GetFaction(faction) : null;
            string name  = definition != null ? definition.DisplayName : $"F{faction}";
            if (name.Length > 4) name = name.Substring(0, 4);
            Color  color = definition != null ? definition.Color : MapGraphStyle.PaletteColor(faction);
            return DrawChip(name.ToUpperInvariant(), color, right, y);
        }

        // --- Tier, issues, hover ----------------------------------------------------

        // "T2" on the room's top-right corner in the tier's colour, with "!3" beside it when
        // validation issues name the room or one of its links.
        private void DrawTierAndIssueChips(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint || !session.ShowDetails) return;

            foreach (MapNode node in session.Graph.Nodes)
            {
                Rect  rect  = NodeRect(node, session);
                float right = DrawChip($"T{node.EffectiveTier}", MapGraphStyle.TierColor(node.EffectiveTier), rect.xMax + 6f, rect.y - 9f) - 2f;

                int issues = session.IssuesAbout(node.Id).Count;
                if (issues > 0) DrawChip($"!{issues}", MapGraphStyle.Issue, right, rect.y - 9f);
            }
        }

        // Draws a filled chip ending at `right`; returns where it starts.
        private float DrawChip(string text, Color color, float right, float y)
        {
            var label = new GUIContent(text);
            float width = _factionChipStyle.CalcSize(label).x + 6f;
            var chip = new Rect(right - width, y, width, ChipHeight);

            EditorGUI.DrawRect(chip, color);
            _factionChipStyle.normal.textColor = color.grayscale > 0.5f ? Color.black : Color.white;
            GUI.Label(chip, label, _factionChipStyle);
            return chip.x;
        }

        private void UpdateHover(Vector2 mouse, MapGraphEditorSession session)
        {
            MapNode node = _drag == DragMode.None && _bounds.Contains(mouse) ? HitNode(mouse, session) : null;
            int id = node != null ? node.Id : MapGraphEditorSession.None;
            if (id == _hoverNodeId) return;

            _hoverNodeId = id;
            GUI.changed  = true;
        }

        // Everything known about the hovered room, beside the cursor and kept inside the canvas.
        private void DrawHoverPanel(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint || _drag != DragMode.None) return;
            if (!session.Graph.TryGetNode(_hoverNodeId, out MapNode node)) return;

            MapNodeInfo.Describe(node, session, session.Objectives.BuildSettings, _locks, ObjectivePlan(session), _hoverLines);

            float width = 0f;
            for (int i = 0; i < _hoverLines.Count; i++)
            {
                _hoverStyle.fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal;
                width = Mathf.Max(width, _hoverStyle.CalcSize(new GUIContent(_hoverLines[i])).x);
            }
            var size  = new Vector2(Mathf.Min(width, 460f) + 12f, _hoverLines.Count * HoverLineHeight + 10f);
            var panel = new Rect(_mouse + new Vector2(16f, 16f), size);
            if (panel.xMax > _bounds.xMax) panel.x = _mouse.x - 16f - size.x;
            if (panel.yMax > _bounds.yMax) panel.y = Mathf.Max(_bounds.y, _bounds.yMax - size.y);

            EditorGUI.DrawRect(panel, new Color(0.08f, 0.08f, 0.09f, 0.95f));
            DrawOutline(panel, MapGraphStyle.TypeColor(node.Type), 1f);
            for (int i = 0; i < _hoverLines.Count; i++)
            {
                _hoverStyle.fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal;
                _hoverStyle.normal.textColor = _hoverLines[i].StartsWith("  • ") ? MapGraphStyle.Issue : MapGraphStyle.NodeText;
                GUI.Label(new Rect(panel.x + 6f, panel.y + 5f + i * HoverLineHeight, panel.width - 12f, HoverLineHeight), _hoverLines[i], _hoverStyle);
            }
        }

        // --- Objectives ------------------------------------------------------------

        private static IReadOnlyList<PlannedObjective> ObjectivePlan(MapGraphEditorSession session) =>
            session.ShowObjectives ? session.Objectives.PlanFor(session) : NoObjectives;

        // A dotted line from step to step of each objective, in its colour.
        private void DrawObjectiveRoutes(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            IReadOnlyList<PlannedObjective> plan = ObjectivePlan(session);
            for (int i = 0; i < plan.Count; i++)
            {
                Handles.color = MapObjectivePreview.ColorOf(plan, i);
                IReadOnlyList<int> steps = plan[i].NodeIds;
                for (int s = 1; s < steps.Count; s++)
                    if (session.Graph.TryGetNode(steps[s - 1], out MapNode from) && session.Graph.TryGetNode(steps[s], out MapNode to))
                        Handles.DrawDottedLine(ToScreen(from.Position, session), ToScreen(to.Position, session), 5f);
            }
            Handles.color = Color.white;
        }

        // "A2" on the corner of the room holding objective A's second step.
        private void DrawObjectiveBadges(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            IReadOnlyList<PlannedObjective> plan = ObjectivePlan(session);
            for (int i = 0; i < plan.Count; i++)
            {
                Color color = MapObjectivePreview.ColorOf(plan, i);
                IReadOnlyList<int> steps = plan[i].NodeIds;
                for (int s = 0; s < steps.Count; s++)
                {
                    if (!session.Graph.TryGetNode(steps[s], out MapNode node)) continue;

                    Rect rect  = NodeRect(node, session);
                    var  badge = new Rect(rect.x - 6f, rect.y - 9f, 26f, 16f);
                    EditorGUI.DrawRect(badge, color);
                    GUI.Label(badge, $"{MapObjectivePreview.Letter(i)}{s + 1}", _badgeStyle);
                }
            }

            DrawBossRequirement(session, plan);
        }

        // The Boss doors stay shut until every main objective is done: "needs A B".
        private void DrawBossRequirement(MapGraphEditorSession session, IReadOnlyList<PlannedObjective> plan)
        {
            MapNode boss = session.Graph.FindFirst(MapNodeType.Boss);
            if (boss == null) return;

            string letters = "";
            for (int i = 0; i < plan.Count; i++)
                if (plan[i].IsMain) letters += " " + MapObjectivePreview.Letter(i);
            if (letters.Length == 0) return;

            var content = new GUIContent("needs" + letters);
            Rect rect  = NodeRect(boss, session);
            var  badge = new Rect(rect.x - 6f, rect.y - 9f, _badgeStyle.CalcSize(content).x + 6f, 16f);
            EditorGUI.DrawRect(badge, MapObjectivePreview.MainColor);
            GUI.Label(badge, content, _badgeStyle);
        }

        // Where the dragged node will land when released.
        private void DrawSnapTarget(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint || _drag != DragMode.Node || !session.SnapToGrid) return;
            if (!session.Graph.TryGetNode(_dragNodeId, out MapNode node)) return;

            Vector2 target = MapGraphGrid.SnapToFreeCell(session.Graph, node.Id, node.Position, session.CellSpacing);
            Vector2 size   = MapGraphStyle.NodeSize * session.Zoom;
            DrawOutline(new Rect(ToScreen(target, session) - size * 0.5f, size), new Color(1f, 1f, 1f, 0.5f), 2f);
        }

        private void DrawLegend(Rect local, MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            MapGraphStyle.CollectLegend(session.ViewMode, session.Asset.Content, session.Analysis, _legend);
            if (session.ShowDetails)
            {
                for (int tier = 1; tier <= 3; tier++) _legend.Add((MapGraphStyle.TierColor(tier), $"T{tier} room tier"));
                _legend.Add((MapGraphStyle.IntensityColor(1f), "Bar: intensity"));
                _legend.Add((MapGraphStyle.Issue, "!n issues (hover)"));
            }

            const float line = 16f;
            var box = new Rect(8f, local.height - 8f - _legend.Count * line - 8f, 140f, _legend.Count * line + 8f);
            EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.4f));

            for (int i = 0; i < _legend.Count; i++)
            {
                float y = box.y + 4f + i * line;
                EditorGUI.DrawRect(new Rect(box.x + 6f, y + 3f, 10f, 10f), _legend[i].color);
                GUI.Label(new Rect(box.x + 22f, y, box.width - 24f, line), _legend[i].label, EditorStyles.miniLabel);
            }
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y - thickness, rect.width + thickness * 2f, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.yMax, rect.width + thickness * 2f, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, thickness, rect.height), color);
        }

        // --- Input -----------------------------------------------------------------

        private void HandleInput(MapGraphEditorSession session)
        {
            Event e = Event.current;
            _mouse = e.mousePosition;
            if (e.type is EventType.MouseMove or EventType.MouseDrag or EventType.MouseUp) UpdateHover(_mouse, session);
            if (e.type == EventType.MouseLeaveWindow && _hoverNodeId != MapGraphEditorSession.None)
            {
                _hoverNodeId = MapGraphEditorSession.None;
                GUI.changed  = true;
            }

            // Clicks, scrolls and keys belong to the canvas only when aimed at it; drags
            // and releases must still reach it after the cursor leaves mid-drag.
            bool startsHere = e.type is EventType.MouseDown or EventType.ScrollWheel or EventType.ContextClick;
            if (startsHere && !_bounds.Contains(e.mousePosition)) return;
            if (e.type == EventType.KeyDown && EditorGUIUtility.editingTextField) return;

            switch (e.type)
            {
                case EventType.MouseDown:    OnMouseDown(e, session); break;
                case EventType.MouseDrag:    OnMouseDrag(e, session); break;
                case EventType.MouseUp:      OnMouseUp(e, session); break;
                case EventType.ScrollWheel:  OnScroll(e, session); break;
                case EventType.ContextClick: OnContextClick(e, session); break;
                case EventType.KeyDown:      OnKeyDown(e, session); break;
            }
        }

        private void OnMouseDown(Event e, MapGraphEditorSession session)
        {
            GUI.FocusControl(null);

            if (e.button == 2 || (e.button == 0 && e.alt))
            {
                StartDrag(DragMode.Pan, e);
                return;
            }
            if (e.button != 0) return;

            MapNode node = HitNode(e.mousePosition, session);
            if (node != null)
            {
                session.SelectNode(node.Id);
                _dragNodeId = node.Id;
                if (!e.shift) session.BeginMove();
                StartDrag(e.shift ? DragMode.Connect : DragMode.Node, e);
                return;
            }

            MapConnection connection = HitConnection(e.mousePosition, session);
            if (connection != null)
            {
                session.SelectConnection(connection);
                e.Use();
                return;
            }

            session.ClearSelection();
            StartDrag(DragMode.Pan, e);
        }

        private void StartDrag(DragMode mode, Event e)
        {
            _drag = mode;
            GUI.changed = true;
            e.Use();
        }

        private void OnMouseDrag(Event e, MapGraphEditorSession session)
        {
            switch (_drag)
            {
                case DragMode.Node when session.Graph.TryGetNode(_dragNodeId, out MapNode node):
                    session.MoveNode(node, node.Position + e.delta / session.Zoom);
                    break;
                case DragMode.Pan:
                    session.Pan += e.delta;
                    break;
                case DragMode.Connect:
                    break;
                default:
                    return;
            }

            GUI.changed = true;
            e.Use();
        }

        private void OnMouseUp(Event e, MapGraphEditorSession session)
        {
            if (_drag == DragMode.None) return;

            if (_drag == DragMode.Connect)
            {
                MapNode target = HitNode(e.mousePosition, session);
                if (target != null && target.Id != _dragNodeId)
                    session.Connect(_dragNodeId, target.Id);
            }
            else if (_drag == DragMode.Node && session.Graph.TryGetNode(_dragNodeId, out MapNode moved))
            {
                session.EndMove(moved);
            }

            _drag       = DragMode.None;
            _dragNodeId = MapGraphEditorSession.None;
            GUI.changed = true;
            e.Use();
        }

        // Zooms around the cursor so the point under it stays put.
        private void OnScroll(Event e, MapGraphEditorSession session)
        {
            Vector2 world = ToWorld(e.mousePosition, session);
            session.Zoom *= 1f - e.delta.y * 0.05f;
            session.Pan = e.mousePosition - _center - world * session.Zoom;

            GUI.changed = true;
            e.Use();
        }

        private void OnKeyDown(Event e, MapGraphEditorSession session)
        {
            switch (e.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    DeleteSelection(session);
                    break;
                case KeyCode.F:
                    Frame(session, _bounds.size);
                    break;
                default:
                    return;
            }

            GUI.changed = true;
            e.Use();
        }

        private static void DeleteSelection(MapGraphEditorSession session)
        {
            if (session.SelectedNode != null)
                session.DeleteNode(session.SelectedNodeId);
            else if (session.SelectedConnection is { } connection)
                session.Disconnect(connection.A, connection.B);
        }

        // --- Context menus ---------------------------------------------------------

        private void OnContextClick(Event e, MapGraphEditorSession session)
        {
            Vector2 mouse = e.mousePosition;
            var menu = new GenericMenu();

            MapNode node = HitNode(mouse, session);
            MapConnection connection = node == null ? HitConnection(mouse, session) : null;

            if (node != null)
            {
                session.SelectNode(node.Id);
                MapGraphContextMenu.AddNodeItems(menu, node, session);
            }
            else if (connection != null)
            {
                session.SelectConnection(connection);
                MapGraphContextMenu.AddConnectionItems(menu, connection, session);
            }
            else
            {
                MapGraphContextMenu.AddCanvasItems(menu, ToWorld(mouse, session), session);
            }

            menu.ShowAsContext();
            e.Use();
        }

        // --- Hit testing -----------------------------------------------------------

        // Topmost first — nodes later in the list are drawn on top.
        private MapNode HitNode(Vector2 point, MapGraphEditorSession session)
        {
            IReadOnlyList<MapNode> nodes = session.Graph.Nodes;
            for (int i = nodes.Count - 1; i >= 0; i--)
                if (NodeRect(nodes[i], session).Contains(point))
                    return nodes[i];
            return null;
        }

        private MapConnection HitConnection(Vector2 point, MapGraphEditorSession session)
        {
            foreach (MapConnection connection in session.Graph.Connections)
                if (TryGetCurve(connection, session, out MapGraphCurve curve) && curve.Distance(point) <= ConnectionHitRadius)
                    return connection;
            return null;
        }

        // --- Curves ----------------------------------------------------------------

        private bool TryGetCurve(MapConnection connection, MapGraphEditorSession session, out MapGraphCurve curve)
        {
            curve = default;
            if (!session.Graph.TryGetNode(connection.A, out MapNode a)) return false;
            if (!session.Graph.TryGetNode(connection.B, out MapNode b)) return false;

            curve = MapGraphCurve.Between(ToScreen(a.Position, session), ToScreen(b.Position, session),
                                          connection.Type, session.Zoom);
            return true;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = MapGraphStyle.NodeText }
            };
            _detailStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(1f, 1f, 1f, 0.8f) }
            };
            _linkLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            _badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.black }
            };
            _keyBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            _factionChipStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            _hoverStyle = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
            _titleStyle.clipping  = TextClipping.Clip;
            _detailStyle.clipping = TextClipping.Clip;
        }
    }
}
