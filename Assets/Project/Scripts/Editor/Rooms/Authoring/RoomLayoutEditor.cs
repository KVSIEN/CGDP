using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A room layout's Inspector: straight to the Room Lab on the place that lists it, a check
    // of the layout over a run of seeds (RoomLayoutCheck), and a copy of it as a new layout of
    // the same place, so a variant starts from what already works.
    [CustomEditor(typeof(RoomLayout))]
    public class RoomLayoutEditor : UnityEditor.Editor
    {
        private RoomFunction _place;
        private RoomLayoutCheck _check;
        private int _seeds = RoomLayoutCheck.DefaultSeeds;
        private string _copyName = string.Empty;

        private void OnEnable() => _place = RoomAuthoring.PlaceOf((RoomLayout)target);

        public override void OnInspectorGUI()
        {
            var layout = (RoomLayout)target;
            using (new EditorGUI.DisabledScope(_place == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Open in Room Lab",
                            _place != null ? $"Build {_place.DisplayName} in this layout and watch it change as you edit it" : "No room function lists this layout yet")))
                        RoomLabWindow.OpenLayout(_place, layout);
                    if (GUILayout.Button(new GUIContent("Check", "Build this layout over a run of seeds and see how each piece fits")))
                        _check = RoomLayoutCheck.Run(RoomAuthoring.SettingsFor(_place), _place, layout, _seeds);
                }
                _seeds = EditorGUILayout.IntSlider(new GUIContent("Seeds", "Rooms the check builds"), _seeds, 4, 48);
                RoomLayoutCheckView.Draw(_check);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _copyName = EditorGUILayout.TextField(new GUIContent("New variant", "Name of a copy of this layout, added to the same place"), _copyName);
                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_copyName)))
                        if (GUILayout.Button("Duplicate", GUILayout.Width(72f)))
                        {
                            Selection.activeObject = RoomAuthoring.CreateLayout(_place, _copyName.Trim(), layout);
                            GUIUtility.ExitGUI();
                        }
                }
            }
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck()) _check = null;   // stale once the layout changes
        }
    }
}
