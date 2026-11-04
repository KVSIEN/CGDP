using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Input;
using CGD.Player;

namespace CGD.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class DodgeHUD : HUDElement
    {
        [SerializeField] private PlayerDodge _dodge;

        [Header("Layout")]
        [SerializeField] private float _slotSize            = 56f;
        [SerializeField] private float _screenPaddingBottom = 24f;
        [SerializeField] private float _offsetFromCenter    = 160f;

        // Opacity of the slot background in every state.
        private const float SlotAlpha = 0.85f;

        private Image          _bg;
        private Image          _overlay;
        private TextMeshProUGUI _nameLabel;
        private TextMeshProUGUI _keyText;

        private void OnEnable()
        {
            if (Bindings != null) Bindings.Changed += RefreshKey;
            RefreshKey();
        }

        private void OnDisable()
        {
            if (Bindings != null) Bindings.Changed -= RefreshKey;
        }

        private void RefreshKey() => _keyText.text = KeyOf(GameAction.Dodge);

        private void Awake()
        {
            var rt              = GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(_offsetFromCenter, _screenPaddingBottom);
            rt.sizeDelta        = Vector2.one * _slotSize;

            _bg          = UIFactory.MakeImage("DodgeBg", rt);
            _bg.color    = Theme.Accent.WithAlpha(SlotAlpha);
            UIFactory.Stretch(_bg.rectTransform);

            _overlay       = UIFactory.MakeImage("DodgeOverlay", _bg.rectTransform);
            _overlay.color = Theme.Overlay;
            UIFactory.Stretch(_overlay.rectTransform);

            var keyLabel         = UIFactory.MakeText("DodgeKey", _bg.rectTransform);
            _keyText             = keyLabel;
            keyLabel.fontSize    = 11f;
            keyLabel.color       = Theme.TextSecondary;
            keyLabel.alignment   = TextAlignmentOptions.TopLeft;
            UIFactory.Stretch(keyLabel.rectTransform);
            keyLabel.rectTransform.offsetMin = new Vector2(4f, 0f);
            keyLabel.rectTransform.offsetMax = new Vector2(0f, -3f);

            _nameLabel           = UIFactory.MakeText("DodgeName", _bg.rectTransform);
            _nameLabel.text      = "DODGE";
            _nameLabel.fontSize  = 9f;
            _nameLabel.color     = Theme.TextPrimary;
            _nameLabel.alignment = TextAlignmentOptions.Center;
            UIFactory.Stretch(_nameLabel.rectTransform);
        }

        private void Update()
        {
            if (_dodge == null) return;

            if (_dodge.IsDodging)
            {
                _bg.color = _dodge.IsCommitted ? Theme.Positive.WithAlpha(SlotAlpha) : Theme.Warning.WithAlpha(SlotAlpha);
                _overlay.rectTransform.anchorMax = new Vector2(1f, 0f);
            }
            else
            {
                float ratio = _dodge.Cooldown.Ratio;
                _bg.color = Color.Lerp(Theme.SlotEmpty, Theme.Accent.WithAlpha(SlotAlpha), ratio);
                _overlay.rectTransform.anchorMax = new Vector2(1f, 1f - ratio);
            }

            string label = _dodge.Label.ToUpperInvariant();
            if (_nameLabel.text != label) _nameLabel.text = label;
        }

        public override void Refresh() { }
    }
}
