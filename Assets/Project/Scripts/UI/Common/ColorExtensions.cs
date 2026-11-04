using UnityEngine;

namespace CGD.UI
{
    public static class ColorExtensions
    {
        // The same colour with a different opacity.
        public static Color WithAlpha(this Color color, float alpha) =>
            new(color.r, color.g, color.b, alpha);
    }
}
