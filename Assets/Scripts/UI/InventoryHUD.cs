using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class InventoryHUD : HUDElement
{
    [SerializeField] private PlayerInputHandler  _input;
    [SerializeField] private PlayerWeaponLoadout _loadout;

    [Header("Layout")]
    [SerializeField] private float _panelWidth = 300f;
    [SerializeField] private float _rowHeight  = 52f;
    [SerializeField] private float _rowGap     = 6f;
    [SerializeField] private float _padding    = 16f;

    private const int SlotCount = 4;
    private static readonly string[]     SlotKeys    = { "1", "2", "3", "4" };
    private static readonly GameAction[] SlotActions =
    {
        GameAction.Weapon1,
        GameAction.Weapon2,
        GameAction.Weapon3,
        GameAction.Weapon4,
    };

    private static readonly Color PanelBg  = new Color(0.05f, 0.05f, 0.08f, 0.93f);
    private static readonly Color SlotBg   = new Color(0.12f, 0.12f, 0.18f, 0.88f);
    private static readonly Color ActiveBg = new Color(0.22f, 0.42f, 0.82f, 0.92f);
    private static readonly Color EmptyBg  = new Color(0.08f, 0.08f, 0.10f, 0.72f);

    private Image[]           _slotBgs     = new Image[SlotCount];
    private TextMeshProUGUI[] _slotNames   = new TextMeshProUGUI[SlotCount];
    private Image[]           _barSlotBgs  = new Image[SlotCount];
    private TextMeshProUGUI   _activeBarText;
    private CanvasGroup       _cg;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        BuildPanel();
        BuildActiveBar();
        SetPanelAlpha(false);
        // IsVisible starts true in HUDElement — sync it
        IsVisible = false;
    }

    private void Update()
    {
        // WasPressedRaw bypasses InputEnabled so the key works even while input is locked
        if (_input != null && _input.WasPressedRaw(GameAction.Inventory))
            Toggle();

        if (_loadout == null) return;

        int active = _loadout.ActiveSlot;
        for (int i = 0; i < SlotCount; i++)
        {
            var  weapon   = _loadout.Slots[i];
            bool isActive = active == i;

            // Always update the compact bar
            _barSlotBgs[i].color = isActive ? ActiveBg : (weapon != null ? SlotBg : EmptyBg);
            if (isActive)
                _activeBarText.text = weapon != null ? weapon.WeaponName : "— Empty —";

            if (!IsVisible) continue;

            if (_input.WasPressedRaw(SlotActions[i]))
                _loadout.EquipSlot(i);

            _slotBgs[i].color  = isActive ? ActiveBg : (weapon != null ? SlotBg : EmptyBg);
            _slotNames[i].text = weapon != null ? weapon.WeaponName : "— Empty —";
        }
    }

    // ── HUDElement overrides ──────────────────────────────────────────────

    public override void Show()
    {
        IsVisible = true;
        SetPanelAlpha(true);
        LockInput(true);
    }

    public override void Hide()
    {
        IsVisible = false;
        SetPanelAlpha(false);
        LockInput(false);
    }

    public override void Toggle()
    {
        if (IsVisible) Hide(); else Show();
    }

    public override void Refresh() { }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void LockInput(bool inventoryOpen)
    {
        if (_input != null)
            _input.InputEnabled = !inventoryOpen;

        Cursor.lockState = inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = inventoryOpen;
    }

    private void SetPanelAlpha(bool visible)
    {
        _cg.alpha          = visible ? 1f : 0f;
        _cg.blocksRaycasts = visible;
        _cg.interactable   = visible;
    }

    private void BuildPanel()
    {
        float titleHeight = 28f;
        float totalHeight = _padding + titleHeight + _rowGap
                          + SlotCount * _rowHeight + (SlotCount - 1) * _rowGap
                          + _padding;

        var rt              = GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(_panelWidth, totalHeight);

        var panelBg = MakeImage("PanelBg", rt);
        panelBg.color = PanelBg;
        Stretch(panelBg.rectTransform);

        var title = MakeText("Title", rt);
        title.text      = "LOADOUT";
        title.fontSize  = 13f;
        title.fontStyle = FontStyles.Bold;
        title.color     = new Color(1f, 1f, 1f, 0.85f);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.rectTransform.anchorMin        = new Vector2(0f, 1f);
        title.rectTransform.anchorMax        = new Vector2(1f, 1f);
        title.rectTransform.pivot            = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -_padding);
        title.rectTransform.sizeDelta        = new Vector2(-_padding * 2f, titleHeight);

        float slotStartY = -(_padding + titleHeight + _rowGap);

        for (int i = 0; i < SlotCount; i++)
        {
            float topY = slotStartY - i * (_rowHeight + _rowGap);

            var bg = MakeImage("Slot_" + i, rt);
            bg.color = EmptyBg;
            bg.rectTransform.anchorMin        = new Vector2(0f, 1f);
            bg.rectTransform.anchorMax        = new Vector2(1f, 1f);
            bg.rectTransform.pivot            = new Vector2(0.5f, 1f);
            bg.rectTransform.anchoredPosition = new Vector2(0f, topY);
            bg.rectTransform.sizeDelta        = new Vector2(-_padding * 2f, _rowHeight);
            _slotBgs[i] = bg;

            var key = MakeText("Key_" + i, bg.rectTransform);
            key.text      = SlotKeys[i];
            key.fontSize  = 12f;
            key.color     = new Color(1f, 1f, 1f, 0.5f);
            key.alignment = TextAlignmentOptions.Midline;
            key.rectTransform.anchorMin        = new Vector2(0f, 0f);
            key.rectTransform.anchorMax        = new Vector2(0f, 1f);
            key.rectTransform.pivot            = new Vector2(0f, 0.5f);
            key.rectTransform.anchoredPosition = new Vector2(10f, 0f);
            key.rectTransform.sizeDelta        = new Vector2(22f, 0f);

            var name = MakeText("Name_" + i, bg.rectTransform);
            name.text      = "— Empty —";
            name.fontSize  = 13f;
            name.color     = new Color(1f, 1f, 1f, 0.80f);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.rectTransform.anchorMin        = new Vector2(0f, 0f);
            name.rectTransform.anchorMax        = new Vector2(1f, 1f);
            name.rectTransform.pivot            = new Vector2(0f, 0.5f);
            name.rectTransform.anchoredPosition = new Vector2(40f, 0f);
            name.rectTransform.sizeDelta        = new Vector2(-50f, 0f);
            _slotNames[i] = name;
        }
    }

    private void BuildActiveBar()
    {
        // Sibling of InventoryHUD so it's unaffected by the panel's CanvasGroup
        var barGO = new GameObject("ActiveWeaponBar", typeof(RectTransform));
        barGO.transform.SetParent(transform.parent, false);

        var barRt = barGO.GetComponent<RectTransform>();
        barRt.anchorMin = barRt.anchorMax = barRt.pivot = new Vector2(0f, 0f);
        barRt.anchoredPosition = new Vector2(20f, 20f);
        barRt.sizeDelta = new Vector2(240f, 36f);

        var bg = MakeImage("Bg", barRt);
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.82f);
        Stretch(bg.rectTransform);

        const float boxSize = 24f;
        const float boxGap  = 4f;

        for (int i = 0; i < SlotCount; i++)
        {
            var box = MakeImage("SlotBox_" + i, barRt);
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            box.rectTransform.pivot     = new Vector2(0f, 0.5f);
            box.rectTransform.anchoredPosition = new Vector2(6f + i * (boxSize + boxGap), 0f);
            box.rectTransform.sizeDelta        = new Vector2(boxSize, boxSize);
            box.color = EmptyBg;
            _barSlotBgs[i] = box;

            var key = MakeText("Key_" + i, box.rectTransform);
            key.text      = SlotKeys[i];
            key.fontSize  = 10f;
            key.color     = new Color(1f, 1f, 1f, 0.85f);
            key.alignment = TextAlignmentOptions.Center;
            Stretch(key.rectTransform);
        }

        float nameX = 6f + SlotCount * (boxSize + boxGap) + 6f;
        _activeBarText = MakeText("WeaponName", barRt);
        _activeBarText.fontSize  = 12f;
        _activeBarText.color     = new Color(1f, 1f, 1f, 0.85f);
        _activeBarText.alignment = TextAlignmentOptions.MidlineLeft;
        _activeBarText.rectTransform.anchorMin = Vector2.zero;
        _activeBarText.rectTransform.anchorMax = Vector2.one;
        _activeBarText.rectTransform.pivot     = new Vector2(0f, 0.5f);
        _activeBarText.rectTransform.offsetMin = new Vector2(nameX, 0f);
        _activeBarText.rectTransform.offsetMax = new Vector2(-6f, 0f);
        _activeBarText.text = "— Empty —";
    }

    private static Image MakeImage(string n, RectTransform parent)
    {
        var go  = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string n, RectTransform parent)
    {
        var go   = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
