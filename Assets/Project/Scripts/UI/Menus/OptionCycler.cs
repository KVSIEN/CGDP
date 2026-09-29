using System;
using TMPro;
using UnityEngine;

namespace CGD.UI
{
    // "< value >" picker for a short list of options (window mode, quality level, on/off).
    // Wraps around at both ends; stands in for a dropdown, which UIFactory doesn't build.
    public class OptionCycler
    {
        private readonly TextMeshProUGUI _valueText;
        private readonly Action<int> _onChanged;

        private string[] _options;
        private int _index;

        public OptionCycler(RectTransform parent, Vector2 pos, float width, string[] options, Action<int> onChanged)
        {
            _options   = options;
            _onChanged = onChanged;

            const float ArrowWidth = 24f;
            var prev = UIFactory.MakeButton("Prev", parent, "<", out _);
            UIFactory.Place(prev.GetComponent<RectTransform>(), pos, new Vector2(ArrowWidth, 20f));
            prev.onClick.AddListener(() => Step(-1));

            _valueText = UIFactory.MakeText("Value", parent);
            _valueText.fontSize  = 12f;
            _valueText.alignment = TextAlignmentOptions.Center;
            UIFactory.Place(_valueText.rectTransform, pos + new Vector2(ArrowWidth, 0f), new Vector2(width - ArrowWidth * 2f, 20f));

            var next = UIFactory.MakeButton("Next", parent, ">", out _);
            UIFactory.Place(next.GetComponent<RectTransform>(), pos + new Vector2(width - ArrowWidth, 0f), new Vector2(ArrowWidth, 20f));
            next.onClick.AddListener(() => Step(1));
        }

        public void SetOptions(string[] options, int index)
        {
            _options = options;
            SetIndexWithoutNotify(index);
        }

        public void SetIndexWithoutNotify(int index)
        {
            _index = _options.Length == 0 ? 0 : Mathf.Clamp(index, 0, _options.Length - 1);
            _valueText.text = _options.Length == 0 ? "-" : _options[_index];
        }

        private void Step(int direction)
        {
            if (_options.Length == 0) return;

            SetIndexWithoutNotify((_index + direction + _options.Length) % _options.Length);
            _onChanged(_index);
        }
    }
}
