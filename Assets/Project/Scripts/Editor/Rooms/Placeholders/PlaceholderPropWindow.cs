using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Placeholder Prop Builder (Window › CGD › Placeholder Prop Builder): a kit prop made from
    // at most three primitives in palette colours, seen from above against the footprint it
    // will take, and saved to the props folder (PlaceholderPropBuilder). Load an existing prop
    // to rework it; it is rebuilt in place, so everything that places it keeps working.
    public class PlaceholderPropWindow : EditorWindow
    {
        private const float PlanSize = 180f;

        [SerializeField] private PlaceholderPalette _palette;
        [SerializeField] private string _name = "NewProp";
        [SerializeField] private Vector2Int _footprint = Vector2Int.one;
        [SerializeField] private List<PlaceholderPart> _parts = new();
        [SerializeField] private GameObject _source;
        private string _loadError;
        private Vector2 _scroll;

        [MenuItem("Window/CGD/Placeholder Prop Builder")]
        public static void Open() => GetWindow<PlaceholderPropWindow>("Placeholder Props");

        private void OnEnable()
        {
            if (_palette == null) _palette = AssetDatabase.LoadAssetAtPath<PlaceholderPalette>(PlaceholderPalette.DefaultPath);
            if (_parts.Count == 0) StartFrom(Template.FloorMachine);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _palette   = (PlaceholderPalette)EditorGUILayout.ObjectField("Palette", _palette, typeof(PlaceholderPalette), false);
            DrawLoad();
            EditorGUILayout.Space(6f);

            _name      = EditorGUILayout.TextField(new GUIContent("Name", "The prefab's name, <Thing> (FuelTank)"), _name);
            _footprint = Vector2Int.Max(Vector2Int.one, EditorGUILayout.Vector2IntField(new GUIContent("Footprint", "Tiles (3 m) the placement gives it; its back (−z) goes against the wall"), _footprint));
            DrawTemplates();
            EditorGUILayout.Space(6f);

            for (int i = 0; i < _parts.Count; i++)
                if (DrawPart(_parts[i])) { _parts.RemoveAt(i); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(_parts.Count >= PlaceholderPropBuilder.MaxParts))
                if (GUILayout.Button($"Add part ({_parts.Count}/{PlaceholderPropBuilder.MaxParts})"))
                    _parts.Add(_parts.Count > 0 ? _parts[_parts.Count - 1].Clone() : new PlaceholderPart());

            EditorGUILayout.Space(6f);
            DrawPlan(GUILayoutUtility.GetRect(PlanSize, PlanSize, GUILayout.ExpandWidth(false)));
            DrawSave();
            EditorGUILayout.EndScrollView();
        }

        private void DrawLoad()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _source = (GameObject)EditorGUILayout.ObjectField("Rework prop", _source, typeof(GameObject), false);
                using (new EditorGUI.DisabledScope(_source == null))
                    if (GUILayout.Button("Load", GUILayout.Width(56f))) Load(_source);
            }
            if (_loadError != null) EditorGUILayout.HelpBox(_loadError, MessageType.Warning);
        }

        private void Load(GameObject prefab)
        {
            List<PlaceholderPart> parts = PlaceholderPropBuilder.Read(prefab, _palette, out _loadError);
            if (parts == null) return;
            _parts = parts;
            _name  = prefab.name;
            Bounds all = new(Vector3.zero, Vector3.zero);
            foreach (PlaceholderPart part in parts) all.Encapsulate(part.Bounds);
            _footprint = new Vector2Int(Mathf.Max(1, Mathf.CeilToInt(all.size.x / PlaceholderPropBuilder.TileMetres - 0.05f)),
                                        Mathf.Max(1, Mathf.CeilToInt(all.size.z / PlaceholderPropBuilder.TileMetres - 0.05f)));
        }

        // --- Templates ------------------------------------------------------------------

        private enum Template { WallCabinet, FloorMachine, Tank, Console }

        private void DrawTemplates()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Start from", "Replaces the parts with a starting point sized to the footprint"));
                if (GUILayout.Button("Wall cabinet", EditorStyles.miniButtonLeft)) StartFrom(Template.WallCabinet);
                if (GUILayout.Button("Machine", EditorStyles.miniButtonMid)) StartFrom(Template.FloorMachine);
                if (GUILayout.Button("Tank", EditorStyles.miniButtonMid)) StartFrom(Template.Tank);
                if (GUILayout.Button("Console", EditorStyles.miniButtonRight)) StartFrom(Template.Console);
            }
        }

        private void StartFrom(Template template)
        {
            float w = _footprint.x * PlaceholderPropBuilder.TileMetres * 0.85f, d = _footprint.y * PlaceholderPropBuilder.TileMetres * 0.85f;
            int metal = Swatch("Plating"), machine = Swatch("Machinery"), accent = Swatch("Screen");
            _parts = template switch
            {
                Template.WallCabinet => new List<PlaceholderPart>
                {
                    Part("Body", PlaceholderShape.Box, new Vector3(0f, 1.1f, -d / 2f + 0.4f), new Vector3(w, 2.2f, 0.8f), metal),
                    Part("Panel", PlaceholderShape.Box, new Vector3(0f, 1.4f, -d / 2f + 0.81f), new Vector3(w * 0.7f, 0.5f, 0.02f), accent, false),
                },
                Template.Tank => new List<PlaceholderPart>
                {
                    Part("Tank", PlaceholderShape.Cylinder, new Vector3(0f, 1.4f, 0f), new Vector3(Mathf.Min(w, d), 2.8f, Mathf.Min(w, d)), metal),
                    Part("Cap", PlaceholderShape.Cylinder, new Vector3(0f, 2.85f, 0f), new Vector3(Mathf.Min(w, d) * 0.4f, 0.1f, Mathf.Min(w, d) * 0.4f), machine, false),
                },
                Template.Console => new List<PlaceholderPart>
                {
                    Part("Desk", PlaceholderShape.Box, new Vector3(0f, 0.45f, 0f), new Vector3(w * 0.8f, 0.9f, d * 0.4f), metal),
                    Part("Screen", PlaceholderShape.Box, new Vector3(0f, 1.25f, -d * 0.12f), new Vector3(w * 0.7f, 0.6f, 0.06f), accent, false),
                },
                _ => new List<PlaceholderPart>
                {
                    Part("Base", PlaceholderShape.Box, new Vector3(0f, 0.2f, 0f), new Vector3(w, 0.4f, d), metal),
                    Part("Housing", PlaceholderShape.Box, new Vector3(0f, 1.2f, 0f), new Vector3(w * 0.8f, 1.6f, d * 0.8f), machine),
                },
            };
        }

        private static PlaceholderPart Part(string name, PlaceholderShape shape, Vector3 centre, Vector3 size, int swatch, bool solid = true) =>
            new() { Name = name, Shape = shape, Centre = centre, Size = size, Swatch = swatch, Solid = solid };

        // The palette swatch named `name`, else the first.
        private int Swatch(string name)
        {
            if (_palette == null) return 0;
            for (int i = 0; i < _palette.Swatches.Count; i++)
                if (_palette.Swatches[i] != null && _palette.Swatches[i].Name == name) return i;
            return 0;
        }

        // --- Parts ----------------------------------------------------------------------

        // True when the part's Remove button was pressed.
        private bool DrawPart(PlaceholderPart part)
        {
            bool remove;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    part.Name  = EditorGUILayout.TextField(part.Name);
                    part.Shape = (PlaceholderShape)EditorGUILayout.EnumPopup(part.Shape, GUILayout.Width(80f));
                    remove = GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60f));
                }
                if (_palette != null) part.Swatch = EditorGUILayout.Popup(new GUIContent("Colour"), part.Swatch, _palette.Labels());
                part.Centre = EditorGUILayout.Vector3Field(new GUIContent("Centre (m)", "From the prop's foot: x right, y up, z to its front"), part.Centre);
                part.Size   = EditorGUILayout.Vector3Field(new GUIContent("Size (m)", "Full width, height and depth"), part.Size);
                using (new EditorGUILayout.HorizontalScope())
                {
                    part.Tilt  = EditorGUILayout.FloatField(new GUIContent("Tilt (°)", "About the x axis"), part.Tilt);
                    part.Solid = EditorGUILayout.ToggleLeft(new GUIContent("Solid", "Has a collider"), part.Solid, GUILayout.Width(60f));
                }
            }
            return remove;
        }

        // Seen from above: the footprint's tiles, its front edge, and each part's outline.
        private void DrawPlan(Rect rect)
        {
            float metres = Mathf.Max(_footprint.x, _footprint.y) * PlaceholderPropBuilder.TileMetres;
            float scale = rect.width / metres;
            Vector2 middle = rect.center;
            Rect Tile(float x, float z, float w, float d) => new(middle.x + (x - w / 2f) * scale, middle.y - (z + d / 2f) * scale, w * scale, d * scale);

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            float fw = _footprint.x * PlaceholderPropBuilder.TileMetres, fd = _footprint.y * PlaceholderPropBuilder.TileMetres;
            EditorGUI.DrawRect(Tile(0f, 0f, fw, fd), new Color(0.22f, 0.22f, 0.22f));
            EditorGUI.DrawRect(Tile(0f, fd / 2f - 0.05f, fw, 0.1f), new Color(0.3f, 0.7f, 0.4f));   // front

            for (int i = 0; i < _parts.Count; i++)
            {
                Bounds bounds = _parts[i].Bounds;
                Rect outline = Tile(bounds.center.x, bounds.center.z, bounds.size.x, bounds.size.z);
                Color colour = Color.HSVToRGB(i / 3f, 0.6f, 0.9f);
                EditorGUI.DrawRect(outline, new Color(colour.r, colour.g, colour.b, 0.35f));
                GUI.Label(outline, _parts[i].Name, EditorStyles.miniLabel);
            }
        }

        private void DrawSave()
        {
            List<string> problems = PlaceholderPropBuilder.Problems(_parts, _footprint, _palette);
            foreach (string problem in problems) EditorGUILayout.HelpBox(problem, MessageType.Warning);

            string path = PlaceholderPropBuilder.PathFor(_name);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            bool tooMany = _parts.Count == 0 || _parts.Count > PlaceholderPropBuilder.MaxParts;
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_name) || tooMany))
                if (GUILayout.Button(exists ? $"Rebuild {_name}" : $"Create {_name}"))
                {
                    if (exists && !EditorUtility.DisplayDialog("Rebuild prop", $"Replace the parts of {path}? Everything that places it keeps working.", "Rebuild", "Cancel"))
                        return;
                    EditorGUIUtility.PingObject(PlaceholderPropBuilder.Save(_name, _parts, _palette));
                }
        }
    }
}
