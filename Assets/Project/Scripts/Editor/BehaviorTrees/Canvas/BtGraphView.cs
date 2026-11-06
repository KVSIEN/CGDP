using System;
using System.Collections.Generic;
using System.Linq;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace CGD.Editor
{
    // The behavior tree canvas. Every edit goes straight into the asset with an Undo step:
    //   Space / right-click › Create Node   searchable node menu (Space is GraphView's own shortcut)
    //   drag from a node's bottom port      connect a child (children run left to right)
    //   Delete, Ctrl+C / Ctrl+V / Ctrl+D    the usual
    //   right-click › Group Selection / Add Sticky Note   frames and notes for readers
    //   middle-drag / alt-drag, scroll      pan, zoom      A  frame all
    internal sealed class BtGraphView : GraphView
    {
        private const float PasteOffset = 40f;

        private readonly EditorWindow _window;
        private readonly BtNodeSearchProvider _search;
        private readonly Dictionary<string, BtNodeView> _views = new();
        private BehaviorTreeAsset _asset;
        // Set while views are being built, so GraphView's group callbacks don't write back.
        private bool _building;

        public BtGraphView(EditorWindow window)
        {
            _window = window;
            style.flexGrow = 1f;

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            _search = ScriptableObject.CreateInstance<BtNodeSearchProvider>();
            _search.hideFlags = HideFlags.HideAndDontSave;
            _search.Initialize((type, screenPosition) => CreateNode(type, ScreenToGraph(screenPosition)));
            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _search);

            graphViewChanged       = OnGraphViewChanged;
            serializeGraphElements = SerializeSelection;
            canPasteSerializedData = data => _asset != null && !string.IsNullOrEmpty(data);
            unserializeAndPaste    = (_, data) => Paste(data);
            elementsAddedToGroup     = OnAddedToGroup;
            elementsRemovedFromGroup = OnRemovedFromGroup;
            groupTitleChanged        = OnGroupTitleChanged;
        }

        // The node selected on its own (null for none or several), for the inspector.
        public event Action<BtNode> NodeSelected;

        public IEnumerable<BtNodeView> NodeViews => _views.Values;

        // A Subtree node asked to open the tree it runs.
        public event Action<BehaviorTreeAsset> OpenRequested;
        // The asset changed through the canvas (add, delete, connect, move, group, note).
        public event Action Edited;

        public void Dispose() => UnityEngine.Object.DestroyImmediate(_search);

        // --- Building the view -----------------------------------------------------

        public void Show(BehaviorTreeAsset asset)
        {
            _asset = asset;
            Rebuild();
        }

        public void Rebuild()
        {
            graphViewChanged = null;
            DeleteElements(graphElements.ToList());
            _views.Clear();
            graphViewChanged = OnGraphViewChanged;

            if (_asset == null) return;

            _building = true;
            foreach (BtNode node in _asset.Nodes)
                if (node != null) AddView(node);
            RebuildEdges();
            foreach (BtGroup group in _asset.Groups) AddGroupView(group);
            foreach (BtNote note in _asset.Notes) AddNoteView(note);
            RefreshBreakpoints();
            _building = false;
            NodeSelected?.Invoke(null);
        }

        public void ShowIssues(List<BtIssue> issues)
        {
            var byNode = new Dictionary<string, List<BtIssue>>();
            foreach (BtIssue issue in issues)
            {
                if (issue.NodeGuid == null) continue;
                if (!byNode.TryGetValue(issue.NodeGuid, out var list)) byNode[issue.NodeGuid] = list = new List<BtIssue>();
                list.Add(issue);
            }
            foreach (var pair in _views)
                pair.Value.ShowIssues(byNode.TryGetValue(pair.Key, out var list) ? list : null);
        }

        public void RefreshBreakpoints()
        {
            foreach (var pair in _views) pair.Value.ShowBreakpoint(BtBreakpoints.Get(pair.Key));
        }

        public void RefreshAllNodes()
        {
            foreach (BtNodeView view in _views.Values) view.Refresh();
        }

        // Selects and frames a node (from the issue list, the trace or a breakpoint).
        public void SelectNode(string guid)
        {
            if (guid == null || !_views.TryGetValue(guid, out BtNodeView view)) return;
            ClearSelection();
            AddToSelection(view);
            FrameSelection();
        }

        public void RefreshNode(BtNode node)
        {
            if (node == null || !_views.TryGetValue(node.Guid, out BtNodeView view)) return;

            // If applying the edit replaced the node object, the views are stale: start over.
            if (!ReferenceEquals(view.Data, node)) Rebuild();
            else view.Refresh();
        }

        private void AddGroupView(BtGroup data)
        {
            var view = new BtGroupView(data);
            AddElement(view);
            foreach (string guid in data.Nodes)
                if (_views.TryGetValue(guid, out BtNodeView member)) view.AddElement(member);
        }

        private void AddNoteView(BtNote data)
        {
            var view = new BtNoteView(data);
            view.RegisterCallback<StickyNoteChangeEvent>(_ => SaveNote(view));
            AddElement(view);
        }

        private BtNodeView AddView(BtNode node)
        {
            var view = new BtNodeView(node);
            view.Selected += OnViewSelected;
            view.OpenRequested += asset => OpenRequested?.Invoke(asset);
            _views[node.Guid] = view;
            AddElement(view);
            return view;
        }

        // Edges always mirror the asset, which also tidies up after GraphView's own edge juggling.
        private void RebuildEdges()
        {
            graphViewChanged = null;
            DeleteElements(edges.ToList());
            graphViewChanged = OnGraphViewChanged;

            foreach (BtNodeView view in _views.Values)
            {
                view.SetOrder(-1);
                if (view.Data is not BtParentNode parent) continue;

                IReadOnlyList<string> children = parent.ChildGuids;
                for (int i = 0; i < children.Count; i++)
                {
                    if (!_views.TryGetValue(children[i], out BtNodeView child) || child.Input == null) continue;
                    AddElement(view.Output.ConnectTo(child.Input));
                    if (children.Count > 1) child.SetOrder(i);
                }
            }
        }

        // --- Editing ---------------------------------------------------------------

        public void CreateNode(Type type, Vector2 position)
        {
            if (_asset == null) return;

            Record("Create Node");
            var node = (BtNode)Activator.CreateInstance(type);
            node.Position = position;
            _asset.AddNode(node);
            Commit();

            BtNodeView view = AddView(node);
            ClearSelection();
            AddToSelection(view);
        }

        public void AutoLayout()
        {
            if (_asset == null) return;

            Record("Auto Layout");
            BtTreeLayout.Apply(_asset);
            Commit();
            Rebuild();
            schedule.Execute(() => FrameAll());
        }

        // A frame around the selected nodes.
        public void GroupSelection(string title = "Group")
        {
            var members = selection.OfType<BtNodeView>().Select(v => v.Data.Guid).ToList();
            if (_asset == null || members.Count == 0) return;

            Record("Group");
            foreach (BtGroup other in _asset.Groups) other.Nodes.RemoveAll(members.Contains);
            _asset.Groups.Add(new BtGroup { Title = title, Nodes = members });
            Commit();
            Rebuild();
        }

        public void AddNote(Vector2 position)
        {
            if (_asset == null) return;

            Record("Add Note");
            _asset.Notes.Add(new BtNote { Text = "", Position = new Rect(position, new Vector2(200f, 120f)) });
            Commit();
            Rebuild();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            foreach (Port port in ports.ToList())
            {
                if (port.direction == startPort.direction || port.node == startPort.node) continue;

                var parent = (BtNodeView)(startPort.direction == Direction.Output ? startPort.node : port.node);
                var child  = (BtNodeView)(startPort.direction == Direction.Output ? port.node : startPort.node);
                if (!IsAncestor(child.Data, parent.Data)) compatible.Add(port);
            }
            return compatible;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_asset == null) return change;

            bool linksChanged = false;

            if (change.elementsToRemove != null && change.elementsToRemove.Count > 0)
            {
                Record("Delete");
                foreach (GraphElement element in change.elementsToRemove)
                {
                    switch (element)
                    {
                        case Edge edge:           Unlink(edge); break;
                        case BtNodeView view:     RemoveNode(view); break;
                        case BtGroupView group:   _asset.Groups.Remove(group.Data); break;
                        case BtNoteView note:     _asset.Notes.Remove(note.Data); break;
                    }
                }
                linksChanged = true;
            }

            if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
            {
                Record("Connect");
                foreach (Edge edge in change.edgesToCreate) Link(edge);
                linksChanged = true;
            }

            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                Record("Move");
                foreach (GraphElement element in change.movedElements)
                {
                    if (element is BtNoteView note) note.Data.Position = note.GetPosition();
                    if (element is BtGroupView group) group.Data.Position = group.GetPosition();
                }
                // Moving a group moves its nodes too, so read every node back.
                foreach (BtNodeView view in _views.Values) view.Data.Position = view.GetPosition().position;
                SortAllChildren();
                linksChanged = true;
            }

            Commit();
            if (linksChanged) schedule.Execute(RebuildEdges);
            return change;
        }

        private void Link(Edge edge)
        {
            var parent = ((BtNodeView)edge.output.node).Data as BtParentNode;
            var child  = ((BtNodeView)edge.input.node).Data;
            if (parent == null) return;

            // One parent per node, and a single-child node swaps its child.
            _asset.FindParent(child)?.RemoveChild(child.Guid);
            if (parent.MaxChildren == 1) parent.SetChildOrder(new List<string>());
            parent.AddChild(child.Guid);
            SortChildren(parent);
        }

        private static void Unlink(Edge edge)
        {
            if (edge.output?.node is BtNodeView parentView && parentView.Data is BtParentNode parent &&
                edge.input?.node is BtNodeView childView)
                parent.RemoveChild(childView.Data.Guid);
        }

        private void RemoveNode(BtNodeView view)
        {
            _asset.RemoveNode(view.Data);
            _views.Remove(view.Data.Guid);
            foreach (BtGroup group in _asset.Groups) group.Nodes.Remove(view.Data.Guid);
        }

        // --- Groups and notes ----------------------------------------------------------

        private void OnAddedToGroup(Group group, IEnumerable<GraphElement> elements)
        {
            if (_building || group is not BtGroupView view) return;

            Record("Add To Group");
            foreach (BtNodeView node in elements.OfType<BtNodeView>())
            {
                foreach (BtGroup other in _asset.Groups) other.Nodes.Remove(node.Data.Guid);
                view.Data.Nodes.Add(node.Data.Guid);
            }
            Commit();
        }

        private void OnRemovedFromGroup(Group group, IEnumerable<GraphElement> elements)
        {
            if (_building || group is not BtGroupView view) return;

            Record("Remove From Group");
            foreach (BtNodeView node in elements.OfType<BtNodeView>()) view.Data.Nodes.Remove(node.Data.Guid);
            Commit();
        }

        private void OnGroupTitleChanged(Group group, string title)
        {
            if (_building || group is not BtGroupView view) return;

            Record("Rename Group");
            view.Data.Title = title;
            Commit();
        }

        private void SaveNote(BtNoteView view)
        {
            if (_building) return;

            Record("Edit Note");
            view.Data.Title    = view.title;
            view.Data.Text     = view.contents;
            view.Data.Position = view.GetPosition();
            Commit();
        }

        // Children run in left-to-right order on the canvas, as in most behavior tree editors.
        private void SortAllChildren()
        {
            foreach (BtNode node in _asset.Nodes)
                if (node is BtParentNode parent) SortChildren(parent);
        }

        private void SortChildren(BtParentNode parent)
        {
            var ordered = new List<string>(parent.ChildGuids);
            ordered.Sort((a, b) => PositionOf(a).CompareTo(PositionOf(b)));
            parent.SetChildOrder(ordered);
        }

        private float PositionOf(string guid) => _asset.FindNode(guid)?.Position.x ?? 0f;

        private bool IsAncestor(BtNode candidate, BtNode node)
        {
            for (BtNode current = node; current != null; current = _asset.FindParent(current))
                if (current == candidate) return true;
            return false;
        }

        // --- Copy / paste ----------------------------------------------------------

        private static string SerializeSelection(IEnumerable<GraphElement> elements) =>
            string.Join(",", elements.OfType<BtNodeView>().Where(v => v.Data is not BtRoot).Select(v => v.Data.Guid));

        // Duplicates the copied nodes, keeping the links between them.
        private void Paste(string data)
        {
            var copies = new Dictionary<string, BtNode>();
            foreach (string guid in data.Split(','))
            {
                BtNode original = _asset.FindNode(guid);
                if (original != null && original is not BtRoot) copies[guid] = original.Duplicate();
            }
            if (copies.Count == 0) return;

            Record("Paste");
            foreach (BtNode copy in copies.Values)
            {
                copy.Position += new Vector2(PasteOffset, PasteOffset);
                _asset.AddNode(copy);
            }

            foreach (var pair in copies)
            {
                if (_asset.FindNode(pair.Key) is not BtParentNode original) continue;
                foreach (string childGuid in original.ChildGuids)
                    if (copies.TryGetValue(childGuid, out BtNode childCopy))
                        ((BtParentNode)pair.Value).AddChild(childCopy.Guid);
            }
            Commit();

            ClearSelection();
            foreach (BtNode copy in copies.Values) AddToSelection(AddView(copy));
            RebuildEdges();
        }

        // --- Selection, input, helpers ---------------------------------------------

        private void OnViewSelected(BtNodeView _) => schedule.Execute(NotifySelection);

        public override void ClearSelection()
        {
            base.ClearSelection();
            NodeSelected?.Invoke(null);
        }

        private void NotifySelection()
        {
            var selectedNodes = selection.OfType<BtNodeView>().ToList();
            NodeSelected?.Invoke(selectedNodes.Count == 1 ? selectedNodes[0].Data : null);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView)
            {
                Vector2 screen = _window.position.position + evt.mousePosition;
                evt.menu.AppendAction("Create Node", _ =>
                    SearchWindow.Open(new SearchWindowContext(screen), _search));
                Vector2 notePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
                evt.menu.AppendAction("Group Selection", _ => GroupSelection(),
                    _ => selection.OfType<BtNodeView>().Any() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Add Sticky Note", _ => AddNote(notePosition));
                evt.menu.AppendAction("Auto Layout", _ => AutoLayout());
                evt.menu.AppendAction("Frame All", _ => FrameAll());
                evt.menu.AppendSeparator();
            }
            base.BuildContextualMenu(evt);
        }

        // Screen point (search window, context menu) to canvas coordinates, through pan and zoom.
        private Vector2 ScreenToGraph(Vector2 screenPosition)
        {
            Vector2 windowPosition = screenPosition - _window.position.position;
            return _window.rootVisualElement.ChangeCoordinatesTo(contentViewContainer, windowPosition);
        }

        private void Record(string action) => Undo.RecordObject(_asset, $"Behavior Tree: {action}");

        private void Commit()
        {
            EditorUtility.SetDirty(_asset);
            Edited?.Invoke();
        }
    }
}
