using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Artifacts;
using CGD.Player;

namespace CGD.UI
{
    // The held artifact's slot, left of the ability bar: its name, how it is used (tap or hold on
    // the aim input), a dark overlay that shrinks as it becomes ready (or shows the drain meter
    // while a hold is up), and the charge count for a use with charges. Shown only while the
    // artifact is in use — in the offhand with a main hand that leaves room for it.
    [RequireComponent(typeof(RectTransform))]
    public class OffhandHUD : HUDElement
    {
        [SerializeField] private PlayerOffhand _offhand;

        [Header("Layout")]
        [SerializeField] private float _slotSize            = 56f;
        [SerializeField] private float _screenPaddingBottom = 24f;
        [SerializeField] private float _offsetFromCenter    = 160f;

        private static readonly Color ReadyColor   = new Color(0.45f, 0.25f, 0.65f, 0.85f);
        private static readonly Color PassiveColor = new Color(0.25f, 0.2f,  0.3f,  0.85f);
        private static readonly Color RunningColor = new Color(1f,    0.75f, 0.1f,  0.85f);
        private static readonly Color OverlayColor = new Color(0f,    0f,    0f,    0.65f);
        private static readonly string[] ChargeText = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private GameObject      _slot;
        private Image           _bg;
        private Image           _overlay;
        private TextMeshProUGUI _keyLabel;
        private TextMeshProUGUI _nameLabel;
        private TextMeshProUGUI _chargeLabel;
        private string          _shownName;

        private void Awake()
        {
            var rt              = GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(-_offsetFromCenter, _screenPaddingBottom);
            rt.sizeDelta        = Vector2.one * _slotSize;

            _bg       = UIFactory.MakeImage("OffhandBg", rt);
            _bg.color = ReadyColor;
            UIFactory.Stretch(_bg.rectTransform);
            _slot = _bg.gameObject;

            _overlay       = UIFactory.MakeImage("OffhandOverlay", _bg.rectTransform);
            _overlay.color = OverlayColor;
            UIFactory.Stretch(_overlay.rectTransform);

            _keyLabel           = Label("OffhandKey", 11f, TextAlignmentOptions.TopLeft, 0.7f);
            _keyLabel.rectTransform.offsetMin = new Vector2(4f, 0f);
            _keyLabel.rectTransform.offsetMax = new Vector2(0f, -3f);

            _nameLabel = Label("OffhandName", 9f, TextAlignmentOptions.Center, 0.85f);

            _chargeLabel = Label("OffhandCharges", 13f, TextAlignmentOptions.BottomRight, 1f);
            _chargeLabel.rectTransform.offsetMin = new Vector2(0f, 3f);
            _chargeLabel.rectTransform.offsetMax = new Vector2(-4f, 0f);

            _slot.SetActive(false);
        }

        private TextMeshProUGUI Label(string name, float size, TextAlignmentOptions alignment, float alpha)
        {
            TextMeshProUGUI label = UIFactory.MakeText(name, _bg.rectTransform);
            label.fontSize  = size;
            label.color     = new Color(1f, 1f, 1f, alpha);
            label.alignment = alignment;
            UIFactory.Stretch(label.rectTransform);
            return label;
        }

        private void Update()
        {
            ArtifactInstance artifact = _offhand != null ? _offhand.Artifact : null;
            if (_slot.activeSelf != (artifact != null)) _slot.SetActive(artifact != null);
            if (artifact == null) return;

            string label = artifact.DisplayName.ToUpperInvariant();
            if (_shownName != label)
            {
                _shownName      = label;
                _nameLabel.text = label;
            }

            OffhandUse use = artifact.Use;
            if (use == null)
            {
                Show(PassiveColor, 1f, string.Empty, string.Empty);
                return;
            }

            string key = use.Mode == OffhandUseMode.Hold ? "RMB hold" : "RMB";
            int charges = use.Charges;
            Show(use.IsRunning ? RunningColor : ReadyColor, use.Ratio, key,
                 charges >= 0 && charges < ChargeText.Length ? ChargeText[charges] : string.Empty);
        }

        private void Show(Color color, float ratio, string key, string charges)
        {
            _bg.color = color;
            _overlay.rectTransform.anchorMax = new Vector2(1f, 1f - Mathf.Clamp01(ratio));
            if (_keyLabel.text    != key)     _keyLabel.text    = key;
            if (_chargeLabel.text != charges) _chargeLabel.text = charges;
        }

        public override void Refresh() { }
    }
}
