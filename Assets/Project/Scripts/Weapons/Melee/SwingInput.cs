using CGD.Input;

namespace CGD.Weapons
{
    // One melee hand's attack button: released after a tap it asks for a light attack, after a
    // hold past the weapon's threshold for a heavy one. Remembers a release made while the
    // player couldn't act (input buffering) and can swallow a press that already did
    // something else (started a bash), so its release doesn't swing as well.
    public sealed class SwingInput
    {
        private readonly InputBuffer _queued = new();
        private bool  _queuedHeavy;
        private bool  _heldLastFrame;
        private float _holdTimer;
        private bool  _swallowed;

        // Feeds this frame's button state; true on the frame it is released.
        public bool Read(bool held, float deltaTime, float heavyThreshold, out bool heavy)
        {
            if (!held) _swallowed = false;
            if (_swallowed) held = false;

            bool released = _heldLastFrame && !held;
            _heldLastFrame = held;
            if (held) _holdTimer += deltaTime;

            heavy = released && _holdTimer >= heavyThreshold;
            if (released) _holdTimer = 0f;
            return released;
        }

        // The press still down was used for something else: ignore it until it is let go.
        public void Swallow()
        {
            _swallowed = true;
            Reset();
        }

        public void Reset()
        {
            _heldLastFrame = false;
            _holdTimer     = 0f;
        }

        public void Queue(float now, bool heavy)
        {
            _queued.Press(now);
            _queuedHeavy = heavy;
        }

        public bool ConsumeQueued(float now, out bool heavy)
        {
            heavy = _queuedHeavy;
            return _queued.Consume(now);
        }

        public void ClearQueue() => _queued.Clear();
    }
}
