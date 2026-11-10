using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.Input;
using CGD.Player;

namespace CGD.UI
{
    // Placeholder prompt while the player is held in place: what is holding them, the key to
    // mash, and a bar that fills with every tap. Shows below the crosshair only while
    // restrained. Built-in layout only; no skin yet.
    [RequireComponent(typeof(RectTransform))]
    public class StruggleHUD : HUDElement
    {
        [SerializeField] private PlayerRestraint _restraint;

        [Header("Layout")]
        [SerializeField] private Vector2 _size = new(320f, 64f);
        [Tooltip("Pixels below the centre of the screen")]
        [SerializeField] private float _offsetBelowCentre = 150f;

        // Each tap swells the panel, which settles back.
        private const float PulseSize  = 0.06f;
        private const float PulseSpeed = 7f;

        private GameObject      _panel;
        private RectTransform   _panelRect;
        private TextMeshProUGUI _text;
        private Image           _bar;
        private float _pulse;
        private int   _shownTaps;

        private void Awake()
        {
            var self = (RectTransform)transform;
            self.anchorMin = self.anchorMax = self.pivot = new Vector2(0.5f, 0.5f);
            self.anchoredPosition = new Vector2(0f, -_offsetBelowCentre);
            self.sizeDelta = _size;

            Image background = UIFactory.MakeImage("Panel", self);
            background.color = Theme.Window;
            UIFactory.Stretch(background.rectTransform);
            _panel     = background.gameObject;
            _panelRect = background.rectTransform;

            _text = UIFactory.MakeText("Text", _panelRect);
            _text.fontSize  = 16f;
            _text.fontStyle = FontStyles.Bold;
            _text.color     = Theme.TextPrimary;
            _text.alignment = TextAlignmentOptions.Center;
            UIFactory.Place(_text.rectTransform, new Vector2(0f, -8f), new Vector2(_size.x, 26f));

            Image trough = UIFactory.MakeImage("Trough", _panelRect);
            trough.color = Theme.BarBackground;
            UIFactory.Place(trough.rectTransform, new Vector2(16f, -42f), new Vector2(_size.x - 32f, 10f));

            _bar = UIFactory.MakeImage("Fill", trough.rectTransform);
            _bar.color = Theme.Warning;
            RectTransform fill = _bar.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (Bindings != null) Bindings.Changed += RefreshText;
            if (_restraint != null) _restraint.Changed += RefreshText;
            RefreshText();
        }

        private void OnDisable()
        {
            if (Bindings != null) Bindings.Changed -= RefreshText;
            if (_restraint != null) _restraint.Changed -= RefreshText;
        }

        private void RefreshText()
        {
            if (_restraint == null) return;

            _text.text = $"{_restraint.Label.ToUpperInvariant()} — MASH {KeyOf(GameAction.Interact)}";
        }

        private void Update()
        {
            bool restrained = _restraint != null && _restraint.IsRestrained;
            if (_panel.activeSelf != restrained)
            {
                _panel.SetActive(restrained);
                _shownTaps = _restraint != null ? _restraint.TapCount : 0;
                _pulse = 0f;
            }

            if (!restrained) return;

            UIFill.Horizontal(_bar, _restraint.Progress);

            if (_restraint.TapCount != _shownTaps)
            {
                _shownTaps = _restraint.TapCount;
                _pulse = 1f;
            }
            _pulse = Mathf.MoveTowards(_pulse, 0f, PulseSpeed * Time.unscaledDeltaTime);
            _panelRect.localScale = Vector3.one * (1f + PulseSize * _pulse);
        }

        public override void Refresh() { }
    }
}
