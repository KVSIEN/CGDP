using UnityEngine;

namespace CGD.Interaction
{
    // Progress (0..1) toward finishing an effortful action, filled either by holding a key or by
    // tapping it repeatedly. Plain logic with no input of its own: the caller says whether the
    // key is held or was just tapped, and is told the moment the effort is complete.
    public sealed class EffortMeter
    {
        // Seconds after a tap before the bar starts draining, so a steady tapping pace is not
        // punished and only stopping is.
        public const float DrainDelay = 0.35f;

        private const float Epsilon = 0.0001f;

        private float _progress;
        private float _secondsSinceTap;
        private bool  _holdCompleted;

        public float Progress => _progress;

        public void Reset()
        {
            _progress        = 0f;
            _secondsSinceTap = 0f;
            _holdCompleted   = false;
        }

        // Call every frame. Progress rises while the key is held and is lost on release.
        // True on the frame it completes; the key must then be released before it can fill again.
        public bool Hold(bool held, float duration, float deltaTime)
        {
            if (!held)
            {
                Reset();
                return false;
            }

            if (_holdCompleted) return false;

            _progress = Mathf.Min(1f, _progress + deltaTime / Mathf.Max(duration, Epsilon));
            if (_progress < 1f) return false;

            _progress      = 0f;
            _holdCompleted = true;
            return true;
        }

        // Call on each tap. True when this tap completes the effort, which starts over at once
        // (a node with several harvests can be mashed through them in turn).
        public bool Tap(int tapsRequired)
        {
            _secondsSinceTap = 0f;
            _progress += 1f / Mathf.Max(1, tapsRequired);
            if (_progress < 1f - Epsilon) return false;

            _progress = 0f;
            return true;
        }

        // Call every frame while mashing. `perSecond` is the share of the bar lost per second
        // once the player has stopped tapping; 0 never drains.
        public void Drain(float perSecond, float deltaTime)
        {
            _secondsSinceTap += deltaTime;
            if (perSecond <= 0f || _secondsSinceTap < DrainDelay) return;

            _progress = Mathf.Max(0f, _progress - perSecond * deltaTime);
        }
    }
}
