using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.UI;

namespace CGD.DevTools
{
    // The spectator's own HUD (the game HUD is usually switched off while spectating):
    //   top bar      the camera mode and whom it follows, and the game speed / pause
    //   controls     the controls, from the live bindings (H to hide)
    //   agent panel  the followed agent's full status, as the Behavior Tree Editor shows it
    //   toast        a short confirmation when something changes (speed, labels…)
    //   hint         the two keys to remember (help, settings)
    // Built at runtime on its own overlay canvas, which the settings panel also uses.
    internal sealed class SpectatorHud
    {
        private const float ToastSeconds = 1.6f;
        private const float ToastFade    = 0.4f;
        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        private static readonly Color PanelColor = new(0.05f, 0.06f, 0.09f, 0.78f);

        private readonly GameObject      _root;
        private readonly TextMeshProUGUI _mode;
        private readonly TextMeshProUGUI _time;
        private readonly GameObject      _helpPanel;
        private readonly TextMeshProUGUI _help;
        private readonly GameObject      _focusPanel;
        private readonly TextMeshProUGUI _focus;
        private readonly CanvasGroup     _toastGroup;
        private readonly TextMeshProUGUI _toast;
        private readonly TextMeshProUGUI _hint;
        private float _toastUntil;

        public SpectatorHud(Transform parent)
        {
            _root = new GameObject("Spectator HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;   // over the agent labels
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight  = 0.5f;
            Root = (RectTransform)_root.transform;

            // Top bar
            Image bar = Panel("Top Bar", new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(760f, 46f));
            _mode = MakeLabel("Mode", bar.rectTransform, 20f, TextAlignmentOptions.MidlineLeft);
            Inset(_mode.rectTransform, 18f, 6f);
            _time = MakeLabel("Time", bar.rectTransform, 20f, TextAlignmentOptions.MidlineRight);
            Inset(_time.rectTransform, 18f, 6f);

            // Controls (left)
            Image help = Panel("Controls", new Vector2(0f, 1f), new Vector2(16f, 16f), new Vector2(400f, 470f));
            _helpPanel = help.gameObject;
            _help = MakeLabel("Text", help.rectTransform, 15f, TextAlignmentOptions.TopLeft);
            Inset(_help.rectTransform, 16f, 12f);

            // Followed agent (right)
            Image focus = Panel("Agent", new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(400f, 380f));
            _focusPanel = focus.gameObject;
            _focus = MakeLabel("Text", focus.rectTransform, 15f, TextAlignmentOptions.TopLeft);
            Inset(_focus.rectTransform, 16f, 12f);
            _focusPanel.SetActive(false);

            // Toast
            Image toast = Panel("Toast", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(460f, 44f));
            _toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;
            _toast = MakeLabel("Text", toast.rectTransform, 19f, TextAlignmentOptions.Center);
            Inset(_toast.rectTransform, 12f, 4f);

            // Hint (bottom)
            _hint = MakeLabel("Hint", Root, 15f, TextAlignmentOptions.Bottom);
            RectTransform hint = _hint.rectTransform;
            hint.anchorMin = hint.anchorMax = hint.pivot = new Vector2(0.5f, 0f);
            hint.anchoredPosition = new Vector2(0f, 16f);
            hint.sizeDelta = new Vector2(1200f, 28f);
            _hint.color = new Color(1f, 1f, 1f, 0.75f);
        }

        // The canvas the settings panel is built on (so it shares the raycaster and scaling).
        public RectTransform Root { get; }

        public void SetVisible(bool visible) => _root.SetActive(visible);

        public void SetMode(string text) => _mode.text = text;
        public void SetTime(string text) => _time.text = text;
        public void SetHint(string text) => _hint.text = text;

        public void SetHelp(string text, bool visible)
        {
            _helpPanel.SetActive(visible);
            if (visible) _help.text = text;
        }

        public void ShowFocus(string text)
        {
            _focusPanel.SetActive(true);
            _focus.text = text;
        }

        public void HideFocus() => _focusPanel.SetActive(false);

        public void Toast(string message, float now)
        {
            _toast.text = message;
            _toastUntil = now + ToastSeconds;
            _toastGroup.alpha = 1f;
        }

        public void Tick(float now)
        {
            float left = _toastUntil - now;
            _toastGroup.alpha = left <= 0f ? 0f : Mathf.Clamp01(left / ToastFade);
        }

        private Image Panel(string name, Vector2 corner, Vector2 padding, Vector2 size)
        {
            Image panel = UIFactory.MakeImage(name, Root);
            panel.color = PanelColor;
            UIFactory.AnchorToCorner(panel.rectTransform, corner, padding);
            panel.rectTransform.sizeDelta = size;
            return panel;
        }

        private static TextMeshProUGUI MakeLabel(string name, RectTransform parent, float size, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = UIFactory.MakeText(name, parent);
            text.fontSize  = size;
            text.alignment = alignment;
            text.richText  = true;
            return text;
        }

        private static void Inset(RectTransform rect, float x, float y)
        {
            UIFactory.Stretch(rect);
            rect.offsetMin = new Vector2(x, y);
            rect.offsetMax = new Vector2(-x, -y);
        }
    }
}
