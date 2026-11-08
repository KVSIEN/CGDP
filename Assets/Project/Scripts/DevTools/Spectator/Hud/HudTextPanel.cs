using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.UI;

namespace CGD.DevTools
{
    // A HUD panel of rich text that grows and shrinks with what it shows (between a minimum
    // and maximum height), anchored to a screen corner so it grows away from it — the
    // inspector and the match stats.
    internal sealed class HudTextPanel
    {
        private const float PaddingX = 16f;
        private const float PaddingY = 12f;
        private const float MinHeight = 60f;

        private readonly RectTransform   _panel;
        private readonly TextMeshProUGUI _text;
        private readonly float _width;
        private readonly float _maxHeight;

        public HudTextPanel(Image background, TextMeshProUGUI text, float width, float maxHeight)
        {
            _panel     = background.rectTransform;
            _text      = text;
            _width     = width;
            _maxHeight = maxHeight;
            UIFactory.Stretch(_text.rectTransform);
            _text.rectTransform.offsetMin = new Vector2(PaddingX, PaddingY);
            _text.rectTransform.offsetMax = new Vector2(-PaddingX, -PaddingY);
            Hide();
        }

        public void Show(string text)
        {
            _panel.gameObject.SetActive(true);
            _text.text = text;
            float height = _text.GetPreferredValues(text, _width - PaddingX * 2f, 0f).y + PaddingY * 2f + 4f;
            _panel.sizeDelta = new Vector2(_width, Mathf.Clamp(height, MinHeight, _maxHeight));
        }

        public void Hide() => _panel.gameObject.SetActive(false);
    }
}
