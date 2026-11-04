using UnityEngine;

namespace CGD.UI
{
    // The HUD canvas scales from a 1920 x 1080 reference (CanvasScaler: Scale With Screen Size,
    // match 0.5). Canvases that are not under it, such as the world-space prompt and damage
    // numbers, multiply their pixel sizes by Factor so they stay in step with the HUD.
    public static class UIScale
    {
        private const float ReferenceWidth  = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float Match           = 0.5f;

        // Same blend CanvasScaler uses: a logarithmic mix of the width and height ratios.
        public static float Factor
        {
            get
            {
                float width  = Mathf.Max(Screen.width, 1) / ReferenceWidth;
                float height = Mathf.Max(Screen.height, 1) / ReferenceHeight;
                return Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(width, 2f), Mathf.Log(height, 2f), Match));
            }
        }
    }
}
