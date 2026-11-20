using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // The lasting part of an ability's look on an enemy: a translucent shell round its body
    // that pulses while a buff lasts, with an optional ring at its feet and a trail, fading in
    // and out at either end. The most recent buff takes over the colour.
    internal sealed class BuffAura
    {
        private const float PulseSpeed = 6f;     // radians per second
        private const float FadeTime   = 0.35f;
        private const float RingBoost  = 1.6f;   // the ring reads stronger than the shell

        private readonly Renderer      _shell;
        private readonly Renderer      _ring;
        private readonly TrailRenderer _trail;

        private Color _color;
        private float _startedAt;
        private float _endsAt = -1f;   // < 0 = hidden
        private bool  _ringOn;
        private bool  _trailOn;

        public BuffAura(Renderer shell, Renderer ring, TrailRenderer trail)
        {
            _shell = shell;
            _ring  = ring;
            _trail = trail;
            Hide();
        }

        public void Show(Color color, float seconds, bool ring, bool trail, float now)
        {
            _color     = color;
            _startedAt = now;
            _endsAt    = now + seconds;
            _ringOn    = ring && _ring != null;
            _trailOn   = trail && _trail != null;

            _shell.enabled = true;
            if (_ring != null) _ring.enabled = _ringOn;
            if (_trail == null) return;
            _trail.emitting = _trailOn;
            if (_trailOn) _trail.SetTint(color);
        }

        public void Hide()
        {
            _endsAt = -1f;
            _shell.enabled = false;
            if (_ring != null) _ring.enabled = false;
            if (_trail == null) return;
            _trail.emitting = false;
            _trail.Clear();
        }

        public void Tick(float now)
        {
            if (_endsAt < 0f) return;
            if (now >= _endsAt)
            {
                Hide();
                return;
            }

            float fade  = Mathf.Clamp01((_endsAt - now) / FadeTime) * Mathf.Clamp01((now - _startedAt) / FadeTime);
            float pulse = 0.75f + 0.25f * Mathf.Sin((now - _startedAt) * PulseSpeed);
            Color color = _color;
            color.a = _color.a * fade * pulse;
            _shell.SetTint(color);

            if (!_ringOn) return;
            color.a = Mathf.Min(1f, _color.a * RingBoost * fade);
            _ring.SetTint(color);
        }
    }
}
