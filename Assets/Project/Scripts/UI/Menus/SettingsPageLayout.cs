using System;
using UnityEngine;

namespace CGD.UI
{
    // Stacks "label — control" rows down a settings page so each page only lists its options.
    public class SettingsPageLayout
    {
        private const float RowHeight = 26f;

        private readonly RectTransform _page;
        private readonly float _left;
        private readonly float _labelWidth;
        private readonly float _controlWidth;
        private float _y = -20f;

        // A page lists one column of rows by default; a page with more options gives each
        // column its own layout, starting at `left` with narrower label and control widths.
        public SettingsPageLayout(RectTransform page, float left = 10f, float labelWidth = 170f, float controlWidth = 300f)
        {
            _page         = page;
            _left         = left;
            _labelWidth   = labelWidth;
            _controlWidth = controlWidth;
        }

        // Where the next row would go, for placing something below the last one.
        public Vector2 Cursor => new(_left, _y);

        public void Header(string title)
        {
            if (_y < -20f) _y -= 8f;
            var text = UIFactory.MakeText(title + "Header", _page);
            text.text     = $"── {title} ──";
            text.fontSize = 13f;
            UIFactory.Place(text.rectTransform, new Vector2(_left, _y), new Vector2(_labelWidth + _controlWidth, 20f));
            _y -= RowHeight;
        }

        public LabeledSlider Slider(string label, float min, float max, Func<float, string> format, Action<float> onChanged)
        {
            var slider = new LabeledSlider(_page, ControlPosition(label), _controlWidth, min, max, format, onChanged);
            _y -= RowHeight;
            return slider;
        }

        public OptionCycler Options(string label, string[] options, Action<int> onChanged)
        {
            var cycler = new OptionCycler(_page, ControlPosition(label), _controlWidth, options, onChanged);
            _y -= RowHeight;
            return cycler;
        }

        public OptionCycler Toggle(string label, Action<bool> onChanged) =>
            Options(label, OnOff, i => onChanged(i == 1));

        public static int ToggleIndex(bool value) => value ? 1 : 0;

        public static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

        private static readonly string[] OnOff = { "Off", "On" };

        private Vector2 ControlPosition(string label)
        {
            var text = UIFactory.MakeText(label + "Label", _page);
            text.text     = label;
            text.fontSize = 12f;
            UIFactory.Place(text.rectTransform, new Vector2(_left, _y), new Vector2(_labelWidth, 20f));
            return new Vector2(_left + _labelWidth, _y);
        }
    }
}
