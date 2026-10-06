using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Items;
using CGD.Player;
using CGD.Weapons;

namespace CGD.UI
{
    // The item slots, bottom-right above the weapon panel: what each holds and how many
    // are left, with a bar that fills while a consumable is being used, a grey bar that
    // drains over consumables while the shared cooldown runs, and the row lit while its
    // throwable is in hand.
    [RequireComponent(typeof(RectTransform))]
    public class ItemSlotsHUD : HUDElement
    {
        [SerializeField] private PlayerItemSlots     _slots;
        [SerializeField] private PlayerInventory     _inventory;
        [Tooltip("Optional — lights the row of the throwable in hand")]
        [SerializeField] private ThrowableController _throwing;
        [SerializeField] private string[] _keyLabels = { "5", "6", "7", "8" };

        [Header("Layout")]
        [SerializeField] private Vector2 _screenPadding = new(20f, 160f);
        [SerializeField] private float   _width     = 180f;
        [SerializeField] private float   _rowHeight = 24f;

        private static readonly Color RowBg         = new(0f, 0f, 0f, 0.45f);
        private static readonly Color FillColor     = new(0.35f, 0.75f, 0.45f, 0.6f);
        private static readonly Color CooldownColor = new(0.6f, 0.6f, 0.6f, 0.4f);
        private static readonly Color ReadyBg       = new(0.25f, 0.55f, 1f, 0.6f);

        private Image[] _rows;

        private TextMeshProUGUI[] _labels;
        private RectTransform[]   _fills;
        private Image[]           _fillImages;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            UIFactory.AnchorToCorner(self, new Vector2(1f, 0f), _screenPadding);
            self.sizeDelta = new Vector2(_width, PlayerItemSlots.SlotCount * (_rowHeight + 2f));

            _labels = new TextMeshProUGUI[PlayerItemSlots.SlotCount];
            _rows   = new Image[PlayerItemSlots.SlotCount];
            _fills  = new RectTransform[PlayerItemSlots.SlotCount];
            _fillImages = new Image[PlayerItemSlots.SlotCount];

            for (int i = 0; i < PlayerItemSlots.SlotCount; i++)
            {
                Image row = UIFactory.MakeImage($"Slot{i}", self);
                row.color = RowBg;
                _rows[i] = row;
                UIFactory.Place(row.rectTransform, new Vector2(0f, -i * (_rowHeight + 2f)), new Vector2(_width, _rowHeight));

                Image fill = UIFactory.MakeImage("Fill", row.rectTransform);
                fill.color = FillColor;
                _fillImages[i] = fill;
                _fills[i] = fill.rectTransform;
                _fills[i].anchorMin = Vector2.zero;
                _fills[i].anchorMax = new Vector2(0f, 1f);
                _fills[i].offsetMin = _fills[i].offsetMax = Vector2.zero;

                _labels[i] = UIFactory.MakeText("Label", row.rectTransform);
                _labels[i].fontSize  = 13f;
                _labels[i].alignment = TextAlignmentOptions.MidlineLeft;
                UIFactory.Stretch(_labels[i].rectTransform);
                _labels[i].rectTransform.offsetMin = new Vector2(8f, 0f);
            }
        }

        private void OnEnable()
        {
            if (_inventory != null) _inventory.Inventory.Changed += Refresh;
            if (_slots != null)     _slots.SlotsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_inventory != null) _inventory.Inventory.Changed -= Refresh;
            if (_slots != null)     _slots.SlotsChanged -= Refresh;
        }

        // Labels update on inventory/slot events; only the use bar and the lit row change every frame.
        private void Update()
        {
            if (_slots == null) return;

            ThrowableDefinition inHand = _throwing != null ? _throwing.Readied : null;
            for (int i = 0; i < PlayerItemSlots.SlotCount; i++)
            {
                ItemDefinition item = _slots.Slots[i];
                bool  using_      = _slots.IsUsing && _slots.Using == item;
                bool  coolingDown = !using_ && _slots.OnCooldown && item is ConsumableDefinition;
                float fill        = using_ ? _slots.UseProgress : coolingDown ? 1f - _slots.CooldownRatio : 0f;
                _fills[i].anchorMax  = new Vector2(fill, 1f);
                _fillImages[i].color = coolingDown ? CooldownColor : FillColor;
                _rows[i].color = inHand != null && inHand == item ? ReadyBg : RowBg;
            }
        }

        public override void Refresh()
        {
            if (_slots == null || _labels == null) return;

            for (int i = 0; i < PlayerItemSlots.SlotCount; i++)
            {
                ItemDefinition item = _slots.Slots[i];
                string key = i < _keyLabels.Length ? _keyLabels[i] : (i + 1).ToString();
                string text = item != null ? $"[{key}] {item.DisplayName} ×{_slots.CountOf(i)}" : $"[{key}] —";
                if (_labels[i].text != text) _labels[i].text = text;
            }
        }
    }
}
