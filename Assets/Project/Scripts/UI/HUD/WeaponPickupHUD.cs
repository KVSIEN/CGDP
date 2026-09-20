using System.Text;
using TMPro;
using UnityEngine;
using CGD.Interaction;
using CGD.Items;
using CGD.Player;
using CGD.Weapons;

namespace CGD.UI
{
    // Auto-showing panel that summarises the weapon the player is currently hovering
    // over. Sits top-right so it doesn't fight the interact prompt (bottom-centre)
    // or the item inventory panel (middle-right). Hides itself the moment the
    // interactable stops being a weapon pickup.
    //
    // Reads from the pickup's WeaponInstance so quality/tier/attachments and any
    // rolled category label are respected — future comparison-to-equipped tooltips
    // can reuse this same source of truth.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class WeaponPickupHUD : HUDElement
    {
        [SerializeField] private PlayerInteraction _interaction;

        [Header("Layout")]
        [SerializeField] private float   _panelWidth  = 320f;
        [SerializeField] private float   _padding     = 14f;
        [SerializeField] private float   _rowHeight   = 18f;
        [SerializeField] private Vector2 _edgeOffset  = new(40f, 40f);

        private static readonly Color PanelBg = new(0.05f, 0.05f, 0.08f, 0.93f);
        private static readonly Color RuleBg  = new(1f,    1f,    1f,    0.10f);
        private static readonly Color Header  = new(1f,    1f,    1f,    0.92f);
        private static readonly Color KeyText = new(1f,    1f,    1f,    0.55f);
        private static readonly Color ValText = new(1f,    1f,    1f,    0.90f);
        private static readonly Color Footer  = new(1f,    1f,    1f,    0.70f);

        // Standard loot-tier palette — reusable later for inventory tooltips.
        private static readonly Color TierCommon    = new(0.75f, 0.75f, 0.75f, 1f);
        private static readonly Color TierUncommon  = new(0.30f, 0.85f, 0.30f, 1f);
        private static readonly Color TierRare      = new(0.30f, 0.60f, 0.90f, 1f);
        private static readonly Color TierEpic      = new(0.70f, 0.35f, 0.90f, 1f);
        private static readonly Color TierLegendary = new(0.90f, 0.70f, 0.30f, 1f);

        // 5 rows per column captures the essentials without turning into a wall of
        // numbers; contextual footer picks up the mode-specific extras.
        private const int RowsPerColumn = 5;

        private CanvasGroup _cg;

        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _subtitleText;
        private TextMeshProUGUI[] _leftKeys   = new TextMeshProUGUI[RowsPerColumn];
        private TextMeshProUGUI[] _leftVals   = new TextMeshProUGUI[RowsPerColumn];
        private TextMeshProUGUI[] _rightKeys  = new TextMeshProUGUI[RowsPerColumn];
        private TextMeshProUGUI[] _rightVals  = new TextMeshProUGUI[RowsPerColumn];
        private TextMeshProUGUI _footerText;
        private RectTransform   _rectTransform;

        // Reused across refreshes so no per-frame string allocations while hovering.
        private readonly StringBuilder _footerBuilder = new(64);

        private WeaponInstance _shownWeapon;

        private void Awake()
        {
            _cg            = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            BuildPanel();
            SetAlpha(false);
            IsVisible = false;
        }

        private void LateUpdate()
        {
            if (_interaction == null)
            {
                if (IsVisible) HideInternal();
                return;
            }

            WeaponInstance weapon = _interaction.Current is WeaponPickup pickup ? pickup.Weapon : null;

            if (weapon == null)
            {
                if (IsVisible) HideInternal();
                return;
            }

            if (weapon != _shownWeapon)
            {
                _shownWeapon = weapon;
                Populate(weapon);
            }

            if (!IsVisible) ShowInternal();
        }

        // ── HUDElement overrides ──────────────────────────────────────────────

        // Not part of ShowAll — visibility is driven entirely by what the player is
        // hovering, so a HUD-wide toggle (death screen, pause) shouldn't reopen it.
        public override bool ShowWithHud => false;

        public override void Show() => ShowInternal();
        public override void Hide() => HideInternal();
        public override void Refresh() { if (_shownWeapon != null) Populate(_shownWeapon); }

        // ── Population ────────────────────────────────────────────────────────

        private void Populate(WeaponInstance weapon)
        {
            WeaponData d = weapon.Data;
            if (d == null) return;

            _nameText.text = string.IsNullOrEmpty(d.WeaponName) ? "Weapon" : d.WeaponName;

            _subtitleText.color = TierColor(weapon.Tier);
            _subtitleText.text  = BuildSubtitle(weapon, d);

            SetRow(0, _leftKeys, _leftVals,  "Damage",    d.Damage.ToString("0.#"));
            SetRow(1, _leftKeys, _leftVals,  "Fire Rate", $"{d.RoundsPerMinute:0} rpm");
            SetRow(2, _leftKeys, _leftVals,  "Magazine",  d.MagazineSize.ToString());
            SetRow(3, _leftKeys, _leftVals,  "Reload",    $"{d.ReloadTime:0.0}s");
            SetRow(4, _leftKeys, _leftVals,  "Range",     $"{d.RangeOptimal:0} m");

            SetRow(0, _rightKeys, _rightVals, "Fire Mode", FireModeLabel(d.FireMode, d.BurstCount));
            SetRow(1, _rightKeys, _rightVals, "Ammo",      AmmoLabel(d.AmmoType));
            SetRow(2, _rightKeys, _rightVals, "Recoil",    $"{d.RecoilScale.y:0.##}°");
            SetRow(3, _rightKeys, _rightVals, "Spread",    $"{d.HipSpreadDeg:0.##}°");
            SetRow(4, _rightKeys, _rightVals, "Draw",      $"{d.DrawTime:0.0}s");

            _footerText.text = BuildFooter(d);
        }

        private string BuildSubtitle(WeaponInstance weapon, WeaponData d)
        {
            _footerBuilder.Clear();

            if (weapon.Definition is WeaponCategoryData cat)
                _footerBuilder.Append(cat.Type).Append(" · ");

            _footerBuilder.Append(weapon.Tier);
            if (weapon.Quality > 0)
                _footerBuilder.Append(" · Q").Append(weapon.Quality);

            string subtitle = _footerBuilder.ToString();
            _footerBuilder.Clear();
            return subtitle;
        }

        private string BuildFooter(WeaponData d)
        {
            _footerBuilder.Clear();
            bool needsSeparator = false;

            void Append(string chunk)
            {
                if (needsSeparator) _footerBuilder.Append("   ·   ");
                _footerBuilder.Append(chunk);
                needsSeparator = true;
            }

            if (d.HeadshotMultiplier > 1f)
                Append($"Headshot ×{d.HeadshotMultiplier:0.##}");

            if (d.PelletCount > 1)
                Append($"{d.PelletCount} pellets");

            if (d.FireMode == FireMode.Burst && d.BurstCount > 1)
                Append($"Burst {d.BurstCount}×{d.BurstInterval:0.00}s");

            if (d.FireMode == FireMode.Charge && d.ChargeTime > 0f)
                Append($"Charge {d.ChargeTime:0.0}s");

            if (d.ArmorPenetration > 0f)
                Append($"AP {d.ArmorPenetration * 100f:0}%");

            return _footerBuilder.ToString();
        }

        private static void SetRow(int index, TextMeshProUGUI[] keys, TextMeshProUGUI[] vals, string key, string val)
        {
            keys[index].text = key;
            vals[index].text = val;
        }

        // ── Formatting helpers ────────────────────────────────────────────────

        private static string FireModeLabel(FireMode mode, int burstCount) => mode switch
        {
            FireMode.Semi   => "Semi",
            FireMode.Auto   => "Auto",
            FireMode.Burst  => burstCount > 1 ? $"Burst ×{burstCount}" : "Burst",
            FireMode.Charge => "Charge",
            _               => mode.ToString(),
        };

        private static string AmmoLabel(AmmoType ammo) => ammo switch
        {
            AmmoType.LightRounds    => "Light",
            AmmoType.StandardRounds => "Standard",
            AmmoType.HeavyRounds    => "Heavy",
            AmmoType.ShotgunShells  => "Shells",
            AmmoType.Arrows         => "Arrows",
            AmmoType.EnergyCells    => "Energy",
            AmmoType.Cooldown       => "Cooldown",
            _                       => "None",
        };

        private static Color TierColor(ItemTier tier) => tier switch
        {
            ItemTier.Uncommon  => TierUncommon,
            ItemTier.Rare      => TierRare,
            ItemTier.Epic      => TierEpic,
            ItemTier.Legendary => TierLegendary,
            _                  => TierCommon,
        };

        // ── Panel construction ────────────────────────────────────────────────

        private void BuildPanel()
        {
            _rectTransform.anchorMin        = new Vector2(1f, 1f);
            _rectTransform.anchorMax        = new Vector2(1f, 1f);
            _rectTransform.pivot            = new Vector2(1f, 1f);
            _rectTransform.anchoredPosition = new Vector2(-_edgeOffset.x, -_edgeOffset.y);

            float headerHeight   = 20f;
            float subtitleHeight = 16f;
            float ruleHeight     = 1f;
            float ruleGap        = 8f;
            float statsHeight    = RowsPerColumn * _rowHeight;
            float footerGap      = 6f;
            float footerHeight   = 16f;

            float totalHeight = _padding
                              + headerHeight
                              + subtitleHeight
                              + ruleGap + ruleHeight + ruleGap
                              + statsHeight
                              + ruleGap + ruleHeight + footerGap
                              + footerHeight
                              + _padding;

            _rectTransform.sizeDelta = new Vector2(_panelWidth, totalHeight);

            var bg = UIFactory.MakeImage("PanelBg", _rectTransform);
            bg.color = PanelBg;
            UIFactory.Stretch(bg.rectTransform);

            float y = -_padding;

            _nameText = UIFactory.MakeText("Name", _rectTransform);
            _nameText.fontSize  = 16f;
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.color     = Header;
            _nameText.alignment = TextAlignmentOptions.MidlineLeft;
            PlaceRow(_nameText.rectTransform, y, headerHeight);
            y -= headerHeight;

            _subtitleText = UIFactory.MakeText("Subtitle", _rectTransform);
            _subtitleText.fontSize  = 11f;
            _subtitleText.fontStyle = FontStyles.Bold;
            _subtitleText.color     = TierCommon;
            _subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
            PlaceRow(_subtitleText.rectTransform, y, subtitleHeight);
            y -= subtitleHeight + ruleGap;

            var rule1 = UIFactory.MakeImage("RuleTop", _rectTransform);
            rule1.color = RuleBg;
            PlaceRow(rule1.rectTransform, y, ruleHeight);
            y -= ruleHeight + ruleGap;

            float columnWidth = (_panelWidth - _padding * 2f - 12f) * 0.5f;
            BuildColumn(_leftKeys,  _leftVals,  _padding,                       y, columnWidth);
            BuildColumn(_rightKeys, _rightVals, _padding + columnWidth + 12f,  y, columnWidth);
            y -= statsHeight + ruleGap;

            var rule2 = UIFactory.MakeImage("RuleBottom", _rectTransform);
            rule2.color = RuleBg;
            PlaceRow(rule2.rectTransform, y, ruleHeight);
            y -= ruleHeight + footerGap;

            _footerText = UIFactory.MakeText("Footer", _rectTransform);
            _footerText.fontSize  = 11f;
            _footerText.color     = Footer;
            _footerText.alignment = TextAlignmentOptions.MidlineLeft;
            PlaceRow(_footerText.rectTransform, y, footerHeight);
        }

        private void BuildColumn(TextMeshProUGUI[] keys, TextMeshProUGUI[] vals, float xOffset, float topY, float columnWidth)
        {
            float keyWidth = columnWidth * 0.5f;
            float valWidth = columnWidth - keyWidth;

            for (int i = 0; i < RowsPerColumn; i++)
            {
                float y = topY - i * _rowHeight;

                var key = UIFactory.MakeText($"Key_{xOffset}_{i}", _rectTransform);
                key.fontSize  = 12f;
                key.color     = KeyText;
                key.alignment = TextAlignmentOptions.MidlineLeft;
                key.rectTransform.anchorMin        = new Vector2(0f, 1f);
                key.rectTransform.anchorMax        = new Vector2(0f, 1f);
                key.rectTransform.pivot            = new Vector2(0f, 1f);
                key.rectTransform.anchoredPosition = new Vector2(xOffset, y);
                key.rectTransform.sizeDelta        = new Vector2(keyWidth, _rowHeight);
                keys[i] = key;

                var val = UIFactory.MakeText($"Val_{xOffset}_{i}", _rectTransform);
                val.fontSize  = 12f;
                val.color     = ValText;
                val.alignment = TextAlignmentOptions.MidlineRight;
                val.rectTransform.anchorMin        = new Vector2(0f, 1f);
                val.rectTransform.anchorMax        = new Vector2(0f, 1f);
                val.rectTransform.pivot            = new Vector2(0f, 1f);
                val.rectTransform.anchoredPosition = new Vector2(xOffset + keyWidth, y);
                val.rectTransform.sizeDelta        = new Vector2(valWidth, _rowHeight);
                vals[i] = val;
            }
        }

        private void PlaceRow(RectTransform rowRt, float y, float height)
        {
            rowRt.anchorMin        = new Vector2(0f, 1f);
            rowRt.anchorMax        = new Vector2(1f, 1f);
            rowRt.pivot            = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta        = new Vector2(-_padding * 2f, height);
        }

        private void ShowInternal()
        {
            IsVisible = true;
            SetAlpha(true);
        }

        private void HideInternal()
        {
            IsVisible = false;
            SetAlpha(false);
            _shownWeapon = null;
        }

        private void SetAlpha(bool visible)
        {
            _cg.alpha          = visible ? 1f : 0f;
            _cg.blocksRaycasts = false; // never intercepts clicks — read-only tooltip
            _cg.interactable   = false;
        }
    }
}
