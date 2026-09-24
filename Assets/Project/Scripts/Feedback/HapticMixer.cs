using System.Collections.Generic;
using UnityEngine;

namespace CGD.Feedback
{
    // Combines overlapping vibration pulses into one pair of motor speeds. Each pulse
    // fades out linearly over its duration; overlapping pulses add, capped at full speed,
    // so a burst of hits builds up instead of the last one cutting off the rest.
    public class HapticMixer
    {
        private readonly List<Pulse> _pulses = new();

        public bool IsActive => _pulses.Count > 0;

        public void Add(float low, float high, float duration)
        {
            if (duration <= 0f || (low <= 0f && high <= 0f)) return;
            _pulses.Add(new Pulse(low, high, duration));
        }

        public void Clear() => _pulses.Clear();

        public void Tick(float deltaTime, out float low, out float high)
        {
            low = high = 0f;

            for (int i = _pulses.Count - 1; i >= 0; i--)
            {
                Pulse pulse = _pulses[i];
                pulse.Remaining -= deltaTime;
                if (pulse.Remaining <= 0f)
                {
                    _pulses.RemoveAt(i);
                    continue;
                }

                _pulses[i] = pulse;
                float fade = pulse.Remaining / pulse.Duration;
                low  += pulse.Low * fade;
                high += pulse.High * fade;
            }

            low  = Mathf.Clamp01(low);
            high = Mathf.Clamp01(high);
        }

        private struct Pulse
        {
            public Pulse(float low, float high, float duration)
            {
                Low       = low;
                High      = high;
                Duration  = duration;
                Remaining = duration;
            }

            public float Low       { get; }
            public float High      { get; }
            public float Duration  { get; }
            public float Remaining { get; set; }
        }
    }
}
