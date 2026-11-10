using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Core;
using CGD.Crafting;
using CGD.Expedition;
using CGD.Flow;
using CGD.Items;
using CGD.Telemetry;

namespace CGD.UI
{
    // The docked ship between runs: when a run ends it shows how it went, the ship's hold,
    // the kit packed for the next run and the ship's upgrades. Clicking an entry moves it
    // between hold and kit, or buys an upgrade's next level from the hold. Deploy starts the next run (a fresh level from the same scene). Builds its own
    // hidden panel, so it only needs a RectTransform under the HUD canvas.
    [RequireComponent(typeof(RectTransform))]
    public class ExpeditionScreen : MonoBehaviour
    {
        private static readonly Vector2 WindowSize = new(1100f, 540f);
        private const int   Columns    = 3;
        private const float ListTop    = 110f;
        private const float FooterSize = 52f;

        [SerializeField] private ExpeditionRunner _runner;
        [Tooltip("Optional — adds how the run went (time, kills, damage) under the summary")]
        [SerializeField] private RunStatsRecorder _telemetry;

        private GameObject _panel;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _summary;
        private TextMeshProUGUI _runStats;
        private UIButtonList _hold;
        private UIButtonList _kit;
        private UIButtonList _upgrades;

        private ExpeditionLedger Ledger => _runner.Ledger;

        private void Awake()
        {
            Build();
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (_runner != null) _runner.RunEnded += Show;
        }

        private void OnDisable()
        {
            if (_runner != null) _runner.RunEnded -= Show;
            Unsubscribe();
        }

        private void Show(RunReport report)
        {
            if (ModalPanel.Open != null) ModalPanel.Open.Hide();

            _title.text = report.Outcome switch
            {
                RunOutcome.Extracted          => "EXTRACTED",
                RunOutcome.EmergencyExtracted => "EMERGENCY EXTRACTION",
                _                             => "KILLED IN ACTION",
            };
            _title.color  = report.Extracted ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f);
            _summary.text = report.Outcome switch
            {
                RunOutcome.Extracted          => $"{report.Carried} items secured in the ship's hold.",
                RunOutcome.EmergencyExtracted => $"{report.Carried} items secured; {report.Left} didn't fit in the escape pod.",
                _                             => $"Lost {report.Carried} items, {report.Brought} of them brought from the ship.",
            };

            _runStats.text = _telemetry != null ? RunStatsText.Summary(_telemetry.Current) : string.Empty;

            Unsubscribe();
            Ledger.Storage.Changed += Refresh;
            Ledger.Kit.Changed     += Refresh;
            Ledger.Upgrades.Changed += Refresh;
            Refresh();

            _panel.SetActive(true);
            CursorLock.Set(false);
        }

        private void Unsubscribe()
        {
            if (_runner == null) return;
            Ledger.Storage.Changed -= Refresh;
            Ledger.Kit.Changed     -= Refresh;
            Ledger.Upgrades.Changed -= Refresh;
        }

        private void Refresh()
        {
            Fill(_hold, "Ship hold  (click to pack)", Ledger.Storage, pack: true);
            Fill(_kit,  "Bringing next run  (click to unpack)", Ledger.Kit, pack: false);
            FillUpgrades();
        }

        private void Fill(UIButtonList list, string heading, Inventory inventory, bool pack)
        {
            list.Begin();
            list.Heading(heading);
            if (inventory.SlotCount == 0) list.Label("  empty");

            foreach (ItemStack stack in inventory.Stacks)
            {
                ItemDefinition definition = stack.Definition;
                int count = stack.Count;
                list.Add($"{definition.DisplayName}  x{count}", () => Move(definition, count, pack));
            }
            foreach (ItemInstance item in inventory.Items)
                list.Add(item.DisplayName, () => Move(item, pack));
            list.End();
        }

        private void FillUpgrades()
        {
            _upgrades.Begin();
            _upgrades.Heading("Ship upgrades  (click to buy from the hold)");

            ShipUpgradeCatalog catalog = _runner.Upgrades;
            if (catalog == null || catalog.Upgrades.Count == 0) _upgrades.Label("  none available");
            else
                foreach (ShipUpgrade upgrade in catalog.Upgrades)
                    if (upgrade != null) AddUpgrade(upgrade);
            _upgrades.End();
        }

