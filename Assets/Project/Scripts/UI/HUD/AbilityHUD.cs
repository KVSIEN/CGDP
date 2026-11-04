using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Abilities;

namespace CGD.UI
{
    // Displays four ability slots anchored to the bottom-centre of the screen.
    // Each slot shows the ability's colour, a dark overlay that shrinks as the next
    // charge recharges (or grows while casting), and the charge count for multi-charge
    // abilities. A slot whose cost can't be paid yet is dimmed, and Surge abilities show
    // their own gauge as a thin bar along the slot's bottom edge.
    [RequireComponent(typeof(RectTransform))]
    public class AbilityHUD : HUDElement
    {
        [SerializeField] private PlayerAbilities _abilities;

        [Header("Layout")]
        [SerializeField] private float _slotSize          = 56f;
        [SerializeField] private float _slotGap           = 8f;
        [SerializeField] private float _screenPaddingBottom = 24f;

        private const int SlotCount = 4;

        private Image[]  _slotBg         = new Image[SlotCount];
        private Image[]  _cooldownOverlay = new Image[SlotCount];
        private TextMeshProUGUI[] _chargeLabels = new TextMeshProUGUI[SlotCount];
        private Image[]  _surgeBg   = new Image[SlotCount];
        private Image[]  _surgeFill = new Image[SlotCount];

        // Cached so the per-frame charge label never allocates.
        private static readonly string[] ChargeText = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private const float UnaffordableAlpha = 0.25f;
        private const float SurgeBarHeight = 5f;

        private void Awake()
        {
            PositionPanel();
            BuildSlots();
        }

        private void PositionPanel()
        {
            var rt           = GetComponent<RectTransform>();
            rt.anchorMin     = new Vector2(0.5f, 0f);
            rt.anchorMax     = new Vector2(0.5f, 0f);
            rt.pivot         = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, _screenPaddingBottom);
            rt.sizeDelta     = new Vector2(SlotCount * _slotSize + (SlotCount - 1) * _slotGap, _slotSize);
        }

        private void BuildSlots()
        {
            if (!BuildSkinnedSlots()) BuildDefaultSlots();
        }

        // With a skin, the skin is one slot: the element makes four, spaces them by the slot's
        // size and the layout gap, and sizes the bar to fit. The surge gauge is optional.
        private bool BuildSkinnedSlots()
        {
            if (!HasSkin) return false;

            var self = GetComponent<RectTransform>();
            Vector2 size = Vector2.one * _slotSize;

            for (int i = 0; i < SlotCount; i++)
            {
                SkinParts slot = NewSkinItem(self);
                _slotBg[i]          = slot.GetImage("Background");
                _cooldownOverlay[i] = slot.GetImage("Cooldown");
                _chargeLabels[i]    = slot.GetText("Charges");
                _surgeBg[i]         = slot.GetImage("Surge", optional: true);
                _surgeFill[i]       = slot.GetImage("SurgeFill", optional: true);
                if (!slot.Accept()) return false;

                size = slot.SizeOr(size);
                RectTransform rt = slot.Root;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                rt.anchoredPosition = new Vector2((i - (SlotCount - 1) * 0.5f) * (size.x + _slotGap), 0f);
            }

            self.sizeDelta = new Vector2(SlotCount * size.x + (SlotCount - 1) * _slotGap, size.y);
            return true;
        }

        private void BuildDefaultSlots()
        {
            var self = GetComponent<RectTransform>();

            for (int i = 0; i < SlotCount; i++)
            {
                // Horizontal centre of this slot relative to the panel centre
                float x = (i - (SlotCount - 1) * 0.5f) * (_slotSize + _slotGap);

                // Grey background
                var bg = UIFactory.MakeImage("Slot_" + i, self);
                bg.color = Theme.SlotEmpty;
                PlaceSquare(bg.rectTransform, x, _slotSize);
                _slotBg[i] = bg;

                // Dark overlay that covers the slot while on cooldown (child of the slot)
                var overlay = UIFactory.MakeImage("Overlay_" + i, bg.rectTransform);
                overlay.color = Theme.Overlay;
                UIFactory.Stretch(overlay.rectTransform);
                _cooldownOverlay[i] = overlay;

                var charges = UIFactory.MakeText("Charges_" + i, bg.rectTransform);
                charges.fontSize  = 12f;
                charges.alignment = TextAlignmentOptions.BottomRight;
                UIFactory.Stretch(charges.rectTransform);
                charges.rectTransform.offsetMax = new Vector2(-3f, 0f);
                _chargeLabels[i] = charges;

                var surgeBg = UIFactory.MakeImage("Surge_" + i, bg.rectTransform);
                surgeBg.color = Theme.Scrim;
                RectTransform surgeRt = surgeBg.rectTransform;
                surgeRt.anchorMin = Vector2.zero;
                surgeRt.anchorMax = new Vector2(1f, 0f);
                surgeRt.pivot     = new Vector2(0.5f, 0f);
                surgeRt.offsetMin = Vector2.zero;
                surgeRt.offsetMax = new Vector2(0f, SurgeBarHeight);
                _surgeBg[i] = surgeBg;

                var surgeFill = UIFactory.MakeImage("Fill", surgeRt);
                surgeFill.color = Theme.Reward;
                _surgeFill[i] = surgeFill;
                RectTransform fillRt = surgeFill.rectTransform;
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(0f, 1f);
                fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            }
        }

        private void Update()
        {
            if (_abilities == null) return;

            for (int i = 0; i < SlotCount; i++)
            {
                var ability = _abilities.Slots[i];

                // Tint the background with the ability's colour, or grey when empty
                _slotBg[i].color = ability != null
                    ? new Color(ability.SlotColor.r, ability.SlotColor.g, ability.SlotColor.b, _abilities.CanAfford(i) ? 0.85f : UnaffordableAlpha)
                    : Theme.SlotEmpty;

                // Shrink the overlay from top to bottom as the cooldown recovers.
                // anchorMax.y = 1 → overlay fills the slot (just used).
                // anchorMax.y = 0 → overlay is gone (ready).
                float overlay = _abilities.CastingSlot == i
                    ? _abilities.CastProgress
                    : 1f - (ability != null ? _abilities.GetReadyRatio(i) : 1f);
                UIFill.Vertical(_cooldownOverlay[i], overlay);

                if (_surgeBg[i] != null)
                {
                    bool hasSurge = ability != null && ability.Surge.IsActive;
                    if (_surgeBg[i].gameObject.activeSelf != hasSurge) _surgeBg[i].gameObject.SetActive(hasSurge);
                    if (hasSurge && _surgeFill[i] != null) UIFill.Horizontal(_surgeFill[i], _abilities.GetSurge(i));
                }

                _chargeLabels[i].text = ability != null && ability.MaxCharges > 1
                    ? ChargeLabel(_abilities.GetCharges(i))
                    : string.Empty;
            }
        }

        // HUDElement contract — this element drives itself in Update, so Refresh is a no-op.
        public override void Refresh() { }

        private static string ChargeLabel(int charges) =>
            charges < ChargeText.Length ? ChargeText[charges] : charges.ToString();

        private static void PlaceSquare(RectTransform rt, float centreX, float size)
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centreX, 0f);
            rt.sizeDelta        = Vector2.one * size;
        }

    }
}
