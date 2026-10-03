using CGD.Core;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The Map Graph window's Blueprint mode: the graph run through LevelLayoutBuilder —
    // exactly what LevelBuilder lays out, with the graph's seed — and drawn as a floor
    // plan (LevelBlueprint). Rebuilt whenever the graph changes. Scroll to zoom, drag to
    // pan, F to frame; clicking a room selects its node, so the side panel works as in
    // Graph mode.
    public class MapBlueprintView
    {
        private const float LabelMinZoom = 1.2f;

        private LevelBuildSettings _builtWith;
        private LevelBlueprint.Fill _builtFill;
        private int         _builtVersion = -1;
        private MapGraphAsset _builtAsset;
        private LevelLayout _layout;
        private Texture2D   _texture;
        private Vector2Int  _tileOrigin;
        private Vector2     _pan;
        private float       _zoom = 1f;
        private bool        _needsFrame = true;
        private GUIStyle    _labelStyle;

        public LevelBlueprint.Fill Fill { get; set; }

        public void Draw(Rect rect, MapGraphEditorSession session, LevelBuildSettings settings)
        {
            EditorGUI.DrawRect(rect, LevelBlueprint.Background);
            if (settings == null)
            {
                DrawMessage(rect, "Pick the Level Build Settings to lay the graph out with.");
                return;
            }

            Rebuild(session, settings);
            if (_texture == null) return;
            if (_needsFrame) Frame(rect.size);

            GUI.BeginClip(rect);
            var local = new Rect(Vector2.zero, rect.size);
            Rect image = ImageRect(local);
            GUI.DrawTexture(image, _texture, ScaleMode.StretchToFill);
            DrawSelection(image, session);
            if (_zoom >= LabelMinZoom) DrawLabels(image);
            DrawFooter(local);
            HandleInput(local, image, session);
            GUI.EndClip();
        }

        public void Frame(Vector2 viewSize)
        {
            if (_texture == null) return;
            _zoom = Mathf.Min(viewSize.x / _texture.width, viewSize.y / _texture.height) * 0.95f;
            _pan  = Vector2.zero;
            _needsFrame = false;
        }

        private void Rebuild(MapGraphEditorSession session, LevelBuildSettings settings)
        {
            MapGraphAsset asset = session.Asset;
            if (_texture != null && _builtVersion == session.Version && _builtWith == settings
                && _builtFill == Fill && _builtAsset == asset) return;

            if (_builtAsset != asset) _needsFrame = true;
            _builtVersion = session.Version;
            _builtWith    = settings;
            _builtFill    = Fill;
            _builtAsset   = asset;

            Vector2 spacing = asset.Settings != null ? asset.Settings.NodeSpacing : Vector2.one;
            _layout = new LevelLayoutBuilder(settings).Build(session.Graph, spacing, Seed.From(asset.Seed), asset.Content);

            Color32[] pixels = LevelBlueprint.Render(_layout, Fill, out int width, out int height, out _tileOrigin);
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                if (_texture != null) Object.DestroyImmediate(_texture);
                _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode   = TextureWrapMode.Clamp,
                    hideFlags  = HideFlags.HideAndDontSave,
                };
            }
            _texture.SetPixels32(pixels);
            _texture.Apply();
        }

        private Rect ImageRect(Rect local)
        {
            Vector2 size = new Vector2(_texture.width, _texture.height) * _zoom;
            return new Rect(local.center + _pan - size * 0.5f, size);
        }

        // Texture pixel (y up) under a GUI point (y down), and back.
        private Vector2 ToPixel(Rect image, Vector2 point) =>
            new((point.x - image.x) / _zoom, (image.yMax - point.y) / _zoom);

        private Vector2 ToGui(Rect image, Vector2 tile)
        {
            Vector2 pixel = (tile - _tileOrigin) * LevelBlueprint.PixelsPerTile;
            return new Vector2(image.x + pixel.x * _zoom, image.yMax - pixel.y * _zoom);
        }

        private void DrawSelection(Rect image, MapGraphEditorSession session)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!_layout.Rooms.TryGetValue(session.SelectedNodeId, out LevelRoom room)) return;

            RectInt bounds = room.Footprint.Bounds;
            Vector2 min = ToGui(image, bounds.min), max = ToGui(image, bounds.max);
            var outline = Rect.MinMaxRect(min.x, max.y, max.x, min.y);
            Handles.DrawSolidRectangleWithOutline(outline, Color.clear, MapGraphStyle.Selection);
        }

        private void DrawLabels(Rect image)
        {
            if (Event.current.type != EventType.Repaint) return;
            _labelStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };

            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                Vector2 at = ToGui(image, room.Anchor);
                GUI.Label(new Rect(at.x - 70f, at.y - 9f, 140f, 18f), $"#{room.Node.Id} {room.DisplayName}", _labelStyle);
            }
        }

        private void DrawFooter(Rect local)
        {
            string text = $"Seed {_builtAsset.Seed} · {_layout.Rooms.Count} rooms · yellow = locked, purple = secret, cyan = one-way";
            if (_layout.Warnings.Count > 0) text += $" · {_layout.Warnings.Count} layout warning(s): {_layout.Warnings[0]}";
            GUI.Label(new Rect(6f, local.height - 20f, local.width - 12f, 18f), text, EditorStyles.whiteMiniLabel);
        }

        private void HandleInput(Rect local, Rect image, MapGraphEditorSession session)
        {
            Event e = Event.current;
            if (!local.Contains(e.mousePosition)) return;

            switch (e.type)
            {
                case EventType.ScrollWheel:
                    float factor = e.delta.y > 0f ? 1f / 1.1f : 1.1f;
                    Vector2 fromCentre = e.mousePosition - local.center - _pan;
                    _zoom = Mathf.Clamp(_zoom * factor, 0.1f, 20f);
                    _pan -= fromCentre * (factor - 1f);
                    e.Use();
                    break;

                case EventType.MouseDrag when e.button == 0 || e.button == 2:
                    _pan += e.delta;
                    e.Use();
                    break;

                case EventType.MouseDown when e.button == 0 && e.clickCount == 1:
                    LevelRoom room = LevelBlueprint.RoomAtPixel(_layout, _tileOrigin, ToPixel(image, e.mousePosition));
                    if (room != null) session.SelectNode(room.Node.Id);
                    else session.ClearSelection();
                    break;

                case EventType.KeyDown when e.keyCode == KeyCode.F && !EditorGUIUtility.editingTextField:
                    Frame(local.size);
                    e.Use();
                    break;
            }
        }

        private static void DrawMessage(Rect rect, string message)
        {
            GUILayout.BeginArea(new Rect(rect.center.x - 170f, rect.center.y - 25f, 340f, 50f));
            EditorGUILayout.HelpBox(message, MessageType.Info);
            GUILayout.EndArea();
        }
    }
}
