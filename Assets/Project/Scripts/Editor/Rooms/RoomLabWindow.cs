using System.Globalization;
using System.Linq;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Room Lab (Window › CGD › Room Lab): one room generated on its own, through the level's
    // own pipeline (RoomSampleBuilder), to iterate on room functions, shapes, structure and
    // furniture without building a whole map. Left: what room (RoomLabRecipePanel). Middle:
    // its plan with one generation pass painted over it, and a readout for the tile under the
    // mouse (RoomLabPlanView); below it the next seeds (RoomLabVariations). Right: what it
    // turned out to be and what to look at (RoomLabReportPanel).
    //
    // Rebuilt whenever the recipe changes or any asset is edited (a room function, a dressing,
    // the build settings), so a designer can tune an asset in the Inspector and watch the room.
    public class RoomLabWindow : EditorWindow
    {
        private const float RecipeWidth = 270f;
        private const float ReportWidth = 310f;

        [SerializeField] private RoomLabRecipe _recipe = new();
        [SerializeField] private RoomLabLayer _layer = RoomLabLayer.Tags;
        [SerializeField] private LevelBlueprint.Fill _fill = LevelBlueprint.Fill.Faction;

        private readonly RoomLabRecipePanel _recipePanel = new();
        private readonly RoomLabPlanView    _planView    = new();
        private readonly RoomLabReportPanel _reportPanel = new();
        private readonly RoomLabVariations  _variations  = new();

        private RoomSample _sample;
        private bool _dirty = true;

        [MenuItem("Window/CGD/Room Lab")]
        public static void Open() => GetWindow<RoomLabWindow>();

        // The Room Lab on a room function: a fight in that place with three doorways.
        public static void OpenFunction(RoomFunction function)
        {
            RoomLabWindow window = GetWindow<RoomLabWindow>();
            window._recipe.Source   = RoomLabRecipe.SourceKind.Recipe;
            window._recipe.Place    = function;
            window._recipe.Category = function.Category;
            window.MarkDirty();
        }

        // The Room Lab on the room a map graph node becomes.
        public static void OpenMapRoom(MapGraphAsset graph, int nodeId, LevelBuildSettings settings)
        {
            RoomLabWindow window = GetWindow<RoomLabWindow>();
            window._recipe.Source = RoomLabRecipe.SourceKind.MapRoom;
            window._recipe.Graph  = graph;
            window._recipe.NodeId = nodeId;
            if (settings != null) window._recipe.BuildSettings = settings;
            window.MarkDirty();
        }

        private void OnEnable()
        {
            titleContent   = new GUIContent("Room Lab");
            wantsMouseMove = true;   // the tile readout follows the mouse
            _recipe ??= new RoomLabRecipe();
            if (_recipe.BuildSettings == null) _recipe.BuildSettings = MapEditorAssets.FindFirst<LevelBuildSettings>();
            if (_recipe.Content == null)       _recipe.Content       = MapEditorAssets.FindFirst<MapContentSettings>();

            Undo.undoRedoPerformed          += MarkDirty;
            Undo.postprocessModifications   += OnModified;
            EditorApplication.update        += BuildVariations;
            MarkDirty();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed        -= MarkDirty;
            Undo.postprocessModifications -= OnModified;
            EditorApplication.update      -= BuildVariations;
            _planView.Release();
            _variations.Release();
        }

        // Any edit in the Inspector may be to something the room is built from.
        private UndoPropertyModification[] OnModified(UndoPropertyModification[] modifications)
        {
            MarkDirty();
            return modifications;
        }

        private void OnProjectChange() => MarkDirty();

        private void MarkDirty()
        {
            _dirty = true;
            Repaint();
        }

        private void BuildVariations()
        {
            if (_variations.BuildSome(_fill)) Repaint();
        }

        private void OnGUI()
        {
            // Rebuilt only between events, so every control drawn in one event sees one room.
            if (_dirty && Event.current.type == EventType.Layout) Rebuild();

            DrawToolbar();
            float top = EditorStyles.toolbar.fixedHeight;
            float height = position.height - top;
            float middle = position.width - RecipeWidth - ReportWidth;

            if (_recipePanel.Draw(new Rect(4f, top + 4f, RecipeWidth - 8f, height - 8f), ref _recipe)) MarkDirty();

            _planView.Layer     = _layer;
            _planView.Fill      = _fill;
            _planView.Highlight = _reportPanel.Highlight;
            _planView.Draw(new Rect(RecipeWidth, top, middle, height - RoomLabVariations.Height), _sample);

            string picked = _variations.Draw(new Rect(RecipeWidth, position.height - RoomLabVariations.Height, middle, RoomLabVariations.Height));
            if (picked != null) UseSeed(picked);

            _reportPanel.Draw(new Rect(RecipeWidth + middle + 6f, top + 4f, ReportWidth - 10f, height - 8f), _sample);

            if (GUI.changed) Repaint();
        }

        private void DrawToolbar()
        {
            using var toolbar = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar);

            using (new EditorGUI.DisabledScope(_recipe.Source == RoomLabRecipe.SourceKind.MapRoom))
            {
                EditorGUIUtility.labelWidth = 34f;
                EditorGUI.BeginChangeCheck();
                string seed = EditorGUILayout.DelayedTextField(new GUIContent("Seed", "A number or any text. A map room uses its graph's seed"),
                                                               _recipe.Source == RoomLabRecipe.SourceKind.MapRoom && _recipe.Graph != null ? _recipe.Graph.SeedText : _recipe.Seed,
                                                               EditorStyles.toolbarTextField, GUILayout.Width(150f));
                if (EditorGUI.EndChangeCheck()) UseSeed(seed);
                EditorGUIUtility.labelWidth = 0f;

                if (GUILayout.Button(new GUIContent("Next", "The next seed (7 → 8, \"run 2\" → \"run 3\")"), EditorStyles.toolbarButton))
                    UseSeed(SeedSequence.CountingFrom(_recipe.Seed, 2).Last());
                if (GUILayout.Button(new GUIContent("Random", "A random seed"), EditorStyles.toolbarButton))
                    UseSeed(Random.Range(0, int.MaxValue).ToString(CultureInfo.InvariantCulture));
            }

            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            _layer = (RoomLabLayer)EditorGUILayout.EnumPopup(new GUIContent("", "What to paint over the plan"), _layer, EditorStyles.toolbarPopup, GUILayout.Width(100f));
            _fill  = (LevelBlueprint.Fill)EditorGUILayout.EnumPopup(new GUIContent("", "Floor colour: room type or faction"), _fill, EditorStyles.toolbarPopup, GUILayout.Width(90f));
            if (EditorGUI.EndChangeCheck()) MarkDirty();

            bool canPlay = _recipe.Source == RoomLabRecipe.SourceKind.MapRoom && MapGraphPlayLauncher.CanPlay(_recipe.Graph);
            using (new EditorGUI.DisabledScope(!canPlay))
            {
                if (GUILayout.Button(new GUIContent("Play from here", "Play the map graph in MapTest, starting in this room (map rooms only: a made-up room has no level)"),
                                     EditorStyles.toolbarButton))
                {
                    MapGraphPlayLauncher.Play(_recipe.Graph, _recipe.NodeId);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void UseSeed(string seed)
        {
            _recipe.Seed = string.IsNullOrWhiteSpace(seed) ? "1" : seed.Trim();
            GUI.FocusControl(null);
            MarkDirty();
        }

        private void Rebuild()
        {
            _dirty = false;
            try
            {
                _sample = RoomSampleBuilder.Build(_recipe);
            }
            catch (System.Exception exception)
            {
                // A half-set-up asset shouldn't break the window; the Console has the details.
                Debug.LogException(exception);
                _sample = new RoomSample($"Building the room failed: {exception.Message}");
            }
            _variations.Reset(_sample.IsValid ? _recipe : null);
        }
    }
}
