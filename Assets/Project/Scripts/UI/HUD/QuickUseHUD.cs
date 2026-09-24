using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Items;
using CGD.Player;

namespace CGD.UI
{
    // The quick-use consumable slots, bottom-right above the weapon panel: what each
    // holds and how many are left, with a bar that fills while one is being used.
    [RequireComponent(typeof(RectTransform))]
    public class QuickUseHUD : HUDElement
    {
        [SerializeField] private PlayerConsumables _consumables;
        [SerializeField] private PlayerInventory   _inventory;
        [SerializeField] private string[] _keyLabels = { "Z", "B" };

        [Header("Layout")]
        [SerializeField] private Vector2 _screenPadding = new(20f, 160f);
        [SerializeField] private float   _width     = 180f;
        [SerializeField] private float   _rowHeight = 24f;

        private static readonly Color RowBg    = new(0f, 0f, 0f, 0.45f);
        private static readonly Color FillColor = new(0.35f, 0.75f, 0.45f, 0.6f);

        private TextMeshProUGUI[] _labels;
        private RectTransform[]   _fills;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            UIFactory.AnchorToCorner(self, new Vector2(1f, 0f), _screenPadding);
            self.sizeDelta = new Vector2(_width, PlayerConsumables.SlotCount * (_rowHeight + 2f));

            _labels = new TextMeshProUGUI[PlayerConsumables.SlotCount];
            _fills  = new RectTransform[PlayerConsumables.SlotCount];

            for (int i = 0; i < PlayerConsumables.SlotCount; i++)
            {
                Image row = UIFactory.MakeImage($"Slot{i}", self);
                row.color = RowBg;
                UIFactory.Place(row.rectTransform, new Vector2(0f, -i * (_rowHeight + 2f)), new Vector2(_width, _rowHeight));

                Image fill = UIFactory.MakeImage("Fill", row.rectTransform);
                fill.color = FillColor;
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
            if (_inventory != null)   _inventory.Inventory.Changed += Refresh;
            if (_consumables != null) _consumables.SlotsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_inventory != null)   _inventory.Inventory.Changed -= Refresh;
            if (_consumables != null) _consumables.SlotsChanged -= Refresh;
        }

        // Labels update on inventory/slot events; only the use bar moves every frame.
        private void Update()
        {
            if (_consumables == null) return;

            for (int i = 0; i < PlayerConsumables.SlotCount; i++)
            {
                bool active = _consumables.IsUsing && _consumables.Using == _consumables.Slots[i];
                _fills[i].anchorMax = new Vector2(active ? _consumables.UseProgress : 0f, 1f);
            }

        }

        public override void Refresh()
        {
            if (_consumables == null || _labels == null) return;

            for (int i = 0; i < PlayerConsumables.SlotCount; i++)
            {
                ConsumableDefinition item = _consumables.Slots[i];
                string key = i < _keyLabels.Length ? _keyLabels[i] : (i + 1).ToString();
                string text = item != null ? $"[{key}] {item.DisplayName} ×{_consumables.CountOf(i)}" : $"[{key}] —";
                if (_labels[i].text != text) _labels[i].text = text;
            }
        }
    }
}
