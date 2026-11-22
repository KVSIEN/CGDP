using UnityEngine;

namespace CGD.Enemies
{
    // How much a carried shield takes before it breaks, and when it is back up. A broken shield
    // stays down for a while and then returns at full strength, so focused fire opens a window
    // rather than removing the shield for good. Plain logic; FrontalShield feeds it hits and time.
    public sealed class ShieldDurability
    {
        private readonly float _max;
        private readonly float _downSeconds;
        private float _left;
        private float _upAt;

        // `max` of 0 makes a shield that never breaks.
        public ShieldDurability(float max, float downSeconds)
        {
            _max         = Mathf.Max(0f, max);
            _downSeconds = Mathf.Max(0f, downSeconds);
            Restore();
        }

        public bool Breakable => _max > 0f;

        public bool IsUp(float now)
        {
            RecoverIfDue(now);
            return !Breakable || _left > 0f;
        }

        // 0..1 of its strength left (0 while down).
        public float Share(float now) => !Breakable ? 1f : IsUp(now) ? _left / _max : 0f;

        // The shield stopped `stopped` damage. True when that broke it.
        public bool Absorb(float stopped, float now)
        {
            if (!IsUp(now) || !Breakable || stopped <= 0f) return false;

            _left -= stopped;
            if (_left > 0f) return false;

            _left = 0f;
            _upAt = now + _downSeconds;
            return true;
        }

        // Back to full at once (a respawn).
        public void Restore()
        {
            _left = _max;
            _upAt = 0f;
        }

        private void RecoverIfDue(float now)
        {
            if (Breakable && _left <= 0f && now >= _upAt) _left = _max;
        }
    }
}
