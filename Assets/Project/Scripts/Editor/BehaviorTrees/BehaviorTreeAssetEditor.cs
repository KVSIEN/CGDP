using System.Collections.Generic;
using CGD.BehaviorTrees;
using CGD.Enemies;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Trees are edited in the graph window, not field by field. The inspector says which enemy
    // types think with the tree, so it is clear whom an edit affects.
    [CustomEditor(typeof(BehaviorTreeAsset))]
    public class BehaviorTreeAssetEditor : UnityEditor.Editor
    {
        private readonly List<EnemyData> _users = new();
        private List<BehaviorTreeAsset> _trees;   // trees that run this one as a behavior (Subtree)

        private void OnEnable()
        {
            _users.Clear();
            foreach (EnemyData data in EnemyAssetLibrary.AllEnemyData())
                if (data.BehaviorTree == target) _users.Add(data);
            _trees = BehaviorLibrary.TreesUsing((BehaviorTreeAsset)target);
        }

        public override void OnInspectorGUI()
        {
            var asset = (BehaviorTreeAsset)target;

            EditorGUILayout.LabelField("Nodes", asset.Nodes.Count.ToString());
            EditorGUILayout.Space();
            if (GUILayout.Button("Open in Behavior Tree Editor", GUILayout.Height(28f)))
                BehaviorTreeWindow.Open(asset);
            if (GUILayout.Button("Open in Behavior Composer"))
                BehaviorComposerWindow.Open(null, asset);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_users.Count == 0 ? "No enemy type thinks with this tree directly." :
                                       $"Used by {_users.Count} enemy type{(_users.Count == 1 ? "" : "s")} — editing it changes all of them:",
                                       EditorStyles.wordWrappedMiniLabel);
            foreach (EnemyData data in _users)
                if (GUILayout.Button(data.name, EditorStyles.linkLabel)) Selection.activeObject = data;

            if (_trees.Count == 0) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Runs as a behavior in {_trees.Count} tree{(_trees.Count == 1 ? "" : "s")}:", EditorStyles.wordWrappedMiniLabel);
            foreach (BehaviorTreeAsset tree in _trees)
                if (GUILayout.Button(tree.name, EditorStyles.linkLabel)) BehaviorComposerWindow.Open(null, tree);
        }
    }
}
