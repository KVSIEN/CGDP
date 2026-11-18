using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A room layout's Inspector with a way straight to the Room Lab, on the place that lists
    // the layout, so the room it builds updates as its fields change.
    [CustomEditor(typeof(RoomLayout))]
    public class RoomLayoutEditor : UnityEditor.Editor
    {
        private RoomFunction _place;

        private void OnEnable() => _place = PlaceOf((RoomLayout)target);

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(_place == null))
                if (GUILayout.Button(new GUIContent("Open in Room Lab",
                        _place != null ? $"Build {_place.DisplayName} in this layout and watch it change as you edit it" : "No room function lists this layout yet")))
                    RoomLabWindow.OpenLayout(_place, (RoomLayout)target);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }

        // The first room function that lists the layout.
        private static RoomFunction PlaceOf(RoomLayout layout)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:RoomFunction"))
            {
                var function = AssetDatabase.LoadAssetAtPath<RoomFunction>(AssetDatabase.GUIDToAssetPath(guid));
                if (function == null) continue;
                foreach (RoomLayout listed in function.Layouts)
                    if (listed == layout) return function;
            }
            return null;
        }
    }
}
