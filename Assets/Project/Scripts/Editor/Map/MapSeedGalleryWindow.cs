using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGD.Core;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A page of maps a Map Graph asset's style grows, one per seed counting up from a typed
    // one (SeedSequence: "100" → 100, 101…; "banana" → banana 1, banana 2…), drawn small so
    // good ones are easy to spot. Use generates the asset from that seed in the Map Graph
    // window. Each preview is generated with the asset's locked rooms pinned, so it is exactly
    // what Use gives. Maps are generated a few per editor tick so the window stays responsive;
    // each is captioned once, when it's generated, since cells redraw on every repaint.
    public class MapSeedGalleryWindow : EditorWindow
    {
        private const float  CellWidth     = 230f;
        private const float  CellHeight    = 200f;
        private const float  CaptionHeight = 62f;
        private const float  Gap           = 6f;
        private const double TickBudget    = 0.03; // seconds of generating per editor tick
        private const int    MaxCount      = 100;

        private class Entry
        {
            public string              Seed;
            public bool                Attempted;
            public MapGenerationResult Result;
            public string              Shape;      // "18 rooms · Branching"
            public string              Modifiers;  // "Lights Out, Bounty" or "no run modifiers"
            public string              Problems;   // "no issues" or "2 issues · 1 generator warning"
            public bool                Clean;
        }

        [SerializeField] private MapGraphAsset _asset;
        [SerializeField] private string _firstSeed = "1";
        [SerializeField] private int    _count = 24;

        private readonly List<Entry> _entries = new();
        // For the page being generated: the style's generator and the asset's locked rooms.
        private MapGenerator     _generator;
        private List<MapNodePin> _pins;
        private int     _next;
        private Vector2 _scroll;
        private GUIStyle _seedStyle;
        private GUIStyle _issueStyle;

        public static void Open(MapGraphAsset asset)
        {
            var window = GetWindow<MapSeedGalleryWindow>("Seed Gallery");
            window._asset = asset;
            window.Restart();
        }

        private void OnEnable()
        {
            EditorApplication.update += GenerateSome;
            Restart();
        }

        private void OnDisable() => EditorApplication.update -= GenerateSome;

        private bool CanGenerate => _asset != null && _asset.CanGenerate;

        private void Restart()
        {
            _entries.Clear();
            _next      = 0;
            _generator = null;
            _pins      = null;
            foreach (string seed in SeedSequence.CountingFrom(_firstSeed, _count))
                _entries.Add(new Entry { Seed = seed });
            Repaint();
        }

        // A failing seed throws into the Console once and shows as failed; the rest go on.
        private void GenerateSome()
        {
            if (_next >= _entries.Count || !CanGenerate) return;

            MapGenerationSettings settings = _asset.Settings;
            _generator ??= new MapGenerator(settings);
            _pins      ??= _asset.BuildPins();

            double stop = EditorApplication.timeSinceStartup + TickBudget;
            try
            {
                do Generate(_entries[_next++], _generator, _pins, settings);
                while (_next < _entries.Count && EditorApplication.timeSinceStartup < stop);
            }
            finally
            {
                Repaint();
            }
        }

        private static void Generate(Entry entry, MapGenerator generator, List<MapNodePin> pins, MapGenerationSettings settings)
        {
            entry.Attempted = true;
            MapGenerationResult result = generator.Generate(Seed.Parse(entry.Seed), null, pins);
            var analysis = new MapGraphAnalysis(result.Graph);
            int issues   = MapGraphValidator.Validate(result.Graph, analysis, result.Layout, settings.Content,
                                                      new MapRunTuning(result.Modifiers)).Count;
            entry.Result = result;
            Caption(entry, issues);
        }

        private static void Caption(Entry entry, int issues)
        {
            MapGenerationResult result = entry.Result;
            string layout = result.Layout != null ? result.Layout.name.Replace("MapLayoutSettings", "") : "?";
            entry.Shape     = $"{result.Graph.Nodes.Count} rooms · {layout}";
            entry.Modifiers = result.Modifiers.Count == 0 ? "no run modifiers"
                            : string.Join(", ", result.Modifiers.Where(m => m != null).Select(m => m.DisplayName));

            int warnings = result.Warnings.Count;
            entry.Clean = issues == 0 && warnings == 0;
            if (entry.Clean)
            {
                entry.Problems = "no issues";
                return;
            }
            var text = new StringBuilder($"{issues} issue{(issues == 1 ? "" : "s")}");
            if (warnings > 0) text.Append($" · {warnings} generator warning{(warnings == 1 ? "" : "s")}");
            entry.Problems = text.ToString();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (_asset == null)
            {
                EditorGUILayout.HelpBox("Pick a Map Graph asset: its style generates the previews and Use loads a seed into it.", MessageType.Info);
                return;
            }
            if (!_asset.CanGenerate)
            {
                EditorGUILayout.HelpBox($"{_asset.name} has no style that can generate (it needs content and at least one layout).", MessageType.Warning);
                return;
            }

            DrawGrid();
        }

        private void DrawToolbar()
        {
            using var toolbar = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            _asset = (MapGraphAsset)EditorGUILayout.ObjectField(_asset, typeof(MapGraphAsset), false, GUILayout.Width(200f));
            EditorGUIUtility.labelWidth = 36f;
            _firstSeed = EditorGUILayout.TextField(new GUIContent("From", "A number or any text; the seeds count up from it"),
                                                   _firstSeed, EditorStyles.toolbarTextField, GUILayout.Width(160f));
            _count = Mathf.Clamp(EditorGUILayout.IntField("Maps", _count, EditorStyles.toolbarTextField, GUILayout.Width(80f)), 1, MaxCount);
            EditorGUIUtility.labelWidth = 0f;
            bool changed = EditorGUI.EndChangeCheck();

            if (GUILayout.Button(new GUIContent("Next Page", "The next seeds after this page"), EditorStyles.toolbarButton))
            {
                _firstSeed = SeedSequence.CountingFrom(_firstSeed, _count + 1).Last();
                changed = true;
            }
            if (GUILayout.Button(new GUIContent("Refresh", "Generate the page again (after changing the style or locked rooms)"), EditorStyles.toolbarButton))
                changed = true;

            GUILayout.FlexibleSpace();
            if (_next < _entries.Count && CanGenerate) GUILayout.Label($"Generating {_next}/{_entries.Count}…", EditorStyles.miniLabel);

            if (changed) Restart();
        }

        private void DrawGrid()
        {
            float width   = position.width;
            int   columns = Mathf.Max(1, Mathf.FloorToInt((width - Gap) / (CellWidth + Gap)));
            int   rows    = Mathf.CeilToInt(_entries.Count / (float)columns);
            var   content = new Rect(0f, 0f, width - 16f, rows * (CellHeight + Gap) + Gap);

            Rect view = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _scroll = GUI.BeginScrollView(view, _scroll, content);
            for (int i = 0; i < _entries.Count; i++)
            {
                var cell = new Rect(Gap + (i % columns) * (CellWidth + Gap), Gap + (i / columns) * (CellHeight + Gap), CellWidth, CellHeight);
                if (cell.yMax < _scroll.y || cell.y > _scroll.y + view.height) continue;
                DrawCell(cell, _entries[i]);
            }
            GUI.EndScrollView();
        }

        private void DrawCell(Rect cell, Entry entry)
        {
            bool current = entry.Seed == _asset.SeedText?.Trim();
            if (current) MapGraphStyle.DrawOutline(cell, MapGraphStyle.Selection, 2f);
            EditorGUI.DrawRect(cell, MapGraphStyle.Background);

            var picture = new Rect(cell.x, cell.y, cell.width, cell.height - CaptionHeight);
            if (entry.Result != null) MapGraphThumbnail.Draw(picture, entry.Result.Graph);
            else GUI.Label(picture, entry.Attempted ? "Failed — see the Console" : "Generating…", EditorStyles.centeredGreyMiniLabel);

            float y = picture.yMax + 2f;
            var seedLine = new Rect(cell.x + 6f, y, cell.width - 62f, 16f);
            GUI.Label(seedLine, current ? $"{entry.Seed}  (current)" : entry.Seed, _seedStyle);

            using (new EditorGUI.DisabledScope(entry.Result == null))
            {
                if (GUI.Button(new Rect(cell.xMax - 52f, y, 46f, 16f), new GUIContent("Use", "Generate the Map Graph asset from this seed"), EditorStyles.miniButton))
                {
                    MapGraphWindow.UseSeed(_asset, entry.Seed);
                    GUIUtility.ExitGUI();
                }
            }
            if (entry.Result == null) return;

            GUI.Label(new Rect(cell.x + 6f, y + 16f, cell.width - 12f, 14f), entry.Shape, EditorStyles.miniLabel);
            GUI.Label(new Rect(cell.x + 6f, y + 29f, cell.width - 12f, 14f), entry.Modifiers, EditorStyles.miniLabel);
            _issueStyle.normal.textColor = entry.Clean ? CleanColor : MapGraphStyle.Issue;
            GUI.Label(new Rect(cell.x + 6f, y + 42f, cell.width - 12f, 14f), entry.Problems, _issueStyle);
        }

        private static readonly Color CleanColor = new(0.6f, 0.85f, 0.6f);

        private void EnsureStyles()
        {
            if (_seedStyle != null) return;
            _seedStyle  = new GUIStyle(EditorStyles.boldLabel) { clipping = TextClipping.Clip };
            _issueStyle = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
        }
    }
}
