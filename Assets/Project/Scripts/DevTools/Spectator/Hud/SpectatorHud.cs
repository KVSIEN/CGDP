using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.UI;

namespace CGD.DevTools
{
    // The spectator's own HUD (the game HUD is usually switched off while spectating):
    //   top bar      the camera mode and whom it follows, and the game speed / pause
    //   overlays     which world overlays are on (senses, cover, paths & plans)
    //   controls     the controls, from the live bindings (H to hide)
    //   inspector    the inspected agent's panel (AgentInspector), as tall as its page
    //   match        the match stats (bottom left, M to hide)
    //   toast        a short confirmation when something changes (speed, labels…)
    //   hint         the two keys to remember (help, settings)
    // Built at runtime on its own overlay canvas, which the settings panel also uses.
    internal sealed class SpectatorHud
    {
        private const float ToastSeconds = 1.6f;
        private const float ToastFade    = 0.4f;
        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        private static readonly Color PanelColor = new(0.05f, 0.06f, 0.09f, 0.78f);
        private const float InspectorWidth = 460f;
        private const float StatsWidth     = 640f;

        private readonly GameObject      _root;
        private readonly TextMeshProUGUI _mode;
        private readonly TextMeshProUGUI _time;
        private readonly GameObject      _helpPanel;
        private readonly TextMeshProUGUI _help;
        private readonly TextMeshProUGUI _overlays;
        private readonly HudTextPanel    _inspector;
        private readonly HudTextPanel    _stats;
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

            // Overlays (under the top bar)
            Image overlays = Panel("Overlays", new Vector2(0.5f, 1f), new Vector2(0f, 64f), new Vector2(760f, 28f));
            _overlays = MakeLabel("Text", overlays.rectTransform, 14f, TextAlignmentOptions.Center);
            Inset(_overlays.rectTransform, 12f, 2f);

            // Controls (left)
            Image help = Panel("Controls", new Vector2(0f, 1f), new Vector2(16f, 16f), new Vector2(400f, 600f));
            _helpPanel = help.gameObject;
            _help = MakeLabel("Text", help.rectTransform, 15f, TextAlignmentOptions.TopLeft);
            Inset(_help.rectTransform, 16f, 12f);

            // Inspected agent (right)
            Image inspector = Panel("Inspector", new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(InspectorWidth, 380f));
            _inspector = new HudTextPanel(inspector, MakeLabel("Text", inspector.rectTransform, 14f, TextAlignmentOptions.TopLeft),
                                          InspectorWidth, 900f);

            // Match stats (bottom left, above the hint)
            Image stats = Panel("Match Stats", new Vector2(0f, 0f), new Vector2(16f, 52f), new Vector2(StatsWidth, 120f));
            _stats = new HudTextPanel(stats, MakeLabel("Text", stats.rectTransform, 14f, TextAlignmentOptions.TopLeft),
                                      StatsWidth, 420f);

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
        public void SetOverlays(string text) => _overlays.text = text;

        public void SetHelp(string text, bool visible)
        {
            _helpPanel.SetActive(visible);
            if (visible) _help.text = text;
        }

        public void ShowInspector(string text) => _inspector.Show(text);
        public void HideInspector()            => _inspector.Hide();
        public void ShowStats(string text)     => _stats.Show(text);
        public void HideStats()                => _stats.Hide();

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
            panel.raycastTarget = true;   // a click on a panel isn't a click on the arena behind it
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
