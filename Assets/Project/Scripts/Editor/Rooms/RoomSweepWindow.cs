using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Room Sweep (Room Lab › Sweep…): builds the Room Lab's recipe for every place and every
    // faction over a run of seeds (RoomSweep) and shows the summary (RoomSweepReport), with a
    // button to copy it. For tuning the trait bands and Priority Tries; see
    // ROOM_GENERATION.md › Roadmap › Tuning.
    public class RoomSweepWindow : EditorWindow
    {
        private const int DefaultSeeds = 10;

        private RoomLabRecipe _recipe;
        private int _seeds = DefaultSeeds;
        private RoomSweep _sweep;
        private string _report = "";
        private Vector2 _scroll;

        public static void Open(RoomLabRecipe recipe)
        {
            var window = GetWindow<RoomSweepWindow>();
            window.titleContent = new GUIContent("Room Sweep");
            window._recipe = recipe.Clone();
            window._sweep  = null;
            window._report = "";
        }

        private void OnEnable()  => EditorApplication.update += Step;
        private void OnDisable() => EditorApplication.update -= Step;

        private void Step()
        {
            if (_sweep == null || !_sweep.Step()) return;
            if (_sweep.IsFinished) _report = RoomSweepReport.Write(_sweep);
            Repaint();
        }

        private void OnGUI()
        {
            if (_recipe == null || _recipe.BuildSettings == null || _recipe.Content == null)
            {
                EditorGUILayout.HelpBox("Open the sweep from the Room Lab (Sweep… in its toolbar), with Level Build Settings and Content picked.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Recipe", $"{_recipe.Type} · tier {_recipe.Tier} · {_recipe.DoorCount} doorway(s) · seeds from \"{_recipe.Seed}\"");
            EditorGUILayout.LabelField("Rooms", "every place but the landmarks × every faction in the content × the seeds");
            _seeds = EditorGUILayout.IntSlider(new GUIContent("Seeds", "Rooms built per place and faction"), _seeds, 1, 50);

            using (new EditorGUILayout.HorizontalScope())
            {
                bool running = _sweep != null && !_sweep.IsFinished;
                if (GUILayout.Button(running ? "Restart" : "Run"))
                {
                    _sweep  = new RoomSweep(_recipe, _seeds);
                    _report = _sweep.IsFinished ? RoomSweepReport.Write(_sweep) : "";
                }
                using (new EditorGUI.DisabledScope(!running))
                    if (GUILayout.Button("Stop"))
                    {
                        // Reports what was built so far.
                        _report = RoomSweepReport.Write(_sweep);
                        _sweep  = null;
                    }
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_report)))
                    if (GUILayout.Button("Copy report")) EditorGUIUtility.systemCopyBuffer = _report;
            }

            if (_sweep != null && !_sweep.IsFinished && string.IsNullOrEmpty(_report))
            {
                Rect bar = EditorGUILayout.GetControlRect(false, 18f);
                EditorGUI.ProgressBar(bar, _sweep.Total > 0 ? _sweep.Done / (float)_sweep.Total : 1f, $"{_sweep.Done} of {_sweep.Total} rooms");
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(_report, EditorStyles.textArea, GUILayout.ExpandHeight(true),
                                            GUILayout.MinHeight(EditorStyles.textArea.CalcHeight(new GUIContent(_report), position.width - 30f)));
            EditorGUILayout.EndScrollView();
        }
    }
}
