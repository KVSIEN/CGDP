using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Combat;
using CGD.Feedback;

namespace CGD.UI
{
    // Top-centre panel for the objective in progress (Siege, Gauntlet): title, a detail
    // line, a countdown and the health of whatever is being defended. Shows nothing while
    // no encounter has published an ObjectiveStatus.
    [RequireComponent(typeof(RectTransform))]
    public class ObjectiveHUD : HUDElement
    {
        [Header("Layout")]
        [SerializeField] private float _topPadding = 30f;
        [SerializeField] private float _width      = 360f;
        [SerializeField] private float _barHeight  = 12f;

        [Header("Colors")]
        [SerializeField] private Color _panel  = new(0f, 0f, 0f, 0.5f);
        [SerializeField] private Color _barBg  = new(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color _barFill = new(0.4f, 0.85f, 0.5f);

        private GameObject      _root;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _detail;
        private TextMeshProUGUI _timer;
        private RectTransform   _bar;
        private RectTransform   _barFillRect;
        private ObjectiveStatus _shown;
        private int             _shownSeconds = -1;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            self.anchorMin = self.anchorMax = self.pivot = new Vector2(0.5f, 1f);
            self.anchoredPosition = new Vector2(0f, -_topPadding);
            self.sizeDelta = new Vector2(_width, 0f);

            // The panel's content hides on its own; this object stays active so the HUD can
            // still show and hide it with the rest.
            var rootImage = UIFactory.MakeImage("Panel", self);
            rootImage.color = _panel;
            _root = rootImage.gameObject;
            var rootRect = rootImage.rectTransform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(_width, 66f + _barHeight);

            _title  = MakeLabel("Title", rootRect, 18f, TextAlignmentOptions.TopLeft, new Vector2(10f, -6f), new Vector2(-90f, -30f));
            _title.fontStyle = FontStyles.Bold;
            _timer  = MakeLabel("Timer", rootRect, 18f, TextAlignmentOptions.TopRight, new Vector2(_width - 90f, -6f), new Vector2(-10f, -30f));
            _detail = MakeLabel("Detail", rootRect, 14f, TextAlignmentOptions.TopLeft, new Vector2(10f, -32f), new Vector2(-10f, -52f));

            Image barBg = UIFactory.MakeImage("HealthBar", rootRect);
            barBg.color = _barBg;
            _bar = barBg.rectTransform;
            _bar.anchorMin = new Vector2(0f, 0f);
            _bar.anchorMax = new Vector2(1f, 0f);
            _bar.pivot     = new Vector2(0.5f, 0f);
            _bar.offsetMin = new Vector2(10f, 8f);
            _bar.offsetMax = new Vector2(-10f, 8f + _barHeight);

            Image fill = UIFactory.MakeImage("Fill", _bar);
            fill.color = _barFill;
            _barFillRect = fill.rectTransform;
            UIFactory.Stretch(_barFillRect);

            _root.SetActive(false);
        }

        private static TextMeshProUGUI MakeLabel(string name, RectTransform parent, float size, TextAlignmentOptions align, Vector2 min, Vector2 max)
        {
            TextMeshProUGUI text = UIFactory.MakeText(name, parent);
            text.fontSize  = size;
            text.alignment = align;
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(min.x, max.y);
            rt.offsetMax = new Vector2(max.x, min.y);
            return text;
        }

        private void OnEnable()
        {
            ObjectiveFeed.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => ObjectiveFeed.Changed -= Refresh;

        private void Update()
        {
            if (_shown == null) return;
            Redraw();
        }

        public override void Refresh()
        {
            _shown = ObjectiveFeed.Current;
            _root.SetActive(_shown != null);
            if (_shown == null) return;

            _title.text = _shown.Title;
            _shownSeconds = -1;
            Redraw();
        }

        private void Redraw()
        {
            _detail.text = _shown.Detail;

            int seconds = _shown.TimeRemaining < 0f ? -1 : Mathf.CeilToInt(_shown.TimeRemaining);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _timer.text = seconds < 0 ? string.Empty : $"{seconds / 60}:{seconds % 60:00}";
            }

            HealthManager health = _shown.Health;
            _bar.gameObject.SetActive(health != null);
            if (health == null) return;

            float fraction = health.MaxHealth > 0f ? Mathf.Clamp01(health.Health / health.MaxHealth) : 0f;
            _barFillRect.anchorMax = new Vector2(fraction, 1f);
        }
    }
}
