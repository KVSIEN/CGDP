using System.Collections.Generic;
using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A room function's Inspector: straight to the Room Lab, a new layout made in the place's
    // own folder and listed by it (RoomAuthoring), and every layout checked over a run of
    // seeds at once (RoomLayoutCheck), so a weak one stands out.
    [CustomEditor(typeof(RoomFunction))]
    public class RoomFunctionEditor : UnityEditor.Editor
    {
        private const int Seeds = 6;

        private List<RoomLayoutCheck> _checks;
        private string _newLayout = string.Empty;

        public override void OnInspectorGUI()
        {
            var place = (RoomFunction)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Open in Room Lab", "Build this place on its own and watch it change as you edit it")))
                    RoomLabWindow.OpenFunction(place);
                using (new EditorGUI.DisabledScope(place.Landmark != null))
                    if (GUILayout.Button(new GUIContent("Check layouts", $"Build every layout over {Seeds} seeds and see which hold up")))
                        _checks = RoomLayoutCheckView.CheckAll(RoomAuthoring.SettingsFor(place), place, Seeds);
            }
            if (_checks != null)
                foreach (RoomLayoutCheck check in _checks) RoomLayoutCheckView.DrawLine(check);

            using (new EditorGUILayout.HorizontalScope())
            {
                _newLayout = EditorGUILayout.TextField(new GUIContent("New layout", $"Made in {RoomAuthoring.FolderFor(place)} and added to this place's layouts"), _newLayout);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newLayout)))
                    if (GUILayout.Button("Create", GUILayout.Width(72f)))
                    {
                        Selection.activeObject = RoomAuthoring.CreateLayout(place, _newLayout.Trim());
                        GUIUtility.ExitGUI();
                    }
            }
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck()) _checks = null;
        }
    }
}
