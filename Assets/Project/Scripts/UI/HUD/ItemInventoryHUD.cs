using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.Core;
using CGD.Input;
using CGD.Items;
using CGD.Player;

namespace CGD.UI
{
    // Basic inventory panel: capacity + weight readout at the top, a flat list of
    // items below. Toggles on the Inventory action alongside the weapon-loadout HUD.
    //
    // Stackable items are aggregated by definition — a player who is carrying two
    // internal LightRounds stacks of 999 and 201 sees a single "Light Rounds 1200"
    // line, so the display matches how the player *thinks* about their ammo pool
    // rather than the container splits the stack limit enforces.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ItemInventoryHUD : HUDElement
    {
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerInventory    _inventory;

        [Header("Layout")]
        [SerializeField] private float   _panelWidth   = 320f;
        [SerializeField] private float   _panelHeight  = 380f;
        [SerializeField] private float   _padding      = 14f;
        [SerializeField] private float   _rowHeight    = 20f;
        [SerializeField] private Vector2 _edgeOffset   = new(40f, 0f);

        private static readonly Color PanelBg = new(0.05f, 0.05f, 0.08f, 0.93f);
        private static readonly Color RuleBg  = new(1f,    1f,    1f,    0.10f);
        private static readonly Color Text    = new(1f,    1f,    1f,    0.90f);
        private static readonly Color Muted   = new(1f,    1f,    1f,    0.55f);
        private static readonly Color Header  = new(1f,    1f,    1f,    0.85f);

        private CanvasGroup     _cg;
        private TextMeshProUGUI _slotsText;
        private TextMeshProUGUI _weightText;
        private RectTransform   _listRoot;
        private float           _listAvailableHeight;

        private readonly List<TextMeshProUGUI> _nameRows  = new();
        private readonly List<TextMeshProUGUI> _countRows = new();

        // Aggregation buffer for stackables — rebuilt on each Refresh. Definitions
        // are ScriptableObjects so reference equality is stable and safe as a key.
        private readonly Dictionary<ItemDefinition, int> _stackTotals = new();

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_inventory == null) _inventory = FindPlayerInventory();

