using UnityEngine;

namespace CGD.UI
{
    // The fill of a bar plus a lighter "trail" behind it. When the value drops the fill
    // jumps down at once and the trail stays where it was for a moment, then shrinks onto it,
    // so the size of the hit stays visible as a band. When the value rises the trail goes with it.
    public class TrailingFill
    {
        private readonly RectTransform _fill;
        private readonly RectTransform _trail;
        private readonly float _holdTime;
        private readonly float _shrinkSpeed;

        private float _value = 1f;
        private float _trailValue = 1f;
        private float _hold;

        // holdTime: seconds the trail waits before shrinking. shrinkSpeed: share of the full bar per second.
        public TrailingFill(RectTransform fill, RectTransform trail, float holdTime, float shrinkSpeed)
        {
            _fill        = fill;
            _trail       = trail;
            _holdTime    = holdTime;
            _shrinkSpeed = shrinkSpeed;
        }

        public void Set(float value)
        {
            value = Mathf.Clamp01(value);
            if (value < _value) _hold = _holdTime;

            _value      = value;
            _trailValue = Mathf.Max(_trailValue, value);
            Apply();
        }

        public void Tick(float deltaTime)
        {
            if (_trailValue <= _value) return;

            if (_hold > 0f)
            {
                _hold -= deltaTime;
                return;
            }

            _trailValue = Mathf.MoveTowards(_trailValue, _value, _shrinkSpeed * deltaTime);
            Apply();
        }

        private void Apply()
        {
            SetWidth(_fill, _value);
            SetWidth(_trail, _trailValue);
        }

        private static void SetWidth(RectTransform rect, float share)
        {
            rect.anchorMax = new Vector2(share, 1f);
            rect.offsetMax = Vector2.zero;
        }
    }
}
