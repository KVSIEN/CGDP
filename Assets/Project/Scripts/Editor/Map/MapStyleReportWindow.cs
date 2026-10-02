using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Runs MapStyleReport for a map style and shows the result, for tuning layout and
    // content settings against many seeds at once.
    public class MapStyleReportWindow : EditorWindow
    {
        [SerializeField] private MapGenerationSettings _settings;
        [SerializeField] private int _seeds = 100;
        [SerializeField] private int _firstSeed = 1;

        private string _report;
        private Vector2 _scroll;

        public static void Open(MapGenerationSettings settings)
        {
            var window = GetWindow<MapStyleReportWindow>("Map Style Report");
            window._settings = settings;
            window.Run();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _settings  = (MapGenerationSettings)EditorGUILayout.ObjectField(_settings, typeof(MapGenerationSettings), false, GUILayout.Width(220f));
                EditorGUIUtility.labelWidth = 44f;
                _seeds     = Mathf.Clamp(EditorGUILayout.IntField("Seeds", _seeds, EditorStyles.toolbarTextField, GUILayout.Width(100f)), 1, 2000);
                _firstSeed = EditorGUILayout.IntField("From", _firstSeed, EditorStyles.toolbarTextField, GUILayout.Width(110f));
                EditorGUIUtility.labelWidth = 0f;

                using (new EditorGUI.DisabledScope(_settings == null || !_settings.CanGenerate))
                    if (GUILayout.Button("Run", EditorStyles.toolbarButton)) Run();

                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_report)))
                    if (GUILayout.Button("Copy", EditorStyles.toolbarButton)) EditorGUIUtility.systemCopyBuffer = _report;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(_report ?? "Pick a style and press Run.", EditorStyles.wordWrappedLabel,
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(position.height - 30f));
            EditorGUILayout.EndScrollView();
        }

        private void Run()
        {
            if (_settings == null || !_settings.CanGenerate) return;
            _report = MapStyleReport.Run(_settings, _seeds, _firstSeed).ToText();
            Repaint();
        }
    }
}
