using UnityEngine;

namespace CGD.Enemies
{
    // How much pressure a faction has been putting on its targets lately (after Alien:
    // Isolation's director): it climbs while members are in contact — faster with more of
    // them, up to three — and drains while none are. Past the back-off line the faction pulls
    // away for a while, and only comes again once it has drained below the resume line, so the
    // hunt breathes in and out instead of grinding on. Pure, so it can be tested.
    public sealed class MenaceGauge
    {
        private const int MaxCounted = 3;

        private readonly float _rise;
        private readonly float _decay;
        private readonly float _max;
        private readonly bool  _backsOff;
        private readonly float _backOffAt;
        private readonly float _backOffSeconds;
        private readonly float _resumeBelow;
        private float _backingOffUntil;

        public MenaceGauge(float rise, float decay, float max, bool backsOff, float backOffAt, float backOffSeconds, float resumeBelow)
        {
            _rise           = Mathf.Max(0f, rise);
            _decay          = Mathf.Max(0f, decay);
            _max            = Mathf.Max(0.01f, max);
            _backsOff       = backsOff;
            _backOffAt      = backOffAt;
            _backOffSeconds = backOffSeconds;
            _resumeBelow    = resumeBelow;
        }

        public float Value        { get; private set; }
        public float Share        => Value / _max;
        public bool  IsBackingOff { get; private set; }

        public void Tick(float deltaTime, int inContact, float time)
        {
            Value = inContact > 0
                ? Mathf.Min(_max, Value + _rise * Mathf.Min(inContact, MaxCounted) * deltaTime)
                : Mathf.Max(0f, Value - _decay * deltaTime);

            if (!IsBackingOff && _backsOff && Value >= _backOffAt)
            {
                IsBackingOff     = true;
                _backingOffUntil = time + _backOffSeconds;
            }
            else if (IsBackingOff && time >= _backingOffUntil && Value <= _resumeBelow)
            {
                IsBackingOff = false;
            }
        }
    }
}
