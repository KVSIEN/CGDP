using System;
using UnityEngine;

namespace CGD.Stats
{
    // A standalone attribute that owns its base value — strength, level-scaled health,
    // a machine's output — plus the modifiers stacked on it. Value is cached and clamped.
    public class Stat
    {
        private float _baseValue;
        private float _value;

        public Stat(float baseValue, float min = float.MinValue, float max = float.MaxValue)
        {
            Min = min;
            Max = max;
            Modifiers.Changed += Recalculate;
            BaseValue = baseValue;
        }

        public float Min { get; }
        public float Max { get; }

        public ModifierStack Modifiers { get; } = new();

        public float BaseValue
        {
            get => _baseValue;
            set
            {
                _baseValue = value;
                Recalculate();
            }
        }

        public float Value => _value;

        public event Action<float> Changed;

        private void Recalculate()
        {
            float value = Mathf.Clamp(Modifiers.Apply(_baseValue), Min, Max);
            if (Mathf.Approximately(value, _value)) return;

            _value = value;
            Changed?.Invoke(value);
        }
    }
}
