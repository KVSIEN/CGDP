using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CGD.UI
{
    // Slider with a live value readout, for settings that change as you drag.
    public class LabeledSlider
    {
        private readonly Slider _slider;
        private readonly TextMeshProUGUI _valueText;
        private readonly Func<float, string> _format;

        public LabeledSlider(RectTransform parent, Vector2 pos, float width, float min, float max,
            Func<float, string> format, Action<float> onChanged)
        {
            _format = format;

            const float ValueWidth = 60f;
            _slider = UIFactory.MakeSlider("Slider", parent);
            UIFactory.Place(_slider.GetComponent<RectTransform>(), pos + new Vector2(0f, -2f), new Vector2(width - ValueWidth - 8f, 16f));
            _slider.minValue = min;
            _slider.maxValue = max;

            _valueText = UIFactory.MakeText("Value", parent);
            _valueText.fontSize = 12f;
            UIFactory.Place(_valueText.rectTransform, pos + new Vector2(width - ValueWidth, 0f), new Vector2(ValueWidth, 20f));

            _slider.onValueChanged.AddListener(v =>
            {
                _valueText.text = _format(v);
                onChanged(v);
            });
        }

        public void SetValueWithoutNotify(float value)
        {
            _slider.SetValueWithoutNotify(value);
            _valueText.text = _format(_slider.value);
        }
    }
}