        private void AddUpgrade(ShipUpgrade upgrade)
        {
            ShipUpgrades owned = Ledger.Upgrades;
            string title = $"{upgrade.DisplayName}  {owned.LevelOf(upgrade)}/{upgrade.MaxLevel}";

            ShipUpgradeLevel next = owned.Next(upgrade);
            if (next == null)
            {
                _upgrades.Add($"{title}\n<size=80%>fully upgraded</size>", () => { }, interactable: false);
                return;
            }

            string line = $"{next.Summary} · {CostText(next)}";
            _upgrades.Add($"{title}\n<size=80%>{line}</size>", () => Ledger.BuyUpgrade(upgrade),
                          owned.CanAfford(upgrade, Ledger.Storage));
        }

        private static string CostText(ShipUpgradeLevel level)
        {
            var text = new StringBuilder();
            foreach (ItemAmount cost in level.Cost)
            {
                if (cost.Item == null) continue;
                if (text.Length > 0) text.Append(", ");
                text.Append(cost.SafeCount).Append(' ').Append(cost.Item.DisplayName);
            }
            return text.Length > 0 ? text.ToString() : "free";
        }

        private void Move(ItemDefinition definition, int count, bool pack)
        {
            if (pack) Ledger.Pack(definition, count);
            else      Ledger.Unpack(definition, count);
        }

        private void Move(ItemInstance item, bool pack)
        {
            if (pack) Ledger.Pack(item);
            else      Ledger.Unpack(item);
        }

        private static void Deploy()
        {
            if (GameFlow.Instance != null) GameFlow.Instance.RestartLevel();
        }

        private void Build()
        {
            var root = GetComponent<RectTransform>();
            UIFactory.Stretch(root);

            Image dim = UIFactory.MakeImage("ExpeditionPanel", root);
            dim.color = new Color(0f, 0f, 0f, 0.8f);
            dim.raycastTarget = true;
            UIFactory.Stretch(dim.rectTransform);
            _panel = dim.gameObject;

            Image window = UIFactory.MakeImage("Window", dim.rectTransform);
            window.color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
            RectTransform windowRt = window.rectTransform;
            windowRt.anchorMin = windowRt.anchorMax = windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = WindowSize;

            _title = UIFactory.MakeText("Title", windowRt);
            _title.fontSize  = 30f;
            _title.fontStyle = FontStyles.Bold;
            _title.alignment = TextAlignmentOptions.Center;
            UIFactory.Place(_title.rectTransform, new Vector2(0f, -14f), new Vector2(WindowSize.x, 40f));

            _summary = UIFactory.MakeText("Summary", windowRt);
            _summary.fontSize  = 16f;
            _summary.alignment = TextAlignmentOptions.Center;
            UIFactory.Place(_summary.rectTransform, new Vector2(0f, -58f), new Vector2(WindowSize.x, 28f));

            _runStats = UIFactory.MakeText("RunStats", windowRt);
            _runStats.fontSize  = 14f;
            _runStats.color     = new Color(0.7f, 0.7f, 0.75f);
            _runStats.alignment = TextAlignmentOptions.Center;
            UIFactory.Place(_runStats.rectTransform, new Vector2(0f, -84f), new Vector2(WindowSize.x, 22f));

            float columnWidth  = (WindowSize.x - 16f * (Columns + 1)) / Columns;
            float columnHeight = WindowSize.y - ListTop - FooterSize - 12f;
            _hold = new UIButtonList(Column("Hold", windowRt, 16f, columnWidth, columnHeight));
            _kit  = new UIButtonList(Column("Kit", windowRt, 32f + columnWidth, columnWidth, columnHeight));
            _upgrades = new UIButtonList(Column("Upgrades", windowRt, 48f + columnWidth * 2f, columnWidth, columnHeight), rowHeight: 40f);

            Button deploy = UIFactory.MakeButton("Deploy", windowRt, "Deploy", out _);
            RectTransform deployRt = deploy.GetComponent<RectTransform>();
            UIFactory.Place(deployRt, new Vector2((WindowSize.x - 200f) * 0.5f, -(WindowSize.y - FooterSize)), new Vector2(200f, 38f));
            deploy.onClick.AddListener(Deploy);
        }

        private static RectTransform Column(string name, RectTransform parent, float x, float width, float height)
        {
            var column = new GameObject(name, typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            column.SetParent(parent, false);
            UIFactory.Place(column, new Vector2(x, -ListTop), new Vector2(width, height));
            return column;
        }
    }
}
