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
    // Everything here is an ordinary edit of the tree asset: the Behavior Tree Editor shows the
    // same tree, and either can be used at any time.
    internal sealed class BehaviorComposerWindow : EditorWindow
    {
        private const int ExpandedDepth = 1;   // deeper lists (choices inside one behavior) start folded

        private EnemyData         _enemy;
        private BehaviorTreeAsset _tree;
        private List<BehaviorLibrary.Entry> _library = new();
        private readonly Dictionary<BehaviorTreeAsset, int> _usage = new();
        private readonly Dictionary<string, bool> _folded = new();
        private int     _treeSharing;   // enemy types thinking with _tree
        private bool    _showLibrary;
        private Vector2 _scroll;

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
            Undo.undoRedoPerformed += Repaint;
            BtEditorEvents.StructureEdited += OnTreeEdited;
            BtEditorEvents.AssetEdited     += OnTreeEdited;
            RefreshLibrary();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            BtEditorEvents.StructureEdited -= OnTreeEdited;
            BtEditorEvents.AssetEdited     -= OnTreeEdited;
        }

        private void OnFocus()
        {
            RefreshLibrary();
            UseTree(_tree);
        }

        private void OnTreeEdited(BehaviorTreeAsset _) => Repaint();

        private void RefreshLibrary()
        {
            _library = BehaviorLibrary.Entries();
            _usage.Clear();
            foreach (BehaviorLibrary.Entry entry in _library)
                _usage[entry.Asset] = BehaviorLibrary.TreesUsing(entry.Asset).Count;
        }

        private void UseEnemy(EnemyData enemy)
        {
            _enemy = enemy;
            UseTree(enemy != null ? enemy.BehaviorTree : _tree);
        }

        private void UseTree(BehaviorTreeAsset tree)
        {
            _tree        = tree;
            _treeSharing = EnemyAssetLibrary.CountUsers(tree, EnemyAssetLibrary.AllEnemyData());
        }

        private void OnGUI()
        {
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_tree == null) DrawNoTree();
            else
            {
                EditorGUILayout.HelpBox(
                    "Each list is tried top to bottom: the first behavior that can act right now runs, and one that " +
                    "aborts lower priority (e.g. Regroup) interrupts those below it as soon as it can. Switched-off " +
                    "behaviors are skipped. Changes are ordinary tree edits (Undo works) and show in the Behavior Tree Editor.",
                    MessageType.None);
                List<BehaviorEditing.BehaviorList> lists = BehaviorEditing.Lists(_tree);
                if (lists.Count == 0)
                    EditorGUILayout.HelpBox("This tree has no Selector to hold behaviors. Open it in the Behavior Tree Editor, or start a new tree.", MessageType.Info);
                foreach (BehaviorEditing.BehaviorList list in lists) DrawList(list);
            }

            DrawLibrary();
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

        private void DrawList(BehaviorEditing.BehaviorList list)
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
                    for (int i = 0; i < children.Count; i++)
                    {
                        BtNode child = _tree.FindNode(children[i]);
                        if (child != null) DrawRow(list.Selector, child, i, children.Count);
                    }
                    DrawAddButton(list.Selector);
                }
            }
        }

        private void DrawRow(Selector list, BtNode node, int index, int count)
        {
            var subtree = node as Subtree;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label((index + 1).ToString(), EditorStyles.miniLabel, GUILayout.Width(18f));

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

                int users = subtree.Tree != null && _usage.TryGetValue(subtree.Tree, out int n) ? n : 0;
                using (new EditorGUI.DisabledScope(subtree.Tree == null))
                {
                    if (GUILayout.Button(new GUIContent("Copy", users > 1
                            ? $"Used by {users} trees: copy it to change it for this tree alone"
                            : "Copy it to change it for this tree alone"), EditorStyles.miniButtonMid, GUILayout.Width(40f)))
                    {
                        BehaviorActions.MakeOwnCopy(_tree, subtree);
                        RefreshLibrary();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Save as Behavior…", "Move this branch into its own behavior asset so other enemy types can use it; this tree keeps working the same"),
                                     EditorStyles.miniButtonMid, GUILayout.Width(118f)))
                {
                    if (BehaviorActions.SaveBranchAsBehavior(_tree, node)) RefreshLibrary();
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

        private void DrawAddButton(Selector list)
        {
            if (!GUILayout.Button("+ Add Behavior", EditorStyles.miniButton, GUILayout.Width(120f))) return;

            var present = new HashSet<BehaviorTreeAsset>();
            foreach (string guid in list.ChildGuids)
                if (_tree.FindNode(guid) is Subtree subtree && subtree.Tree != null) present.Add(subtree.Tree);

            var menu = new GenericMenu();
            if (_library.Count == 0) menu.AddDisabledItem(new GUIContent("The library is empty — Save as Behavior to fill it"));
            foreach (BehaviorLibrary.Entry entry in _library)
            {
                if (entry.Asset == _tree) continue;   // a tree can't run itself
                var content = new GUIContent(entry.MenuPath);
                if (present.Contains(entry.Asset)) menu.AddDisabledItem(content);
                else
                {
                    BehaviorTreeAsset behavior = entry.Asset;
                    menu.AddItem(content, false, () => BehaviorActions.Add(_tree, list, behavior));
                }
            }
            menu.ShowAsContext();
        }

        // --- Library -------------------------------------------------------------------------

        private void DrawLibrary()
        {
            EditorGUILayout.Space(10f);
            _showLibrary = EditorGUILayout.Foldout(_showLibrary, $"Behavior library ({_library.Count})", true, EditorStyles.foldoutHeader);
            if (!_showLibrary) return;

            EditorGUILayout.LabelField(
                $"Every tree in {BehaviorLibrary.BehaviorsFolder.Replace("Assets/Project/Data/", "")} (sub-folders are categories) and Shared/. " +
                "A behavior's description is its Root's comment.", EditorStyles.wordWrappedMiniLabel);

            string category = null;
            foreach (BehaviorLibrary.Entry entry in _library)
            {
                if (entry.Category != category)
                {
                    category = entry.Category;
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(category) ? "Uncategorised" : category, EditorStyles.miniBoldLabel);
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        if (GUILayout.Button(entry.Asset.name, EditorStyles.linkLabel)) BehaviorTreeWindow.Open(entry.Asset);
                        if (entry.Description.Length > 0) GUILayout.Label(entry.Description, EditorStyles.wordWrappedMiniLabel);
                    }
                    int users = _usage.TryGetValue(entry.Asset, out int n) ? n : 0;
                    GUILayout.Label($"in {users} tree{(users == 1 ? "" : "s")}", EditorStyles.miniLabel, GUILayout.Width(60f));
                }
            }
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
