using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A room function's Inspector with a way straight to the Room Lab, where the room it
    // builds updates as its fields change.
    [CustomEditor(typeof(RoomFunction))]
    public class RoomFunctionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(new GUIContent("Open in Room Lab", "Build this place on its own and watch it change as you edit it")))
                RoomLabWindow.OpenFunction((RoomFunction)target);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }
}
