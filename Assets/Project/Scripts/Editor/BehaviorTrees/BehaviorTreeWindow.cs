using System.Collections.Generic;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CGD.Editor
{
    // Behavior tree editor (CGD › AI › Behavior Tree Editor, or double-click a tree asset):
    // the canvas, the selected node's settings, and in Play mode a live view of the tree
    // running on the selected agent — each node's border shows what it last reported
    // (yellow running, green success, red failure; faded when not ticked this frame) — also
    // inside a subtree it uses (double-click a Subtree node to open it) — with its decision
    // trace and breakpoints. The tree is validated after every edit; problems show as badges
    // on the nodes and in the toolbar's issue button.
    public sealed class BehaviorTreeWindow : EditorWindow
    {
        private const long DebugRefreshMs = 100;

        [SerializeField] private BehaviorTreeAsset _asset;

        private BtGraphView       _graph;
        private BtInspectorPanel  _inspector;
        private ObjectField       _assetField;
        private Label             _status;
        private Button            _issuesButton;
        private Component         _watchedOwner;
        private BehaviorTree      _traced;
        private bool              _showingStatus;
        private List<BtIssue>     _issues = new();
        private int               _nextIssue;
        private string            _pausedAt;   // set by a breakpoint until the game resumes

        [MenuItem("CGD/AI/Behavior Tree Editor")]
        public static void OpenWindow() => GetWindow<BehaviorTreeWindow>().Show();

        public static void Open(BehaviorTreeAsset asset)
        {
            var window = GetWindow<BehaviorTreeWindow>();
            window.Load(asset);
            window.Show();
        }

        // Double-clicking a tree asset opens it here (the asset is selected by then).
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is not BehaviorTreeAsset asset) return false;
            Open(asset);
            return true;
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed      += OnUndoRedo;
            BtEditorEvents.AssetEdited  += OnAssetEdited;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed      -= OnUndoRedo;
            BtEditorEvents.AssetEdited  -= OnAssetEdited;
            Trace(null);
            _graph?.Dispose();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(BuildToolbar());

            var split = new TwoPaneSplitView(1, 300f, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1f;
            _graph     = new BtGraphView(this);
            _inspector = new BtInspectorPanel();
            split.Add(_graph);
            split.Add(_inspector);
            rootVisualElement.Add(split);

            _graph.NodeSelected    += _inspector.SetNode;
            _graph.OpenRequested   += Load;
            _graph.Edited          += Validate;
            _inspector.NodeChanged += node => { _graph.RefreshNode(node); Validate(); };
            _inspector.SelectRequested    += _graph.SelectNode;
            _inspector.BreakpointsChanged += _graph.RefreshBreakpoints;

            rootVisualElement.schedule.Execute(UpdateLiveView).Every(DebugRefreshMs);
            Load(_asset);
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();

            _assetField = new ObjectField { objectType = typeof(BehaviorTreeAsset), allowSceneObjects = false };
            _assetField.style.width = 240f;
            _assetField.RegisterValueChangedCallback(evt => Load(evt.newValue as BehaviorTreeAsset));
            toolbar.Add(_assetField);

            toolbar.Add(new ToolbarButton(() => _graph.AutoLayout()) { text = "Auto Layout" });
            toolbar.Add(new ToolbarButton(() => _graph.FrameAll())   { text = "Frame All" });
            toolbar.Add(new ToolbarButton(SelectAsset)               { text = "Select Asset" });
            _issuesButton = new ToolbarButton(SelectNextIssue) { tooltip = "Jump to the next node with an issue" };
            toolbar.Add(_issuesButton);
            toolbar.Add(new ToolbarSpacer { flex = true });

            _status = new Label { style = { unityTextAlign = TextAnchor.MiddleRight, marginRight = 8f } };
            toolbar.Add(_status);

            AddLegend(toolbar, "Composite", BtNodeCategory.Composite);
            AddLegend(toolbar, "Decorator", BtNodeCategory.Decorator);
            AddLegend(toolbar, "Subtree",   BtNodeCategory.Subtree);
            AddLegend(toolbar, "Condition", BtNodeCategory.Condition);
            AddLegend(toolbar, "Action",    BtNodeCategory.Action);
            return toolbar;
        }

        private static void AddLegend(Toolbar toolbar, string text, BtNodeCategory category)
        {
            var label = new Label(text)
            {
                style =
                {
                    backgroundColor = BtNodeTypes.Color(category),
                    color = Color.white,
                    paddingLeft = 6f, paddingRight = 6f, marginLeft = 2f,
                    unityTextAlign = TextAnchor.MiddleCenter,
                },
            };
            toolbar.Add(label);
        }

        private void Load(BehaviorTreeAsset asset)
        {
            _asset = asset;
            titleContent = new GUIContent(asset != null ? asset.name : "Behavior Tree");
            if (_graph == null) return;

            if (asset != null && asset.Root == null)
            {
                Undo.RecordObject(asset, "Behavior Tree: Add Root");
                asset.EnsureRoot();
                EditorUtility.SetDirty(asset);
            }

            _assetField.SetValueWithoutNotify(asset);
            _inspector.SetAsset(asset);
            _graph.Show(asset);
            _showingStatus = true;
            Validate();
            _graph.schedule.Execute(() => _graph.FrameAll()).ExecuteLater(50);
        }

        private void SelectAsset()
        {
            if (_asset == null) return;
            Selection.activeObject = _asset;
            EditorGUIUtility.PingObject(_asset);
        }

        private void OnUndoRedo()
        {
            if (_graph == null) return;
            _graph.Rebuild();
            _inspector.SetNode(null);
            Validate();
        }

        private void OnAssetEdited(BehaviorTreeAsset asset)
        {
            if (asset != _asset || _graph == null) return;
            _graph.RefreshAllNodes();
            Validate();
        }

        // --- Validation -------------------------------------------------------------------

        private void Validate()
        {
            if (_graph == null) return;

            _issues = BtTreeValidator.Validate(_asset);
            _nextIssue = 0;
            _graph.ShowIssues(_issues);
            _inspector.SetIssues(_issues);

            int problems = 0;
            foreach (BtIssue issue in _issues) if (issue.Severity != BtIssueSeverity.Info) problems++;
            _issuesButton.text = _issues.Count == 0 ? "✓ No issues" : $"⚠ {problems} issue{(problems == 1 ? "" : "s")} (+{_issues.Count - problems} notes)";
        }

        private void SelectNextIssue()
        {
            for (int tried = 0; tried < _issues.Count; tried++)
            {
                BtIssue issue = _issues[_nextIssue++ % _issues.Count];
                if (issue.NodeGuid == null) continue;
                _graph.SelectNode(issue.NodeGuid);
                return;
            }
        }

        // Selecting a tree asset opens it; selecting an agent watches it (and opens its tree).
        private void OnSelectionChange()
        {
            if (Selection.activeObject is BehaviorTreeAsset asset)
            {
                if (asset != _asset) Load(asset);
                return;
            }

            GameObject selected = Selection.activeGameObject;
            if (selected == null) return;

            var owner = selected.GetComponentInParent<IBehaviorTreeOwner>() as Component;
            if (owner == null) return;

            _watchedOwner = owner;
            BehaviorTree tree = ((IBehaviorTreeOwner)owner).Tree;
            if (tree != null && !tree.Uses(_asset)) Load(tree.Asset);
        }

        private void UpdateLiveView()
        {
            if (_graph == null) return;

            BehaviorTree tree = WatchedTree();
            Trace(tree);
            if (tree == null)
            {
                _status.text = Application.isPlaying && _asset != null ? "Select an agent running this tree to watch it" : string.Empty;
                _inspector.SetWatched(null);
                ClearStatuses();
                return;
            }

            if (!EditorApplication.isPaused) _pausedAt = null;
            _status.text   = _pausedAt ?? $"Watching {_watchedOwner.name}";
            _showingStatus = true;
            int tick = tree.Context.TickCount;
            foreach (BtNodeView view in _graph.NodeViews)
            {
                if (tree.TryGetNode(_asset, view.Data.Guid, out BtNode node) && node.LastTick >= 0)
                    view.ShowStatus(node.LastStatus, node.IsRunning || node.LastTick == tick);
                else
                    view.ClearStatus();
            }
            _inspector.SetWatched(tree);
        }

        private BehaviorTree WatchedTree()
        {
            if (!Application.isPlaying || _watchedOwner == null) return null;
            BehaviorTree tree = ((IBehaviorTreeOwner)_watchedOwner).Tree;
            return tree != null && tree.Uses(_asset) ? tree : null;
        }

        // --- Trace and breakpoints ---------------------------------------------------------

        // Records decisions for the watched agent only, and stops recording for the previous one.
        private void Trace(BehaviorTree tree)
        {
            if (tree == _traced) return;

            if (_traced?.Trace != null)
            {
                _traced.Trace.Recorded -= OnTraceEntry;
                _traced.StopTrace();
            }
            _traced = tree;
            if (tree != null) tree.StartTrace().Recorded += OnTraceEntry;
        }

        private void OnTraceEntry(BtTraceEntry entry)
        {
            BtBreakOn breakOn = BtBreakpoints.Get(entry.Node.Guid);
            bool hit = (breakOn.HasFlag(BtBreakOn.Start) && entry.Event == BtTraceEvent.Started) ||
                       (breakOn.HasFlag(BtBreakOn.Failure) && entry.Event == BtTraceEvent.Failed);
            if (!hit) return;

            EditorApplication.isPaused = true;
            _pausedAt = $"Paused: {BtNodeTypes.DisplayName(entry.Node.GetType())} {(entry.Event == BtTraceEvent.Started ? "started" : "failed")}";
            _status.text = _pausedAt;
            if (_asset != null && _asset.FindNode(entry.Node.Guid) != null) _graph.SelectNode(entry.Node.Guid);
        }

        private void ClearStatuses()
        {
            if (!_showingStatus) return;
            foreach (BtNodeView view in _graph.NodeViews) view.ClearStatus();
            _showingStatus = false;
        }
    }
}
