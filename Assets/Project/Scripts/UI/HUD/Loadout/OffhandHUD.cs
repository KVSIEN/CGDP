using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Artifacts;
using CGD.Input;
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
        private const float SlotAlpha = 0.85f;
        private static readonly string[] ChargeText = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private GameObject      _slot;
        private Image           _bg;
        private Image           _overlay;
        private TextMeshProUGUI _keyLabel;
        private string _useKey  = string.Empty;
        private string _holdKey = string.Empty;
        private TextMeshProUGUI _nameLabel;
        private TextMeshProUGUI _chargeLabel;
        private string          _shownName;

        private void OnEnable()
        {
            if (Bindings != null) Bindings.Changed += RefreshKeys;
            RefreshKeys();
        }

        private void OnDisable()
        {
            if (Bindings != null) Bindings.Changed -= RefreshKeys;
        }

        // The artifact is used with the aim button; the held text is built here, not every frame.
        private void RefreshKeys()
        {
            _useKey  = KeyOf(GameAction.AimDownSights);
            _holdKey = _useKey + " hold";
        }

        private void Awake()
        {
            var rt              = GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(-_offsetFromCenter, _screenPaddingBottom);
            rt.sizeDelta        = Vector2.one * _slotSize;

            SkinParts skin = UseSkin();
            if (skin != null)
            {
                _bg          = skin.GetImage("Background");
                _overlay     = skin.GetImage("Cooldown");
                _keyLabel    = skin.GetText("Key");
                _nameLabel   = skin.GetText("Name");
                _chargeLabel = skin.GetText("Charges");
                if (skin.Accept())
                {
                    rt.sizeDelta = skin.SizeOr(rt.sizeDelta);
                    _slot = skin.Root.gameObject;
                    _slot.SetActive(false);
                    return;
                }
            }

            _bg       = UIFactory.MakeImage("OffhandBg", rt);
            _bg.color = ReadyColor;
            UIFactory.Stretch(_bg.rectTransform);
            _slot = _bg.gameObject;

            _overlay       = UIFactory.MakeImage("OffhandOverlay", _bg.rectTransform);
            _overlay.color = Theme.Overlay;
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

            string key = use.Mode == OffhandUseMode.Hold ? _holdKey : _useKey;
            int charges = use.Charges;
            Show(use.IsRunning ? Theme.Warning.WithAlpha(SlotAlpha) : ReadyColor, use.Ratio, key,
                 charges >= 0 && charges < ChargeText.Length ? ChargeText[charges] : string.Empty);
        }

        private void Show(Color color, float ratio, string key, string charges)
        {
            _bg.color = color;
            UIFill.Vertical(_overlay, 1f - Mathf.Clamp01(ratio));
            if (_keyLabel.text    != key)     _keyLabel.text    = key;
            if (_chargeLabel.text != charges) _chargeLabel.text = charges;
        }

        public override void Refresh() { }
    }
}
