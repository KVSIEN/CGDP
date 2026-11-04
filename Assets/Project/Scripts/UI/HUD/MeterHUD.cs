using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Meters;

namespace CGD.UI
{
    // One bar per meter on the player's MeterSet (stamina, mana, oxygen...), stacked above
    // the health panel. Colours and names come from each MeterDefinition, so a new
    // resource shows up here without touching this class.
    [RequireComponent(typeof(RectTransform))]
    public class MeterHUD : HUDElement
    {
        [SerializeField] private MeterSet _meters;

        [Header("Layout")]
        [Tooltip("Bottom-left corner; leave room for the health panel below")]
        [SerializeField] private Vector2 _screenPadding = new Vector2(20f, 126f);
        [SerializeField] private Vector2 _innerPadding  = new Vector2(12f, 10f);
        [SerializeField] private float   _panelWidth    = 200f;
        [SerializeField] private float   _barHeight     = 6f;
        [SerializeField] private float   _rowSpacing    = 4f;

        private const float LabelHeight = 14f;

        private readonly List<Row> _rows = new();

        private sealed class Row
        {
            public Meter           Meter;
            public Image           Fill;
            public TextMeshProUGUI Label;
            public Action          OnChanged;
        }

        // Built in Start: MeterSet creates its meters in Awake.
        private void Start()
        {
            if (_meters == null || _meters.Meters.Count == 0)
            {
                Hide();
                return;
            }

            Build();
            Refresh();
        }

        private void OnDestroy()
        {
            foreach (Row row in _rows)
                row.Meter.Changed -= row.OnChanged;
        }

        public override void Refresh()
        {
            foreach (Row row in _rows)
                RefreshRow(row);
        }

        private void RefreshRow(Row row)
        {
            Meter meter = row.Meter;
            UIFill.Horizontal(row.Fill, meter.Ratio);
            row.Fill.color = meter.IsExhausted ? Theme.Disabled : meter.Definition.Color;
            row.Label.text = $"{meter.Definition.DisplayName}  {Mathf.CeilToInt(meter.Current)} / {Mathf.CeilToInt(meter.Max)}";
        }

        private void Build()
        {
            var self = GetComponent<RectTransform>();
            UIFactory.AnchorToCorner(self, new Vector2(0f, 0f), _screenPadding);

            if (!BuildSkinnedRows(self)) BuildDefaultRows(self);
        }

        // With a skin, the skin is one meter's row: `Label` and `Fill`. Rows stack downward with
        // the layout's row spacing; there is no panel behind them, so a row brings its own.
        // The fill is tinted by the meter's colour, or the theme's grey while exhausted.
        private bool BuildSkinnedRows(RectTransform self)
        {
            if (!HasSkin) return false;

            float y = 0f;
            float width = 0f;
            foreach (Meter meter in _meters.Meters)
            {
                SkinParts item = NewSkinItem(self);
                Image fill = item.GetImage("Fill");
                TextMeshProUGUI label = item.GetText("Label");
                if (!item.Accept()) return false;

                Vector2 size = item.SizeOr(new Vector2(_panelWidth - _innerPadding.x * 2f, LabelHeight + _barHeight));
                UIFactory.Place(item.Root, new Vector2(0f, y), size);
                y -= size.y + _rowSpacing;
                width = Mathf.Max(width, size.x);

                var row = new Row { Meter = meter, Fill = fill, Label = label };
                row.OnChanged = () => RefreshRow(row);
                meter.Changed += row.OnChanged;
                _rows.Add(row);
            }

            self.sizeDelta = new Vector2(width, -y - _rowSpacing);
            return true;
        }

        private void BuildDefaultRows(RectTransform self)
        {

            var bg = UIFactory.MakeImage("Background", self);
            bg.color = Theme.Panel;
            UIFactory.Stretch(bg.rectTransform);

            float contentWidth = _panelWidth - _innerPadding.x * 2f;
            float y = -_innerPadding.y;

            foreach (Meter meter in _meters.Meters)
            {
                Row row = BuildRow(self, meter, contentWidth, ref y);
                row.OnChanged = () => RefreshRow(row);
                meter.Changed += row.OnChanged;
                _rows.Add(row);
            }

            self.sizeDelta = new Vector2(_panelWidth, -y - _rowSpacing + _innerPadding.y);
        }

        private Row BuildRow(RectTransform parent, Meter meter, float width, ref float y)
        {
            string id = meter.Definition.DisplayName;

            var label = UIFactory.MakeText(id + "Label", parent);
            label.color     = Theme.TextPrimary;
            label.fontSize  = 11f;
            label.alignment = TextAlignmentOptions.Left;
            UIFactory.Place(label.rectTransform, new Vector2(_innerPadding.x, y), new Vector2(width, LabelHeight));
            y -= LabelHeight;

            var barBg = UIFactory.MakeImage(id + "BarBg", parent);
            barBg.color = Theme.BarBackground;
            UIFactory.Place(barBg.rectTransform, new Vector2(_innerPadding.x, y), new Vector2(width, _barHeight));
            y -= _barHeight + _rowSpacing;

            var fill = UIFactory.MakeImage(id + "Fill", barBg.rectTransform);
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            return new Row { Meter = meter, Fill = fill, Label = label };
        }
    }
}
