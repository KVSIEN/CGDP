using UnityEngine;
using UnityEngine.UI;

namespace CGD.UI
{
    // Full-screen colour flash that fades out — a pickup glint, a heal, a warning pulse.
    // A new flash replaces a weaker one still fading, never cuts a stronger one short.
    // (Taking damage and low health stay on HitEffect.)
    [RequireComponent(typeof(RectTransform))]
    public class ScreenFlashHUD : HUDElement
    {
        private Image _image;
        private Color _color;
        private float _duration;
        private float _remaining;
        private float _strength;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            UIFactory.Stretch(self);

            _image = UIFactory.MakeImage("Flash", self);
            _image.color = Color.clear;
            UIFactory.Stretch(_image.rectTransform);
        }

        // intensity scales the colour's alpha, 0..1.
        public void Flash(Color color, float duration, float intensity = 1f)
        {
            if (_image == null || duration <= 0f) return;

            float strength = color.a * Mathf.Clamp01(intensity);
            if (CurrentAlpha() > strength) return;

            _color     = color;
            _strength  = strength;
            _duration  = duration;
            _remaining = duration;
            Apply();
        }

        public override void Refresh() { }

        private void Update()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.unscaledDeltaTime;
            Apply();
        }

        private float CurrentAlpha() => _remaining > 0f ? _strength * (_remaining / _duration) : 0f;

        private void Apply() => _image.color = new Color(_color.r, _color.g, _color.b, CurrentAlpha());
    }
}
