using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Displays four ability slots anchored to the bottom-centre of the screen.
// Each slot shows the ability's colour and a dark overlay that shrinks as
// the cooldown expires.
[RequireComponent(typeof(RectTransform))]
public class AbilityHUD : HUDElement
{
    [SerializeField] private PlayerAbilities _abilities;

    [Header("Layout")]
    [SerializeField] private float _slotSize          = 56f;
    [SerializeField] private float _slotGap           = 8f;
    [SerializeField] private float _screenPaddingBottom = 24f;

    private const int SlotCount = 4;

    private Image[]            _slotBg          = new Image[SlotCount];
    private Image[]            _cooldownOverlay  = new Image[SlotCount];
    private TextMeshProUGUI[]  _keyLabels        = new TextMeshProUGUI[SlotCount];

    private static readonly string[] KeyNames = { "1", "2", "3", "4" };
    private static readonly Color    EmptySlotColor   = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    private static readonly Color    CooldownOverlayColor = new Color(0f, 0f, 0f, 0.65f);

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
        var self = GetComponent<RectTransform>();

        for (int i = 0; i < SlotCount; i++)
        {
            // Horizontal centre of this slot relative to the panel centre
            float x = (i - (SlotCount - 1) * 0.5f) * (_slotSize + _slotGap);

            // Grey background
            var bg = MakeImage("Slot_" + i, self);
            bg.color = EmptySlotColor;
            PlaceSquare(bg.rectTransform, x, _slotSize);
            _slotBg[i] = bg;

            // Dark overlay that covers the slot while on cooldown (child of the slot)
            var overlay = MakeImage("Overlay_" + i, bg.rectTransform);
            overlay.color = CooldownOverlayColor;
            StretchToParent(overlay.rectTransform);
            _cooldownOverlay[i] = overlay;

            // Key number in the top-left corner
            var key = MakeText("Key_" + i, bg.rectTransform);
            key.text      = KeyNames[i];
            key.fontSize  = 11f;
            key.color     = new Color(1f, 1f, 1f, 0.7f);
            key.alignment = TextAlignmentOptions.TopLeft;
            StretchToParent(key.rectTransform);
            key.rectTransform.offsetMin = new Vector2(4f, 0f);
            key.rectTransform.offsetMax = new Vector2(0f, -3f);
            _keyLabels[i] = key;
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
                ? new Color(ability.SlotColor.r, ability.SlotColor.g, ability.SlotColor.b, 0.85f)
                : EmptySlotColor;

            // Shrink the overlay from top to bottom as the cooldown recovers.
            // anchorMax.y = 1 → overlay fills the slot (just used).
            // anchorMax.y = 0 → overlay is gone (ready).
            float readyRatio = ability != null ? _abilities.GetReadyRatio(i) : 1f;
            _cooldownOverlay[i].rectTransform.anchorMax = new Vector2(1f, 1f - readyRatio);
        }
    }

    // HUDElement contract — this element drives itself in Update, so Refresh is a no-op.
    public override void Refresh() { }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void PlaceSquare(RectTransform rt, float centreX, float size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(centreX, 0f);
        rt.sizeDelta        = Vector2.one * size;
    }

    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image MakeImage(string elementName, RectTransform parent)
    {
        var go = new GameObject(elementName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string elementName, RectTransform parent)
    {
        var go = new GameObject(elementName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }
}
