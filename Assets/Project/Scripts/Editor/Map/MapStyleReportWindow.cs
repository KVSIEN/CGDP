using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Runs MapStyleReport for a map style and shows the result, for tuning layout and
    // content settings against many seeds at once. Given build settings, it can also build the
    // levels themselves (MapDirectorSweep) and read them by the map director's beats and checks:
    // slower, so it runs on its own count of seeds.
    public class MapStyleReportWindow : EditorWindow
    {
        [SerializeField] private MapGenerationSettings _settings;
        [SerializeField] private int _seeds = 100;
        [SerializeField] private string _firstSeed = "1";
        [SerializeField] private LevelBuildSettings _build;
        [SerializeField] private int _levels = 20;
        [SerializeField] private bool _furnish;

        private string _report;
        private string _levelReport;
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
                _firstSeed = EditorGUILayout.TextField(new GUIContent("From", "A number or any text; the seeds count up from it (\"banana\" → banana 1, banana 2…)"),
                                                       _firstSeed, EditorStyles.toolbarTextField, GUILayout.Width(150f));
                EditorGUIUtility.labelWidth = 0f;

                using (new EditorGUI.DisabledScope(_settings == null || !_settings.CanGenerate))
                    if (GUILayout.Button("Run", EditorStyles.toolbarButton)) Run();

                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_report) && string.IsNullOrEmpty(_levelReport)))
                    if (GUILayout.Button("Copy", EditorStyles.toolbarButton)) EditorGUIUtility.systemCopyBuffer = Text();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _build = (LevelBuildSettings)EditorGUILayout.ObjectField(new GUIContent("", "Build settings to build the levels with, for the map director's beats and checks"),
                                                                         _build, typeof(LevelBuildSettings), false, GUILayout.Width(220f));
                EditorGUIUtility.labelWidth = 44f;
                _levels  = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Levels", "Seeds to build levels for, from the same first seed (about a second each)"),
                                                                 _levels, EditorStyles.toolbarTextField, GUILayout.Width(100f)), 1, 500);
                EditorGUIUtility.labelWidth = 0f;
                _furnish = GUILayout.Toggle(_furnish, new GUIContent("Furnish", "Furnish every room too, to check the supplies found a place (several times slower)"),
                                            EditorStyles.toolbarButton, GUILayout.Width(60f));
                using (new EditorGUI.DisabledScope(_settings == null || !_settings.CanGenerate || _build == null))
                    if (GUILayout.Button("Build levels", EditorStyles.toolbarButton)) BuildLevels();
                GUILayout.FlexibleSpace();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(Text() ?? "Pick a style and press Run, or pick build settings too and press Build levels.",
                EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true), GUILayout.MinHeight(position.height - 50f));
            EditorGUILayout.EndScrollView();
        }

        private void Run()
        {
            if (_settings == null || !_settings.CanGenerate) return;
            _report = MapStyleReport.Run(_settings, _seeds, _firstSeed).ToText();
            Repaint();
        }

        private void BuildLevels()
        {
            if (_settings == null || !_settings.CanGenerate || _build == null) return;
            try
            {
                MapDirectorSweep sweep = MapDirectorSweep.Run(_settings, _build, _levels, _firstSeed, _furnish,
                    (done, total) => EditorUtility.DisplayCancelableProgressBar("Building levels", $"Level {done + 1} of {total}", (float)done / total));
                _levelReport = sweep.ToText();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Repaint();
        }

        // Both reports, the levels' first (null when neither has run).
        private string Text()
        {
            if (string.IsNullOrEmpty(_levelReport)) return _report;
            return string.IsNullOrEmpty(_report) ? _levelReport : _levelReport + "\n\n" + _report;
        }
    }
}
