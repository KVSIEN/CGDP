using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.UI;

namespace CGD.DevTools
{
    // The look shared by the spectator's pop-up windows (scenario list, squad sizes, settings):
    // a dark centred panel that catches clicks, with muted secondary text.
    internal static class SpectatorWindow
    {
        public static readonly Color Background = new(0.07f, 0.08f, 0.12f, 0.96f);
        public static readonly Color MutedText  = new(0.6f, 0.63f, 0.7f, 1f);

        // A window of `size`, centred on `parent`.
        public static RectTransform Centred(string name, RectTransform parent, Vector2 size)
        {
            Image window = UIFactory.MakeImage(name, parent);
            window.color = Background;
            window.raycastTarget = true;
            RectTransform rect = window.rectTransform;
            UIFactory.Pin(rect, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return rect;
        }

        // Left-aligned text placed at `position` (from the window's top left) and `extent` in size.
        public static TextMeshProUGUI Label(RectTransform parent, string name, string value, float size, Vector2 position, Vector2 extent)
        {
            TextMeshProUGUI text = UIFactory.MakeLabel(name, parent, value, size, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(text.rectTransform, position, extent);
            return text;
        }
    }
}
