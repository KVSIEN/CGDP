using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Trees are edited in the graph window, not field by field.
    [CustomEditor(typeof(BehaviorTreeAsset))]
    public class BehaviorTreeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (BehaviorTreeAsset)target;

            EditorGUILayout.LabelField("Nodes", asset.Nodes.Count.ToString());
            EditorGUILayout.Space();
            if (GUILayout.Button("Open in Behavior Tree Editor", GUILayout.Height(28f)))
                BehaviorTreeWindow.Open(asset);
        }
    }
}
