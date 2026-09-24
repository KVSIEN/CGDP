using UnityEngine;

namespace CGD.UI
{
    // Small white shapes generated once at runtime, so code-built UI (map icons, masks)
    // needs no imported art. Tint them with Image.color.
    public static class ProceduralSprites
    {
        private const int Size = 64;

        private static Sprite _circle;
        private static Sprite _square;
        private static Sprite _diamond;
        private static Sprite _arrow;

        public static Sprite Circle  => Get(ref _circle,  "Circle",  (x, y) => x * x + y * y <= 1f);
        public static Sprite Square  => Get(ref _square,  "Square",  (x, y) => true);
        public static Sprite Diamond => Get(ref _diamond, "Diamond", (x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 1f);
        // Points up; an arrowhead with a notched tail.
        public static Sprite Arrow   => Get(ref _arrow,   "Arrow",   (x, y) => y >= -0.8f && Mathf.Abs(x) <= (1f - y) * 0.5f && !(y < -0.3f && Mathf.Abs(x) < (-0.3f - y) * 0.9f));

        // Unity's null check, not ??=, so a destroyed sprite is rebuilt.
        private static Sprite Get(ref Sprite cache, string name, System.Func<float, float, bool> inside)
        {
            if (cache == null) cache = Make(name, inside);
            return cache;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _circle = _square = _diamond = _arrow = null;

        // inside(x, y) with x, y in -1..1; edges get one pixel of anti-aliasing via 4x supersampling.
        private static Sprite Make(string name, System.Func<float, float, bool> inside)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            var pixels  = new Color32[Size * Size];

            for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                int hits = 0;
                for (int s = 0; s < 4; s++)
                {
                    float x = (px + 0.25f + 0.5f * (s & 1)) / Size * 2f - 1f;
                    float y = (py + 0.25f + 0.5f * (s >> 1)) / Size * 2f - 1f;
                    if (inside(x, y)) hits++;
                }
                pixels[py * Size + px] = new Color32(255, 255, 255, (byte)(hits * 255 / 4));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }
    }
}