            BuildPanel();
            SetAlpha(false);
            IsVisible = false;
        }

        private void OnEnable()
        {
            if (_inventory != null) _inventory.Inventory.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (_inventory != null) _inventory.Inventory.Changed -= Refresh;
        }

        private void Update()
        {
            // WasPressedRaw bypasses InputEnabled so the key works even while the
            // panel or a paired modal has locked input.
            if (_input != null && _input.WasPressedRaw(GameAction.Inventory))
                Toggle();
        }

        // ── HUDElement overrides ──────────────────────────────────────────────

        public override bool ShowWithHud => false;

        public override void Show()
        {
            IsVisible = true;
            SetAlpha(true);
            Refresh();
        }

        public override void Hide()
        {
            IsVisible = false;
            SetAlpha(false);
        }

        public override void Toggle()
        {
            if (IsVisible) Hide(); else Show();
        }

        public override void Refresh()
        {
            if (_inventory == null || _slotsText == null) return;

            Inventory inv = _inventory.Inventory;
            _slotsText.text  = $"Slots   {inv.SlotCount} / {_inventory.MaxSlots}";
            _weightText.text = $"Weight  {inv.TotalWeight:0.0} / {_inventory.MaxWeight:0.0} kg";
            _slotsText.color  = inv.SlotCount   > _inventory.MaxSlots  ? Warning() : Muted;
            _weightText.color = inv.TotalWeight > _inventory.MaxWeight ? Warning() : Muted;

            RebuildRows(inv);
        }

        // ── Row rendering ─────────────────────────────────────────────────────

        private void RebuildRows(Inventory inv)
        {
            _stackTotals.Clear();
            foreach (ItemStack stack in inv.Stacks)
            {
                if (stack.Definition == null) continue;
                _stackTotals.TryGetValue(stack.Definition, out int prev);
                _stackTotals[stack.Definition] = prev + stack.Count;
            }

            int row = 0;
            int maxRows = Mathf.Max(1, Mathf.FloorToInt(_listAvailableHeight / _rowHeight));

            foreach (KeyValuePair<ItemDefinition, int> kv in _stackTotals)
            {
                if (row >= maxRows) break;
                SetRow(row++, kv.Key.DisplayName, kv.Value.ToString());
            }

            foreach (ItemInstance item in inv.Items)
            {
                if (row >= maxRows) break;
                SetRow(row++, item.DisplayName, string.Empty);
            }

            for (int i = row; i < _nameRows.Count; i++)
            {
                _nameRows[i].gameObject.SetActive(false);
                _countRows[i].gameObject.SetActive(false);
            }
        }

        private void SetRow(int index, string name, string count)
        {
            EnsureRow(index);
            var nameLabel  = _nameRows[index];
            var countLabel = _countRows[index];

            nameLabel.gameObject.SetActive(true);
            countLabel.gameObject.SetActive(true);
            nameLabel.text  = name;
            countLabel.text = count;
        }

        private void EnsureRow(int index)
        {
            while (_nameRows.Count <= index)
                CreateRow(_nameRows.Count);
        }

        private void CreateRow(int index)
        {
            float topY = -index * _rowHeight;

            var nameLabel = UIFactory.MakeText("Name_" + index, _listRoot);
            nameLabel.color     = Text;
            nameLabel.fontSize  = 12f;
            nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            nameLabel.rectTransform.pivot     = new Vector2(0f, 1f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, topY);
            nameLabel.rectTransform.sizeDelta        = new Vector2(-56f, _rowHeight);

            var countLabel = UIFactory.MakeText("Count_" + index, _listRoot);
            countLabel.color     = Muted;
            countLabel.fontSize  = 12f;
            countLabel.alignment = TextAlignmentOptions.MidlineRight;
            countLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
            countLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            countLabel.rectTransform.pivot     = new Vector2(1f, 1f);
            countLabel.rectTransform.anchoredPosition = new Vector2(0f, topY);
            countLabel.rectTransform.sizeDelta        = new Vector2(50f, _rowHeight);

            _nameRows.Add(nameLabel);
            _countRows.Add(countLabel);
        }

        // ── Panel construction ────────────────────────────────────────────────

        private void BuildPanel()
        {
            var rt              = GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0.5f);
            rt.anchorMax        = new Vector2(1f, 0.5f);
            rt.pivot            = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-_edgeOffset.x, _edgeOffset.y);
            rt.sizeDelta        = new Vector2(_panelWidth, _panelHeight);

            var bg = UIFactory.MakeImage("PanelBg", rt);
            bg.color = PanelBg;
            UIFactory.Stretch(bg.rectTransform);

            float y = -_padding;

            var title = UIFactory.MakeText("Title", rt);
            title.text      = "INVENTORY";
            title.fontSize  = 13f;
            title.fontStyle = FontStyles.Bold;
            title.color     = Header;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            PlaceRow(title.rectTransform, y, 20f);
            y -= 24f;

            _slotsText = UIFactory.MakeText("SlotsText", rt);
            _slotsText.fontSize  = 12f;
            _slotsText.color     = Muted;
            _slotsText.alignment = TextAlignmentOptions.MidlineLeft;
            PlaceRow(_slotsText.rectTransform, y, 18f);
            y -= 18f;

            _weightText = UIFactory.MakeText("WeightText", rt);
            _weightText.fontSize  = 12f;
            _weightText.color     = Muted;
            _weightText.alignment = TextAlignmentOptions.MidlineLeft;
            PlaceRow(_weightText.rectTransform, y, 18f);
            y -= 22f;

            var rule = UIFactory.MakeImage("Rule", rt);
            rule.color = RuleBg;
            rule.rectTransform.anchorMin = new Vector2(0f, 1f);
            rule.rectTransform.anchorMax = new Vector2(1f, 1f);
            rule.rectTransform.pivot     = new Vector2(0.5f, 1f);
            rule.rectTransform.anchoredPosition = new Vector2(0f, y);
            rule.rectTransform.sizeDelta        = new Vector2(-_padding * 2f, 1f);
            y -= 8f;

            var listGO = new GameObject("List", typeof(RectTransform));
            listGO.transform.SetParent(rt, false);
            _listRoot = listGO.GetComponent<RectTransform>();
            _listRoot.anchorMin        = new Vector2(0f, 1f);
            _listRoot.anchorMax        = new Vector2(1f, 1f);
            _listRoot.pivot            = new Vector2(0.5f, 1f);
            _listRoot.anchoredPosition = new Vector2(0f, y);
            _listAvailableHeight       = _panelHeight + y - _padding;
            _listRoot.sizeDelta        = new Vector2(-_padding * 2f, _listAvailableHeight);
        }

        private void PlaceRow(RectTransform rowRt, float y, float height)
        {
            rowRt.anchorMin        = new Vector2(0f, 1f);
            rowRt.anchorMax        = new Vector2(1f, 1f);
            rowRt.pivot            = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta        = new Vector2(-_padding * 2f, height);
        }

        private void SetAlpha(bool visible)
        {
            _cg.alpha          = visible ? 1f : 0f;
            _cg.blocksRaycasts = visible;
            _cg.interactable   = visible;
        }

        private static Color Warning() => new(1f, 0.55f, 0.35f, 1f);

        private static PlayerInventory FindPlayerInventory()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<PlayerInventory>();
#else
            return Object.FindObjectOfType<PlayerInventory>();
#endif
        }
    }
}
