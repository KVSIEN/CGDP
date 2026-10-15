using System;

namespace CGD.Director
{
    // Rubber-banding over the session: each peak the player comes out of comfortably nudges
    // a rating up, each one that nearly killed them (or a death) nudges it down. The rating
    // becomes a pressure multiplier, 1 at neutral, that the director's systems scale by —
    // bounded on both sides and moving one step at a time, so it adapts without whiplash.
    public class SessionPerformance
    {
        private readonly PerformanceTuning _tuning;

        public SessionPerformance(PerformanceTuning tuning) => _tuning = tuning;

        // -1 (struggling) to 1 (cruising).
        public float Rating { get; private set; }

        public float Pressure => Rating >= 0f
            ? 1f + (_tuning.MaxPressure - 1f) * Rating
            : 1f + (1f - _tuning.MinPressure) * Rating;

        // lowestHealth: the smallest share of health the player was down to during the peak.
        public void RecordPeak(float lowestHealth)
        {
            if (lowestHealth >= _tuning.ComfortableHealth) Shift(_tuning.Step);
            else if (lowestHealth <= _tuning.StrugglingHealth) Shift(-_tuning.Step);
        }

        public void RecordDeath() => Shift(-_tuning.DeathStep);

        private void Shift(float amount) => Rating = Math.Clamp(Rating + amount, -1f, 1f);
    }
}
