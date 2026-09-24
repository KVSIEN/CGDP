using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The graph itself is edited in the Map Graph window; the Inspector only shows the
    // generation inputs and a way in.
    [CustomEditor(typeof(MapGraphAsset))]
    public class MapGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var asset = (MapGraphAsset)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Nodes / Connections", $"{asset.Graph.Nodes.Count} / {asset.Graph.Connections.Count}");

            if (GUILayout.Button("Open Map Graph Editor"))
                MapGraphWindow.Open(asset);
        }
    }
}
