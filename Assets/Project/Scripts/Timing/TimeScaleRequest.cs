namespace CGD.Timing
{
    // One reason time is running at a different speed (slow motion, hit stop, a debug
    // fast-forward). Requests stack by multiplying, so two systems can slow time without
    // either undoing the other; release the request to remove its effect.
    public sealed class TimeScaleRequest
    {
        private readonly GameClock _clock;

        internal TimeScaleRequest(GameClock clock, float scale, float realDuration)
        {
            _clock        = clock;
            Scale         = scale;
            RealRemaining = realDuration;
        }

        public float Scale    { get; }
        public bool  IsActive { get; internal set; } = true;

        // Unscaled seconds left, or 0 for a request that lasts until released. Counted in
        // real time so a 0.1 s hit stop doesn't stretch itself out.
        internal float RealRemaining { get; set; }

        public void Release() => _clock.Release(this);
    }
}
