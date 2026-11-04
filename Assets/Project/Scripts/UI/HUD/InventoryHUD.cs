using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Core;
using CGD.Input;
using CGD.Player;

namespace CGD.UI
{
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
        private static readonly GameAction[] SlotActions =
        {
            GameAction.Weapon1,
            GameAction.Weapon2,
            GameAction.Weapon3,
            GameAction.Weapon4,
        };

        private const float ActiveAlpha = 0.92f;

        private Image[]           _slotBgs     = new Image[SlotCount];
        private TextMeshProUGUI[] _slotKeys    = new TextMeshProUGUI[SlotCount];
        private TextMeshProUGUI[] _barKeys     = new TextMeshProUGUI[SlotCount];
        private TextMeshProUGUI[] _slotNames   = new TextMeshProUGUI[SlotCount];
        private Image[]           _barSlotBgs  = new Image[SlotCount];
        private TextMeshProUGUI   _activeBarText;
        private CanvasGroup       _cg;

        private void OnEnable()
        {
            if (Bindings != null) Bindings.Changed += RefreshKeys;
            RefreshKeys();
        }

        private void OnDisable()
        {
            if (Bindings != null) Bindings.Changed -= RefreshKeys;
        }

        private void RefreshKeys()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                string key = KeyOf(SlotActions[i]);
                _slotKeys[i].text = key;
                _barKeys[i].text  = key;
            }
        }

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
                bool isActive = active == i;

                // Always update the compact bar
                bool filled = _loadout.IsFilled(i);
                _barSlotBgs[i].color = isActive ? Theme.AccentStrong.WithAlpha(ActiveAlpha) : (filled ? Theme.Slot : Theme.SlotEmpty);
                if (isActive)
                    _activeBarText.text = _loadout.SlotLabel(i);

                if (!IsVisible) continue;

                if (_input.WasPressedRaw(SlotActions[i]))
                    _loadout.EquipSlot(i);

                _slotBgs[i].color  = isActive ? Theme.AccentStrong.WithAlpha(ActiveAlpha) : (filled ? Theme.Slot : Theme.SlotEmpty);
                _slotNames[i].text = _loadout.SlotLabel(i);
            }
        }

        public override bool ShowWithHud => false;

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

        private void LockInput(bool inventoryOpen)
        {
            if (_input != null)
                _input.InputEnabled = !inventoryOpen;

            CursorLock.Set(!inventoryOpen);
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

            var panelBg = UIFactory.MakeImage("PanelBg", rt);
            panelBg.color = Theme.Window;
            UIFactory.Stretch(panelBg.rectTransform);

            var title = UIFactory.MakeText("Title", rt);
            title.text      = "LOADOUT";
            title.fontSize  = 13f;
            title.fontStyle = FontStyles.Bold;
            title.color     = Theme.TextPrimary;
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

                var bg = UIFactory.MakeImage("Slot_" + i, rt);
                bg.color = Theme.SlotEmpty;
                bg.rectTransform.anchorMin        = new Vector2(0f, 1f);
                bg.rectTransform.anchorMax        = new Vector2(1f, 1f);
                bg.rectTransform.pivot            = new Vector2(0.5f, 1f);
                bg.rectTransform.anchoredPosition = new Vector2(0f, topY);
                bg.rectTransform.sizeDelta        = new Vector2(-_padding * 2f, _rowHeight);
                _slotBgs[i] = bg;

                var key = UIFactory.MakeText("Key_" + i, bg.rectTransform);
                _slotKeys[i]  = key;
                key.fontSize  = 12f;
                key.color     = Theme.TextMuted;
                key.alignment = TextAlignmentOptions.Midline;
                key.rectTransform.anchorMin        = new Vector2(0f, 0f);
                key.rectTransform.anchorMax        = new Vector2(0f, 1f);
                key.rectTransform.pivot            = new Vector2(0f, 0.5f);
                key.rectTransform.anchoredPosition = new Vector2(10f, 0f);
                key.rectTransform.sizeDelta        = new Vector2(22f, 0f);
                UIFactory.ShrinkToFit(key, 7f);

                var name = UIFactory.MakeText("Name_" + i, bg.rectTransform);
                name.text      = "— Empty —";
                name.fontSize  = 13f;
                name.color     = Theme.TextPrimary;
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

            var bg = UIFactory.MakeImage("Bg", barRt);
            bg.color = Theme.Window;
            UIFactory.Stretch(bg.rectTransform);

            const float boxSize = 24f;
            const float boxGap  = 4f;

            for (int i = 0; i < SlotCount; i++)
            {
                var box = UIFactory.MakeImage("SlotBox_" + i, barRt);
                box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                box.rectTransform.pivot     = new Vector2(0f, 0.5f);
                box.rectTransform.anchoredPosition = new Vector2(6f + i * (boxSize + boxGap), 0f);
                box.rectTransform.sizeDelta        = new Vector2(boxSize, boxSize);
                box.color = Theme.SlotEmpty;
                _barSlotBgs[i] = box;

                var key = UIFactory.MakeText("Key_" + i, box.rectTransform);
                _barKeys[i]   = key;
                key.fontSize  = 10f;
                key.color     = Theme.TextPrimary;
                key.alignment = TextAlignmentOptions.Center;
                UIFactory.Stretch(key.rectTransform);
                UIFactory.ShrinkToFit(key, 6f);
            }

            float nameX = 6f + SlotCount * (boxSize + boxGap) + 6f;
            _activeBarText = UIFactory.MakeText("WeaponName", barRt);
            _activeBarText.fontSize  = 12f;
            _activeBarText.color     = Theme.TextPrimary;
            _activeBarText.alignment = TextAlignmentOptions.MidlineLeft;
            _activeBarText.rectTransform.anchorMin = Vector2.zero;
            _activeBarText.rectTransform.anchorMax = Vector2.one;
            _activeBarText.rectTransform.pivot     = new Vector2(0f, 0.5f);
            _activeBarText.rectTransform.offsetMin = new Vector2(nameX, 0f);
            _activeBarText.rectTransform.offsetMax = new Vector2(-6f, 0f);
            _activeBarText.text = "— Empty —";
        }
    }
}
