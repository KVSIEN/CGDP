using CGD.Level;
using UnityEngine;

namespace CGD.Editor
{
    // A RoomSample drawn as a blueprint (LevelBlueprint, furniture included) into a texture,
    // and which part of it shows the room: `Frame` in tiles, `Uv` in texture coordinates.
    public sealed class RoomPlanTexture
    {
        // The blueprint keeps this much floor-free border too, so the frame stays on the texture.
        public const int MarginTiles = 2;

        private Texture2D _texture;
        private Vector2Int _origin;

        public Texture2D Texture => _texture;
        public RectInt   Frame   { get; private set; }
        public Rect      Uv      { get; private set; }

        public void Render(RoomSample sample, LevelBlueprint.Fill fill)
        {
            Color32[] pixels = LevelBlueprint.Render(sample.Layout, sample.Settings, fill, out int width, out int height,
                                                     out _origin, sample.Furnishing.Props);
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                Release();
                _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode   = TextureWrapMode.Clamp,
                    hideFlags  = HideFlags.HideAndDontSave,
                };
            }
            _texture.SetPixels32(pixels);
            _texture.Apply();

            Frame = sample.Frame(MarginTiles);
            float ppt = LevelBlueprint.PixelsPerTile;
            Uv = new Rect((Frame.xMin - _origin.x) * ppt / width, (Frame.yMin - _origin.y) * ppt / height,
                          Frame.width * ppt / width, Frame.height * ppt / height);
        }

        // The largest rect of `area` that shows the frame without stretching it.
        public Rect Fit(Rect area)
        {
            float scale = Mathf.Min(area.width / Frame.width, area.height / Frame.height);
            var size = new Vector2(Frame.width, Frame.height) * scale;
            return new Rect(area.center - size * 0.5f, size);
        }

        public void Draw(Rect rect)
        {
            if (_texture != null) GUI.DrawTextureWithTexCoords(rect, _texture, Uv);
        }

        public void Release()
        {
            if (_texture != null) Object.DestroyImmediate(_texture);
            _texture = null;
        }
    }
}
