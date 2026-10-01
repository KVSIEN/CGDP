using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Player;

namespace CGD.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class DodgeHUD : HUDElement
    {
        [SerializeField] private PlayerDodge _dodge;
        [SerializeField] private string _keyLabel = "C";

        [Header("Layout")]
        [SerializeField] private float _slotSize            = 56f;
        [SerializeField] private float _screenPaddingBottom = 24f;
        [SerializeField] private float _offsetFromCenter    = 160f;

        private static readonly Color ReadyColor    = new Color(0.25f, 0.55f, 1f,  0.85f);
        private static readonly Color CooldownColor = new Color(0.1f,  0.1f,  0.1f, 0.85f);
        // While dodging: amber when you can still act, green while committed (roll, dash).
        private static readonly Color FreeColor      = new Color(1f,    0.75f, 0.1f, 0.85f);
        private static readonly Color CommittedColor = new Color(0.15f, 0.9f,  0.4f, 0.85f);
        private static readonly Color OverlayColor  = new Color(0f,    0f,    0f,   0.65f);

        private Image          _bg;
        private Image          _overlay;
        private TextMeshProUGUI _nameLabel;

        private void Awake()
        {
            var rt              = GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(_offsetFromCenter, _screenPaddingBottom);
            rt.sizeDelta        = Vector2.one * _slotSize;

            _bg          = UIFactory.MakeImage("DodgeBg", rt);
            _bg.color    = ReadyColor;
            UIFactory.Stretch(_bg.rectTransform);

            _overlay       = UIFactory.MakeImage("DodgeOverlay", _bg.rectTransform);
            _overlay.color = OverlayColor;
            UIFactory.Stretch(_overlay.rectTransform);

            var keyLabel         = UIFactory.MakeText("DodgeKey", _bg.rectTransform);
            keyLabel.text        = _keyLabel;
            keyLabel.fontSize    = 11f;
            keyLabel.color       = new Color(1f, 1f, 1f, 0.7f);
            keyLabel.alignment   = TextAlignmentOptions.TopLeft;
            UIFactory.Stretch(keyLabel.rectTransform);
            keyLabel.rectTransform.offsetMin = new Vector2(4f, 0f);
            keyLabel.rectTransform.offsetMax = new Vector2(0f, -3f);

            _nameLabel           = UIFactory.MakeText("DodgeName", _bg.rectTransform);
            _nameLabel.text      = "DODGE";
            _nameLabel.fontSize  = 9f;
            _nameLabel.color     = new Color(1f, 1f, 1f, 0.85f);
            _nameLabel.alignment = TextAlignmentOptions.Center;
            UIFactory.Stretch(_nameLabel.rectTransform);
        }

        private void Update()
        {
            if (_dodge == null) return;

            if (_dodge.IsDodging)
            {
                _bg.color = _dodge.IsCommitted ? CommittedColor : FreeColor;
                _overlay.rectTransform.anchorMax = new Vector2(1f, 0f);
            }
            else
            {
                float ratio = _dodge.Cooldown.Ratio;
                _bg.color = Color.Lerp(CooldownColor, ReadyColor, ratio);
                _overlay.rectTransform.anchorMax = new Vector2(1f, 1f - ratio);
            }

            string label = _dodge.Label.ToUpperInvariant();
            if (_nameLabel.text != label) _nameLabel.text = label;
        }

        public override void Refresh() { }
    }
}
