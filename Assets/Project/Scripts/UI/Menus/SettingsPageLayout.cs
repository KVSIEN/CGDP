using System;
using UnityEngine;

namespace CGD.UI
{
    // Stacks "label — control" rows down a settings page so each page only lists its options.
    public class SettingsPageLayout
    {
        private const float LabelWidth   = 170f;
        private const float ControlWidth = 300f;
        private const float RowHeight    = 26f;
        private const float Margin       = 10f;

        private readonly RectTransform _page;
        private float _y = -20f;

        public SettingsPageLayout(RectTransform page) => _page = page;

        public void Header(string title)
        {
            if (_y < -20f) _y -= 8f;
            var text = UIFactory.MakeText(title + "Header", _page);
            text.text     = $"── {title} ──";
            text.fontSize = 13f;
            UIFactory.Place(text.rectTransform, new Vector2(Margin, _y), new Vector2(580f, 20f));
            _y -= RowHeight;
        }

        public LabeledSlider Slider(string label, float min, float max, Func<float, string> format, Action<float> onChanged)
        {
            var slider = new LabeledSlider(_page, ControlPosition(label), ControlWidth, min, max, format, onChanged);
            _y -= RowHeight;
            return slider;
        }

        public OptionCycler Options(string label, string[] options, Action<int> onChanged)
        {
            var cycler = new OptionCycler(_page, ControlPosition(label), ControlWidth, options, onChanged);
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
            UIFactory.Place(text.rectTransform, new Vector2(Margin, _y), new Vector2(LabelWidth, 20f));
            return new Vector2(Margin + LabelWidth, _y);
        }
    }
}
