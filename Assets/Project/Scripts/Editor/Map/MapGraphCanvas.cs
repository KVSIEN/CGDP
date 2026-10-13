using System.Collections.Generic;
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
    // Node cards, their badges and the hover panel are drawn by MapNodeCards.
    public class MapGraphCanvas
    {
        private const float ConnectionWidth     = 3f;
        private const float HighlightWidth      = 6f;
        private const float ConnectionHitRadius = 6f;

        private enum DragMode { None, Node, Connect, Pan }

        private static readonly List<PlannedObjective> NoObjectives = new();

        private readonly List<(Color color, string label)> _legend = new();
        private (int version, MapGraphViewMode mode, bool details) _legendKey = (-1, default, false);
        private readonly MapNodeCards _cards = new();
        // Link labels, formatted once per graph change.
        private readonly Dictionary<MapConnection, string> _linkLabels = new();
        private int _linkLabelsVersion = -1;
        private readonly GUIContent _content = new();

        private DragMode _drag;
        private int      _dragNodeId = MapGraphEditorSession.None;
        private int      _hoverNodeId = MapGraphEditorSession.None;
        private Vector2  _mouse;
        private Vector2  _center;
        private Rect     _bounds;

        private GUIStyle _linkLabelStyle;

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
                IReadOnlyList<PlannedObjective> plan = ObjectivePlan(session);
                DrawConnections(session);
                DrawObjectiveRoutes(session, plan);
                DrawPendingConnection(session);
                _cards.Draw(session, node => NodeRect(node, session), plan);
                DrawSnapTarget(session);
                DrawLegend(local, session);
                if (_drag == DragMode.None && session.Graph.TryGetNode(_hoverNodeId, out MapNode hovered))
                    _cards.DrawHoverPanel(session, hovered, _mouse, _bounds, plan);
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

                if ((connection.Type != ConnectionType.Normal || connection.OneWay || connection.Direct) && session.Zoom >= MapGraphStyle.LabelMinZoom)
                    DrawConnectionLabel(curve.Midpoint, connection, session);
            }
        }

        private void DrawConnectionLabel(Vector2 at, MapConnection connection, MapGraphEditorSession session)
        {
            _content.text = LinkLabel(connection, session);
            Vector2 size = _linkLabelStyle.CalcSize(_content);
            var rect = new Rect(at - size * 0.5f, size);

            EditorGUI.DrawRect(rect, MapGraphStyle.Background);
            _linkLabelStyle.normal.textColor = MapGraphStyle.ConnectionColor(connection.Type);
            GUI.Label(rect, _content, _linkLabelStyle);
        }

        // Locked links also name their key or terminal rooms, so a missing one is visible at a glance.
        private string LinkLabel(MapConnection connection, MapGraphEditorSession session)
        {
            if (_linkLabelsVersion != session.Version)
            {
                _linkLabels.Clear();
                _linkLabelsVersion = session.Version;
            }
            if (_linkLabels.TryGetValue(connection, out string text)) return text;

            ConnectionType type = connection.Type;
            text = type != ConnectionType.Locked ? type.ToString() : LockLabel(connection, session);
            if (connection.Direct) text = type == ConnectionType.Normal ? "Direct" : text + " · direct";
            if (connection.OneWay) text += " · one-way";
            _linkLabels[connection] = text;
            return text;
        }

        // "L2 Main lock · key #5": a main lock stands between Start and the Exit.
        private static string LockLabel(MapConnection connection, MapGraphEditorSession session)
        {
            string name = $"{session.LockName(connection)} {(session.Analysis.IsRequired(connection) ? "Main lock" : "Locked")}";
            if (connection.UsesSingleKey)
                return connection.HasKey ? $"{name} · key #{connection.KeyNodeIds[0]}" : $"{name} · no key";
            if (!connection.HasKey) return $"{name} · no terminals";
            return $"{name} · terminals #" + string.Join(" #", connection.KeyNodeIds);
        }

        private void DrawPendingConnection(MapGraphEditorSession session)
        {
            if (_drag != DragMode.Connect || Event.current.type != EventType.Repaint) return;
            if (!session.Graph.TryGetNode(_dragNodeId, out MapNode from)) return;

            Vector2 start = ToScreen(from.Position, session);
            Handles.DrawBezier(start, _mouse, start, _mouse, MapGraphStyle.Selection, null, ConnectionWidth);
        }

        // --- Objectives ------------------------------------------------------------

        private static IReadOnlyList<PlannedObjective> ObjectivePlan(MapGraphEditorSession session) =>
            session.ShowObjectives ? session.Objectives.PlanFor(session) : NoObjectives;

        // A dotted line from step to step of each objective, in its colour.
        private void DrawObjectiveRoutes(MapGraphEditorSession session, IReadOnlyList<PlannedObjective> plan)
        {
            if (Event.current.type != EventType.Repaint) return;

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

        // Where the dragged node will land when released.
        private void DrawSnapTarget(MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint || _drag != DragMode.Node || !session.SnapToGrid) return;
            if (!session.Graph.TryGetNode(_dragNodeId, out MapNode node)) return;

            Vector2 target = MapGraphGrid.SnapToFreeCell(session.Graph, node.Id, node.Position, session.CellSpacing);
            Vector2 size   = MapGraphStyle.NodeSize * session.Zoom;
            MapGraphStyle.DrawOutline(new Rect(ToScreen(target, session) - size * 0.5f, size), new Color(1f, 1f, 1f, 0.5f), 2f);
        }

        private void DrawLegend(Rect local, MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            RefreshLegend(session);

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

        // Rebuilt only when what it shows can change: the view mode, Details, or the graph
        // (its branches; an edit also picks up renamed factions and sections).
        private void RefreshLegend(MapGraphEditorSession session)
        {
            var key = (session.Version, session.ViewMode, session.ShowDetails);
            if (key == _legendKey) return;
            _legendKey = key;

            MapGraphStyle.CollectLegend(session.ViewMode, session.Asset.Content, session.Analysis, _legend);
            if (!session.ShowDetails) return;
            for (int tier = 1; tier <= 3; tier++) _legend.Add((MapGraphStyle.TierColor(tier), $"T{tier} room tier"));
            _legend.Add((MapGraphStyle.IntensityColor(1f), "Bar: intensity"));
            _legend.Add((MapGraphStyle.Issue, "!n issues (hover)"));
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

        // The node under the cursor, for the hover panel; none while dragging.
        private void UpdateHover(Vector2 mouse, MapGraphEditorSession session)
        {
            MapNode node = _drag == DragMode.None && _bounds.Contains(mouse) ? HitNode(mouse, session) : null;
            int id = node != null ? node.Id : MapGraphEditorSession.None;
            if (id == _hoverNodeId) return;

            _hoverNodeId = id;
            GUI.changed  = true;
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
            if (_linkLabelStyle != null) return;
            _linkLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        }
    }
}
