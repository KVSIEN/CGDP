using System.Collections.Generic;
using CGD.BehaviorTrees;
using CGD.Enemies;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // CGD › AI › Behavior Composer: an enemy type's behaviors as plain lists instead of a graph.
    // Each priority list (a Selector in its tree — "most urgent first", "fight behaviors") shows
    // its behaviors top to bottom in the order they are tried, each one switchable on and off,
    // movable, removable, and addable from the shared library (BehaviorLibrary). Branches still
    // written into the tree itself show up too, with Save as Behavior to make them reusable —
    // the way to turn the AITest agents' trees into building blocks for new enemy types.
    // Each row also says when the behavior acts (BehaviorTrigger) and which kit pieces it casts
    // from (KitNeed), warning when the chosen enemy's kit leaves one empty — the kit can be
    // filled in right here — plus the tree checks' problems inside each behavior, and a hint when
    // one sits below a behavior that takes every turn. In Play mode, selecting an agent marks
    // the behaviors it is running.
    // Everything here is an ordinary edit of the tree asset: the Behavior Tree Editor shows the
    // same tree, and either can be used at any time.
    internal sealed class BehaviorComposerWindow : EditorWindow
    {
        private const int ExpandedDepth = 1;   // deeper lists (choices inside one behavior) start folded
        private const int ShownProblems = 3;
        private static readonly Color RunningTint = new(1f, 0.8f, 0.2f, 0.18f);

        private EnemyData         _enemy;
        private SerializedObject  _enemyObject;   // for editing its kit in place
        private BehaviorTreeAsset _tree;
        private List<KitNeed>     _treeNeeds = new();
        private readonly BehaviorLibraryPanel _library = new();
        private readonly Dictionary<string, bool> _folded = new();
        private readonly Dictionary<string, List<BtIssue>> _rowProblems = new();   // by row node guid
        private int       _treeSharing;   // enemy types thinking with _tree
        private bool      _showKit;
        private Component _watchedOwner;  // the selected agent, in Play mode
        private Vector2   _scroll;

        [MenuItem("CGD/AI/Behavior Composer")]
        public static void OpenWindow() => Open(null, null);

        public static void Open(EnemyData enemy, BehaviorTreeAsset tree)
        {
            var window = GetWindow<BehaviorComposerWindow>("Behavior Composer");
            window.minSize = new Vector2(460f, 400f);
            if (enemy != null) window.UseEnemy(enemy);
            else if (tree != null) window.UseTree(tree);
            window.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += RefreshTreeFacts;
            BtEditorEvents.StructureEdited += OnTreeEdited;
            BtEditorEvents.AssetEdited     += OnTreeEdited;
            _library.Refresh();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RefreshTreeFacts;
            BtEditorEvents.StructureEdited -= OnTreeEdited;
            BtEditorEvents.AssetEdited     -= OnTreeEdited;
        }

        private void OnFocus()
        {
            _library.Refresh();
            UseTree(_tree);
        }

        private void OnTreeEdited(BehaviorTreeAsset _) => RefreshTreeFacts();

        private void RefreshTreeFacts()
        {
            _treeNeeds = KitNeed.Of(EnemyTreeContents.Nodes(_tree));
            RefreshProblems();
            Repaint();
        }

        // Keeps the "running now" marks moving while the game plays.
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && _watchedOwner != null) Repaint();
        }

        // Selecting an agent in Play mode watches it, and brings up its tree if this isn't it.
        private void OnSelectionChange()
        {
            GameObject selected = Selection.activeGameObject;
            var owner = selected != null ? selected.GetComponentInParent<IBehaviorTreeOwner>() as Component : null;
            if (owner == null) return;

            _watchedOwner = owner;
            BehaviorTree running = ((IBehaviorTreeOwner)owner).Tree;
            if (running != null && !running.Uses(_tree))
            {
                if (owner is EnemyAI agent && agent.Data != null && agent.Data.BehaviorTree == running.Asset) UseEnemy(agent.Data);
                else UseTree(running.Asset);
            }
            Repaint();
        }

        private void UseEnemy(EnemyData enemy)
        {
            _enemy       = enemy;
            _enemyObject = enemy != null ? new SerializedObject(enemy) : null;
            UseTree(enemy != null ? enemy.BehaviorTree : _tree);
            _showKit = enemy != null && KitNeed.Missing(enemy, _treeNeeds).Count > 0;
        }

        private void UseTree(BehaviorTreeAsset tree)
        {
            _tree        = tree;
            _treeSharing = EnemyAssetLibrary.CountUsers(tree, EnemyAssetLibrary.AllEnemyData());
            _treeNeeds   = KitNeed.Of(EnemyTreeContents.Nodes(tree));
            RefreshProblems();
        }

        // The tree checks' warnings and errors, gathered per row: those on the row's own nodes,
        // and for a behavior those inside its asset. Validating is too slow to redo every repaint.
        private void RefreshProblems()
        {
            _rowProblems.Clear();
            if (_tree == null) return;

            var byNode = new Dictionary<string, List<BtIssue>>();
            foreach (BtIssue issue in BtTreeValidator.Validate(_tree)) AddProblem(byNode, issue.NodeGuid, issue);

            var insideBehaviors = new Dictionary<BehaviorTreeAsset, List<BtIssue>>();
            foreach (BehaviorEditing.BehaviorList list in BehaviorEditing.Lists(_tree))
            foreach (string guid in list.Selector.ChildGuids)
            {
                BtNode row = _tree.FindNode(guid);
                if (row == null) continue;

                foreach (BtNode node in BehaviorEditing.Branch(_tree, row))
                    if (byNode.TryGetValue(node.Guid, out List<BtIssue> own))
                        foreach (BtIssue issue in own) AddProblem(_rowProblems, row.Guid, issue);

                if (row is not Subtree { Tree: not null } subtree) continue;
                if (!insideBehaviors.TryGetValue(subtree.Tree, out List<BtIssue> inside))
                    insideBehaviors[subtree.Tree] = inside = BtTreeValidator.Validate(subtree.Tree);
                foreach (BtIssue issue in inside) AddProblem(_rowProblems, row.Guid, issue);
            }
        }

        private static void AddProblem(Dictionary<string, List<BtIssue>> problems, string guid, BtIssue issue)
        {
            if (guid == null || issue.Severity == BtIssueSeverity.Info) return;
            if (!problems.TryGetValue(guid, out List<BtIssue> list)) problems[guid] = list = new List<BtIssue>();
            list.Add(issue);
        }

        // The selected agent's running tree, when it runs this one.
        private BehaviorTree WatchedTree()
        {
            if (!Application.isPlaying || _watchedOwner == null || _tree == null) return null;
            BehaviorTree tree = ((IBehaviorTreeOwner)_watchedOwner).Tree;
            return tree != null && tree.Uses(_tree) ? tree : null;
        }

        private void OnGUI()
        {
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_tree == null) DrawNoTree();
            else
            {
                EditorGUILayout.HelpBox(
                    "Each list is tried top to bottom: the first behavior whose checks (When) hold right now runs, and one that " +
                    "aborts lower priority (e.g. Regroup) interrupts those below it as soon as it can. A behavior that acts " +
                    "\"always\" leaves little or nothing below it a turn. Switched-off behaviors are skipped. Changes are ordinary tree edits " +
                    "(Undo works) and show in the Behavior Tree Editor.",
                    MessageType.None);
                DrawWatchStatus();
                List<BehaviorEditing.BehaviorList> lists = BehaviorEditing.Lists(_tree);
                if (lists.Count == 0)
                    EditorGUILayout.HelpBox("This tree has no Selector to hold behaviors. Open it in the Behavior Tree Editor, or start a new tree.", MessageType.Info);
                BehaviorTree watched = WatchedTree();
                foreach (BehaviorEditing.BehaviorList list in lists) DrawList(list, watched);
            }

            _library.Draw(_enemy);
            EditorGUILayout.EndScrollView();
        }

        // --- Header ------------------------------------------------------------------------

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                var enemy = (EnemyData)EditorGUILayout.ObjectField(
                    new GUIContent("Enemy", "Optional: the enemy type whose tree to compose"), _enemy, typeof(EnemyData), false);
                if (EditorGUI.EndChangeCheck()) UseEnemy(enemy);

                EditorGUI.BeginChangeCheck();
                var tree = (BehaviorTreeAsset)EditorGUILayout.ObjectField("Tree", _tree, typeof(BehaviorTreeAsset), false);
                if (EditorGUI.EndChangeCheck()) UseTree(tree);

                if (_enemy != null && _tree != null && _enemy.BehaviorTree != _tree)
                    EditorGUILayout.HelpBox($"{_enemy.name} thinks with {(_enemy.BehaviorTree != null ? _enemy.BehaviorTree.name : "no tree")}, not this one.", MessageType.Warning);

                if (_tree != null && _treeSharing > 1)
                    EditorGUILayout.HelpBox($"{_treeSharing} enemy types think with this tree: changes here change all of them. " +
                                            "Give one its own (Make Own Copy in its Enemy Data) to change it alone.", MessageType.Warning);

                if (_enemyObject != null && _enemyObject.targetObject != null) EnemyKitSection.Draw(_enemyObject, _treeNeeds, ref _showKit);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_tree == null))
                    {
                        if (GUILayout.Button("Open in Behavior Tree Editor")) BehaviorTreeWindow.Open(_tree);
                    }
                    if (GUILayout.Button("New Starter Tree…")) NewStarterTree();
                }
            }
        }

        private void DrawNoTree()
        {
            EditorGUILayout.HelpBox(
                "Pick an enemy or a tree to compose. A new starter tree is laid out like the AITest agents' trees " +
                "(regroup, raise alarm, fight while it knows the target, routine), ready for fight behaviors from the library.",
                MessageType.Info);
        }

        private void DrawWatchStatus()
        {
            if (!Application.isPlaying) return;
            string status = WatchedTree() != null
                ? $"Watching {_watchedOwner.name}: ▶ marks what it is doing now."
                : "Select an agent running this tree to see what it is doing.";
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        }

        private void NewStarterTree()
        {
            string suggested = _enemy != null ? EnemyAssetLibrary.EnemyName(_enemy) + "BehaviorTree" : "NewBehaviorTree";
            string path = EditorUtility.SaveFilePanelInProject("New Starter Tree", suggested, "asset",
                _enemy != null ? $"The new tree becomes {_enemy.name}'s." : "Where to save the new tree.", BehaviorLibrary.TreesFolder);
            if (string.IsNullOrEmpty(path)) return;

            UseTree(BehaviorActions.CreateStarterTree(path, _enemy));
            GUIUtility.ExitGUI();
        }

        // --- Lists ---------------------------------------------------------------------------

        private void DrawList(BehaviorEditing.BehaviorList list, BehaviorTree watched)
        {
            string key = list.Selector.Guid;
            bool folded = _folded.TryGetValue(key, out bool f) ? f : list.Depth > ExpandedDepth;

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(list.Depth * 14f);
                folded = !EditorGUILayout.Foldout(!folded, list.Title, true, EditorStyles.foldoutHeader);
            }
            _folded[key] = folded;
            if (folded) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(list.Depth * 14f + 12f);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    IReadOnlyList<string> children = list.Selector.ChildGuids;
                    string takesEveryTurn = null;   // the first behavior above with no checks
                    for (int i = 0; i < children.Count; i++)
                    {
                        BtNode child = _tree.FindNode(children[i]);
                        if (child == null) continue;

                        if (takesEveryTurn != null)
                            EditorGUILayout.LabelField($"↓ May never get a turn: “{takesEveryTurn}” above has no checks, so it acts " +
                                                       "whenever it can. Move this above it, or give that one a condition.",
                                                       EditorStyles.wordWrappedMiniLabel);
                        DrawRow(list.Selector, child, i, children.Count, watched);

                        bool on = child is not Subtree { IsOn: false };
                        if (takesEveryTurn == null && on && BehaviorTrigger.ActsAlways(_tree, child)) takesEveryTurn = Title(child);
                    }
                    DrawAddButton(list.Selector);
                }
            }
        }

        private void DrawRow(Selector list, BtNode node, int index, int count, BehaviorTree watched)
        {
            var subtree = node as Subtree;
            bool running = watched != null && watched.TryGetNode(_tree, node.Guid, out BtNode live) && live.IsRunning;

            using (var row = new EditorGUILayout.HorizontalScope())
            {
                if (running && Event.current.type == EventType.Repaint) EditorGUI.DrawRect(row.rect, RunningTint);
                GUILayout.Label(running ? "▶" : (index + 1).ToString(), EditorStyles.miniLabel, GUILayout.Width(18f));

                if (subtree != null)
                {
                    bool on = EditorGUILayout.Toggle(subtree.IsOn, GUILayout.Width(16f));
                    if (on != subtree.IsOn)
                    {
                        BehaviorActions.SetOn(_tree, subtree, on);
                        GUIUtility.ExitGUI();
                    }
                }
                else GUILayout.Space(20f);

                using (new EditorGUI.DisabledScope(subtree != null && !subtree.IsOn))
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(new GUIContent(Title(node), Tooltip(node)), EditorStyles.boldLabel);
                    string description = Description(node);
                    if (description.Length > 0) GUILayout.Label(description, EditorStyles.wordWrappedMiniLabel);
                    DrawFacts(node, subtree);
                    DrawProblems(node);
                }

                DrawRowButtons(list, node, subtree, index, count);
            }
        }

        private void DrawRowButtons(Selector list, BtNode node, Subtree subtree, int index, int count)
        {
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button(new GUIContent("▲", "Try it sooner"), EditorStyles.miniButtonLeft, GUILayout.Width(22f)))
                {
                    BehaviorActions.Move(_tree, list, index, index - 1);
                    GUIUtility.ExitGUI();
                }
            }
            using (new EditorGUI.DisabledScope(index == count - 1))
            {
                if (GUILayout.Button(new GUIContent("▼", "Try it later"), EditorStyles.miniButtonMid, GUILayout.Width(22f)))
                {
                    BehaviorActions.Move(_tree, list, index, index + 1);
                    GUIUtility.ExitGUI();
                }
            }

            if (subtree != null)
            {
                if (GUILayout.Button(new GUIContent("Open", "Edit this behavior in the Behavior Tree Editor (changes every tree using it)"),
                                     EditorStyles.miniButtonMid, GUILayout.Width(40f)) && subtree.Tree != null)
                    BehaviorTreeWindow.Open(subtree.Tree);

                int users = _library.Users(subtree.Tree);
                using (new EditorGUI.DisabledScope(subtree.Tree == null))
                {
                    if (GUILayout.Button(new GUIContent("Copy", users > 1
                            ? $"Used by {users} trees: copy it to change it for this tree alone"
                            : "Copy it to change it for this tree alone"), EditorStyles.miniButtonMid, GUILayout.Width(40f)))
                    {
                        BehaviorActions.MakeOwnCopy(_tree, subtree);
                        _library.Refresh();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Save as Behavior…", "Move this branch into its own behavior asset so other enemy types can use it; this tree keeps working the same"),
                                     EditorStyles.miniButtonMid, GUILayout.Width(118f)))
                {
                    if (BehaviorActions.SaveBranchAsBehavior(_tree, node)) _library.Refresh();
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button(new GUIContent("✕", subtree != null ? "Remove from this list" : "Delete this branch"),
                                 EditorStyles.miniButtonRight, GUILayout.Width(22f)))
            {
                BehaviorActions.Remove(_tree, node);
                GUIUtility.ExitGUI();
            }
        }

        // When it acts, and which kit pieces it casts from — flagged when the enemy lacks them.
        private void DrawFacts(BtNode node, Subtree subtree)
        {
            GUILayout.Label("When: " + BehaviorTrigger.Describe(_tree, node), EditorStyles.wordWrappedMiniLabel);

            List<BtNode> nodes = subtree != null ? EnemyTreeContents.Nodes(subtree.Tree) : EnemyTreeContents.Nodes(_tree, node);
            List<KitNeed> needs = KitNeed.Of(nodes);
            if (needs.Count == 0) return;

            GUILayout.Label("Needs kit: " + KitNeed.Names(needs), EditorStyles.wordWrappedMiniLabel);
            if (_enemy == null) return;

            List<KitNeed> missing = KitNeed.Missing(_enemy, needs);
            if (missing.Count > 0)
                EditorGUILayout.HelpBox($"{_enemy.name}'s {KitNeed.Names(missing)} {(missing.Count == 1 ? "is" : "are")} empty: " +
                                        "the parts using it never fire (fill it in under Kit above).", MessageType.Warning);
        }

        private void DrawProblems(BtNode node)
        {
            if (!_rowProblems.TryGetValue(node.Guid, out List<BtIssue> problems)) return;

            bool error = problems.Exists(issue => issue.Severity == BtIssueSeverity.Error);
            var lines = new List<string>();
            for (int i = 0; i < problems.Count && i < ShownProblems; i++) lines.Add(problems[i].Message);
            if (problems.Count > ShownProblems) lines.Add($"… and {problems.Count - ShownProblems} more (Open to see them on the nodes).");
            EditorGUILayout.HelpBox(string.Join("\n", lines), error ? MessageType.Error : MessageType.Warning);
        }

        private void DrawAddButton(Selector list)
        {
            Rect button = GUILayoutUtility.GetRect(new GUIContent("+ Add Behavior"), EditorStyles.miniButton, GUILayout.Width(120f));
            if (!GUI.Button(button, "+ Add Behavior", EditorStyles.miniButton)) return;

            var present = new HashSet<BehaviorTreeAsset>();
            foreach (string guid in list.ChildGuids)
                if (_tree.FindNode(guid) is Subtree subtree && subtree.Tree != null) present.Add(subtree.Tree);

            var choices = _library.Entries.FindAll(entry => entry.Asset != _tree);   // a tree can't run itself
            int index = InsertIndex(list);
            new BehaviorPicker(choices, present, _enemy,
                behavior => BehaviorActions.Add(_tree, list, behavior, index),
                () => AddNewBehavior(list, index)).Show(button);
        }

        // New behaviors go above the first one that takes every turn (a routine at the bottom of
        // the top list, a catch-all attack) — appended below it they would never run.
        private int InsertIndex(Selector list)
        {
            IReadOnlyList<string> children = list.ChildGuids;
            for (int i = 0; i < children.Count; i++)
            {
                BtNode child = _tree.FindNode(children[i]);
                if (child != null && child is not Subtree { IsOn: false } && BehaviorTrigger.ActsAlways(_tree, child)) return i;
            }
            return -1;
        }

        private void AddNewBehavior(Selector list, int index)
        {
            BehaviorTreeAsset behavior = BehaviorActions.CreateEmptyBehavior(_tree, list, index);
            if (behavior == null) return;

            _library.Refresh();
            BehaviorTreeWindow.Open(behavior);
        }

        // --- Text ----------------------------------------------------------------------------

        private static string Title(BtNode node) => node switch
        {
            Subtree { Tree: not null } subtree => ObjectNames.NicifyVariableName(subtree.Tree.name),
            Subtree                            => "(behavior with no tree)",
            _                                  => $"{BtNodeTypes.DisplayName(node.GetType())}  (written into this tree)",
        };

        private static string Description(BtNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.Comment)) return node.Comment;
            return node is Subtree subtree ? BehaviorLibrary.Description(subtree.Tree) : "";
        }

        private static string Tooltip(BtNode node) => node is Subtree { Tree: not null } subtree
            ? AssetDatabase.GetAssetPath(subtree.Tree)
            : "A branch kept inside this tree. Save as Behavior to reuse it in other enemy types.";
    }
}
