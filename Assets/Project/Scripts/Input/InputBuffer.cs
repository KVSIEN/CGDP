namespace CGD.Input
{
    // Remembers a press the character couldn't act on yet (mid-draw, mid-dodge, between
    // shots) so it happens as soon as it can, if it comes within Window. Callers only feed
    // it when the player has input buffering turned on.
    public class InputBuffer
    {
        public const float Window = 0.4f;

        private float _pressedAt = float.NegativeInfinity;

        public void Press(float now) => _pressedAt = now;

        public void Clear() => _pressedAt = float.NegativeInfinity;

        // True once for a press still inside the window, which it then uses up.
        public bool Consume(float now)
        {
            if (now - _pressedAt > Window) return false;
            Clear();
            return true;
        }
    }
}
