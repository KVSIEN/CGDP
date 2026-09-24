using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Editor for MapGraphAsset: generate from a seed, then inspect and hand-edit the
    // result. Drawing and input live in MapGraphCanvas, the side panel in
    // MapGraphInspectorPanel, and all edits go through MapGraphEditorSession.
    public class MapGraphWindow : EditorWindow
    {
        private const float PanelWidth    = 300f;
        private const string DefaultFolder = "Assets/Project/Data/Map";

        [SerializeField] private MapGraphEditorSession _session = new();

        private readonly MapGraphCanvas         _canvas = new();
        private readonly MapGraphInspectorPanel _panel  = new();

        [MenuItem("Window/CGD/Map Graph")]
        public static void Open() => GetWindow<MapGraphWindow>();

        public static void Open(MapGraphAsset asset)
        {
            var window = GetWindow<MapGraphWindow>();
            window._session.Asset = asset;
            window._session.Invalidate();
            window.Repaint();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Map Graph");
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        private void OnUndoRedo()
        {
            _session.Invalidate();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            float top = EditorStyles.toolbar.fixedHeight;
            float panelWidth = _session.HasGraph ? PanelWidth : 0f;
            var canvasRect = new Rect(0f, top, position.width - panelWidth, position.height - top);

            _canvas.Draw(canvasRect, _session);

            if (_session.HasGraph)
                _panel.Draw(new Rect(canvasRect.xMax, top, panelWidth, canvasRect.height), _session);
            else
                DrawEmptyState(canvasRect);

            if (GUI.changed) Repaint();
        }

        private void DrawToolbar()
        {
            using var toolbar = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar);

            _session.Asset = (MapGraphAsset)EditorGUILayout.ObjectField(
                _session.Asset, typeof(MapGraphAsset), false, GUILayout.Width(200f));

            if (!_session.HasGraph)
            {
                GUILayout.FlexibleSpace();
                return;
            }

            DrawGenerationControls(_session.Asset);

            GUILayout.FlexibleSpace();

            _session.ViewMode = (MapGraphViewMode)EditorGUILayout.EnumPopup(
                _session.ViewMode, EditorStyles.toolbarPopup, GUILayout.Width(120f));
            _session.HighlightPath = GUILayout.Toggle(_session.HighlightPath,
                new GUIContent("Path", "Highlight the route from Start to the selected node"), EditorStyles.toolbarButton);
            _session.HighlightBranch = GUILayout.Toggle(_session.HighlightBranch,
                new GUIContent("Branch", "Dim everything outside the selected node's branch"), EditorStyles.toolbarButton);

            if (GUILayout.Button(new GUIContent("Frame", "Fit the graph to the view (F)"), EditorStyles.toolbarButton))
                _canvas.Frame(_session, new Vector2(position.width - PanelWidth, position.height));
        }

        // Settings and seed are plain serialized fields on the asset, edited through
        // SerializedObject so they get undo and prefab-style dirtying for free.
        private void DrawGenerationControls(MapGraphAsset asset)
        {
            var serialized = new SerializedObject(asset);
            SerializedProperty settings = serialized.FindProperty("_settings");
            SerializedProperty seed     = serialized.FindProperty("_seed");

            EditorGUILayout.PropertyField(settings, GUIContent.none, GUILayout.Width(180f));
            EditorGUIUtility.labelWidth = 34f;
            seed.intValue = EditorGUILayout.IntField("Seed", seed.intValue, EditorStyles.toolbarTextField, GUILayout.Width(110f));
            EditorGUIUtility.labelWidth = 0f;
            serialized.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(asset.Settings == null))
            {
                if (GUILayout.Button(new GUIContent("Generate", "Rebuild the map from this seed"), EditorStyles.toolbarButton))
                    TryRegenerate(asset.Seed);

                if (GUILayout.Button(new GUIContent("New Seed", "Pick a random seed and generate"), EditorStyles.toolbarButton))
                    TryRegenerate(Random.Range(0, int.MaxValue));
            }

            using (new EditorGUI.DisabledScope(!_session.HasEdits()))
            {
                if (GUILayout.Button(new GUIContent("Revert Edits", "Back to the last generated graph"), EditorStyles.toolbarButton))
                    _session.RevertEdits();
            }
        }

        private void TryRegenerate(int seed)
        {
            if (!_session.ConfirmDiscardEdits()) return;

            _session.Regenerate(seed);
            _canvas.Frame(_session, new Vector2(position.width - PanelWidth, position.height));
            GUIUtility.ExitGUI();
        }

        private void DrawEmptyState(Rect rect)
        {
            GUILayout.BeginArea(new Rect(rect.center.x - 160f, rect.center.y - 50f, 320f, 100f));
            EditorGUILayout.HelpBox("Pick a Map Graph asset in the toolbar, or create one.", MessageType.Info);
            if (GUILayout.Button("Create Map Graph…")) CreateAsset();
            GUILayout.EndArea();
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Map Graph", "MapGraph", "asset", "Choose where to save the map graph.", DefaultFolder);
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<MapGraphAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _session.Asset = asset;
        }
    }
}
