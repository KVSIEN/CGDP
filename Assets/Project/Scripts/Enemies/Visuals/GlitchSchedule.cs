using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // When a glitching body blinks out and back (HorrorPresence): while it glitches, every so
    // often (Every) it vanishes for a few hundredths of a second (Length); a burst blinks
    // several times in quick succession whatever it is doing. Not glitching, it never blinks.
    public sealed class GlitchSchedule
    {
        // Gap between the blinks of a burst.
        public const float BurstGap = 0.07f;

        private readonly FloatRange _every;
        private readonly FloatRange _length;
        private float _untilNext;
        private float _hiddenLeft;
        private int   _burstLeft;

        public GlitchSchedule(FloatRange every, FloatRange length)
        {
            _every     = every;
            _length    = length;
            _untilNext = every.EvaluateClamped();
        }

        public bool IsHidden    { get; private set; }
        // A blink began on this tick: the moment for the body to twitch to a new place.
        public bool JustBlinked { get; private set; }

        // Blinks `blinks` times in quick succession, starting now.
        public void Burst(int blinks)
        {
            _burstLeft = Mathf.Max(_burstLeft, blinks);
            _untilNext = 0f;
        }

        public void Tick(float deltaTime, bool glitching)
        {
            JustBlinked = false;
            if (IsHidden)
            {
                _hiddenLeft -= deltaTime;
                if (_hiddenLeft <= 0f) IsHidden = false;
                return;
            }

            if (!glitching && _burstLeft == 0) return;

            _untilNext -= deltaTime;
            if (_untilNext > 0f) return;

            IsHidden    = true;
            JustBlinked = true;
            _hiddenLeft = _length.EvaluateClamped();
            if (_burstLeft > 0) _burstLeft--;
            _untilNext = _burstLeft > 0 ? BurstGap : _every.EvaluateClamped();
        }

        public void Reset()
        {
            IsHidden    = false;
            JustBlinked = false;
            _burstLeft  = 0;
            _untilNext  = _every.EvaluateClamped();
        }
    }
}
