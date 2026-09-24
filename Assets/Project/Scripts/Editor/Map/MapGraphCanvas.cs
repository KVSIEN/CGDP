using System;
using System.Collections.Generic;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Draws the graph and handles direct manipulation:
    //   click            select a node or connection
    //   drag node        move it
    //   shift-drag node  connect it to the node you release over
    //   drag empty / middle-drag / alt-drag   pan
    //   scroll           zoom
    //   right-click      context menu (add, type, lock, disconnect, delete)
    //   Delete           delete selection     F   frame the whole graph
    public class MapGraphCanvas
    {
        private const float ConnectionWidth     = 3f;
        private const float HighlightWidth      = 6f;
        private const float ConnectionHitRadius = 6f;
        private const float LabelMinZoom        = 0.6f;

        private enum DragMode { None, Node, Connect, Pan }

        private readonly List<(Color color, string label)> _legend = new();

        private DragMode _drag;
        private int      _dragNodeId = MapGraphEditorSession.None;
        private Vector2  _mouse;
        private Vector2  _center;
        private Rect     _bounds;

        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;
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
                DrawConnections(session);
                DrawPendingConnection(session);
                DrawNodes(session);
                DrawLegend(local, session);
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

        private void DrawGrid(Rect local, MapGraphEditorSession session)
        {
            DrawGridLines(local, session, 20f, MapGraphStyle.GridMinor);
            DrawGridLines(local, session, 100f, MapGraphStyle.GridMajor);
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
                if (!TryGetCurve(connection, session, out Curve curve)) continue;

                bool dimmed = session.IsDimmed(connection.A) && session.IsDimmed(connection.B);
                Color color = MapGraphStyle.ConnectionColor(connection.Type);
                if (dimmed) color.a = 0.25f;

                if (connection == selected)
                    curve.Draw(MapGraphStyle.Selection, HighlightWidth);
                else if (session.IsOnHighlightedPath(connection))
                    curve.Draw(MapGraphStyle.PathHighlight, HighlightWidth);

                curve.Draw(color, ConnectionWidth);

                if (connection.Type != ConnectionType.Normal && session.Zoom >= LabelMinZoom)
                    DrawConnectionLabel(curve.Midpoint, connection.Type);
            }
        }

        private void DrawConnectionLabel(Vector2 at, ConnectionType type)
        {
            var content = new GUIContent(type.ToString());
            Vector2 size = _linkLabelStyle.CalcSize(content);
            var rect = new Rect(at - size * 0.5f, size);

            EditorGUI.DrawRect(rect, MapGraphStyle.Background);
            _linkLabelStyle.normal.textColor = MapGraphStyle.ConnectionColor(type);
            GUI.Label(rect, content, _linkLabelStyle);
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
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.5f), 1f);

            string edited = session.IsEdited(node) ? "*" : "";
            if (session.Zoom >= LabelMinZoom)
            {
                var title  = new Rect(rect.x, rect.y + 4f, rect.width, rect.height * 0.5f);
                var detail = new Rect(rect.x, rect.center.y, rect.width, rect.height * 0.5f - 2f);
                GUI.Label(title, $"{node.Type} #{node.Id}{edited}", _titleStyle);
                GUI.Label(detail, Detail(node, session), _detailStyle);
            }
            else
            {
                GUI.Label(rect, $"#{node.Id}{edited}", _titleStyle);
            }

            if (session.Asset.IsLocked(node.Id))
                GUI.Label(new Rect(rect.xMax - 18f, rect.y, 18f, 18f), EditorGUIUtility.IconContent("IN LockButton on"));

            if (session.IsDimmed(node.Id))
                EditorGUI.DrawRect(rect, MapGraphStyle.Dimmed);
        }

        // Second line of a node: whatever the current view mode is about.
        private static string Detail(MapNode node, MapGraphEditorSession session)
        {
            MapGraphAnalysis analysis = session.Analysis;
            return session.ViewMode switch
            {
                MapGraphViewMode.Intensity        => $"intensity {node.Intensity:0.00}",
                MapGraphViewMode.Faction          => node.HasFaction
                    ? $"{session.Asset.Settings?.FactionName(node.Faction) ?? node.Faction.ToString()} {node.FactionInfluence:0.00}"
                    : "no faction",
                MapGraphViewMode.RequiredOptional => !analysis.IsReachable(node.Id) ? "unreachable"
                    : analysis.IsRequired(node.Id) ? "required" : "optional",
                MapGraphViewMode.Branches         => analysis.BranchOf(node.Id) == MapGraphAnalysis.MainPathBranch
                    ? "main path" : $"branch {analysis.BranchOf(node.Id) + 1}",
                _                                 => $"depth {analysis.Depth(node.Id)}"
            };
        }

        private void DrawLegend(Rect local, MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;

            MapGraphStyle.CollectLegend(session.ViewMode, session.Asset.Settings, session.Analysis, _legend);

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
                BuildNodeMenu(menu, node, session);
            }
            else if (connection != null)
            {
                session.SelectConnection(connection);
                BuildConnectionMenu(menu, connection, session);
            }
            else
            {
                Vector2 world = ToWorld(mouse, session);
                foreach (MapNodeType type in Enum.GetValues(typeof(MapNodeType)))
                    menu.AddItem(new GUIContent($"Add Node/{type}"), false, () => session.AddNode(type, world));
            }

            menu.ShowAsContext();
            e.Use();
        }

        private static void BuildNodeMenu(GenericMenu menu, MapNode node, MapGraphEditorSession session)
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

        private static void BuildConnectionMenu(GenericMenu menu, MapConnection connection, MapGraphEditorSession session)
        {
            foreach (ConnectionType type in Enum.GetValues(typeof(ConnectionType)))
                menu.AddItem(new GUIContent($"Type/{type}"), connection.Type == type,
                             () => session.SetConnectionType(connection, type));

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Connection"), false, () => session.Disconnect(connection.A, connection.B));
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
                if (TryGetCurve(connection, session, out Curve curve) && curve.Distance(point) <= ConnectionHitRadius)
                    return connection;
            return null;
        }

        // --- Curves ----------------------------------------------------------------

        private bool TryGetCurve(MapConnection connection, MapGraphEditorSession session, out Curve curve)
        {
            curve = default;
            if (!session.Graph.TryGetNode(connection.A, out MapNode a)) return false;
            if (!session.Graph.TryGetNode(connection.B, out MapNode b)) return false;

            // Always run left → right so tangents point along the flow of the map.
            if (a.Position.x > b.Position.x) (a, b) = (b, a);

            Vector2 start = ToScreen(a.Position, session);
            Vector2 end   = ToScreen(b.Position, session);
            float   pull  = Mathf.Max(Mathf.Abs(end.x - start.x) * 0.5f, 30f * session.Zoom);

            // Shortcuts run between nodes on the same row; arc them over the nodes they skip.
            float arc = connection.Type == ConnectionType.Shortcut ? -Mathf.Abs(end.x - start.x) * 0.35f : 0f;

            curve = new Curve(start, end,
                              start + new Vector2(pull, arc),
                              end   + new Vector2(-pull, arc));
            return true;
        }

        private readonly struct Curve
        {
            private readonly Vector2 _start;
            private readonly Vector2 _end;
            private readonly Vector2 _startTangent;
            private readonly Vector2 _endTangent;

            public Curve(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent)
            {
                _start        = start;
                _end          = end;
                _startTangent = startTangent;
                _endTangent   = endTangent;
            }

            public Vector2 Midpoint =>
                0.125f * (_start + _end) + 0.375f * (_startTangent + _endTangent);

            public void Draw(Color color, float width) =>
                Handles.DrawBezier(_start, _end, _startTangent, _endTangent, color, null, width);

            public float Distance(Vector2 point) =>
                HandleUtility.DistancePointBezier(point, _start, _end, _startTangent, _endTangent);
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
        }
    }
}
