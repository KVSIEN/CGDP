using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.Combat;

namespace CGD.UI
{
    // Row of active status effects above the ability bar: one tile per effect with its
    // icon (or colored tile and short name), stack count, and a shrinking overlay
    // showing the remaining duration.
    [RequireComponent(typeof(RectTransform))]
    public class StatusEffectHUD : HUDElement
    {
        [SerializeField] private StatusEffectController _target;

        [Header("Layout")]
        [SerializeField] private int   _maxTiles            = 8;
        [SerializeField] private float _tileSize            = 36f;
        [SerializeField] private float _tileGap             = 6f;
        [SerializeField] private float _screenPaddingBottom = 92f;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.55f);

        private readonly List<ActiveStatus> _active = new();
        private readonly Dictionary<StatusEffect, string> _shortNames = new();
        private Image[]           _tiles;
        private Image[]           _overlays;
        private TextMeshProUGUI[] _names;
        private TextMeshProUGUI[] _stacks;

        private void Awake()
        {
            var rt              = GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, _screenPaddingBottom);
            rt.sizeDelta        = new Vector2(_maxTiles * (_tileSize + _tileGap) - _tileGap, _tileSize);

            _tiles    = new Image[_maxTiles];
            _overlays = new Image[_maxTiles];
            _names    = new TextMeshProUGUI[_maxTiles];
            _stacks   = new TextMeshProUGUI[_maxTiles];

            for (int i = 0; i < _maxTiles; i++)
                BuildTile(rt, i);
        }

        private void BuildTile(RectTransform parent, int index)
        {
            var tile = UIFactory.MakeImage("Status_" + index, parent);
            var tileRt = tile.rectTransform;
            tileRt.anchorMin = tileRt.anchorMax = tileRt.pivot = new Vector2(0f, 0.5f);
            tileRt.anchoredPosition = new Vector2(index * (_tileSize + _tileGap), 0f);
            tileRt.sizeDelta = Vector2.one * _tileSize;

            var overlay = UIFactory.MakeImage("Remaining", tileRt);
            overlay.color = OverlayColor;
            UIFactory.Stretch(overlay.rectTransform);

            var nameText = UIFactory.MakeText("Name", tileRt);
            nameText.fontSize  = 9f;
            nameText.alignment = TextAlignmentOptions.Center;
            UIFactory.Stretch(nameText.rectTransform);

            var stackText = UIFactory.MakeText("Stacks", tileRt);
            stackText.fontSize  = 10f;
            stackText.alignment = TextAlignmentOptions.BottomRight;
            UIFactory.Stretch(stackText.rectTransform);
            stackText.rectTransform.offsetMax = new Vector2(-2f, 0f);

            tile.gameObject.SetActive(false);
            _tiles[index]    = tile;
            _overlays[index] = overlay;
            _names[index]    = nameText;
            _stacks[index]   = stackText;
        }

        private void Update()
        {
            if (_target == null) return;

            _target.GetActive(_active);
            for (int i = 0; i < _maxTiles; i++)
            {
                bool used = i < _active.Count;
                if (_tiles[i].gameObject.activeSelf != used)
                    _tiles[i].gameObject.SetActive(used);
                if (used) Draw(i, _active[i]);
            }
        }

        private void Draw(int index, ActiveStatus status)
        {
            StatusEffect effect = status.Effect;
            Image tile = _tiles[index];

            tile.sprite = effect.Icon;
            tile.color  = effect.Icon != null ? Color.white : effect.Color;

            _names[index].text = effect.Icon != null ? string.Empty : ShortName(effect);
            _stacks[index].text = status.Stacks > 1 ? StackLabel(status.Stacks) : string.Empty;

            // Overlay covers the elapsed part, growing downward from the top.
            _overlays[index].rectTransform.anchorMin = new Vector2(0f, status.RemainingRatio);
        }

        private string ShortName(StatusEffect effect)
        {
            if (_shortNames.TryGetValue(effect, out string name)) return name;

            name = effect.DisplayName.Length > 4 ? effect.DisplayName.Substring(0, 4) : effect.DisplayName;
            name = name.ToUpperInvariant();
            _shortNames[effect] = name;
            return name;
        }

        private static readonly string[] StackLabels = { "", "", "x2", "x3", "x4", "x5", "x6", "x7", "x8", "x9" };

        // Cached labels avoid a per-frame string allocation for common stack counts.
        private static string StackLabel(int stacks) =>
            stacks < StackLabels.Length ? StackLabels[stacks] : "x" + stacks;

        public override void Refresh() { }
    }
}
