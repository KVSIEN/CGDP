using System;

namespace CGD.Director
{
    // The player's emotional intensity, 0 (calm) to 1 (overwhelmed) — the "stress meter".
    // Stressful moments push it up; it only drains once the fight is over and nothing
    // stressful has happened for a moment, so a long fight keeps it high.
    public class IntensityMeter
    {
        private readonly IntensityTuning _tuning;
        private float _sinceStress;

        public IntensityMeter(IntensityTuning tuning) => _tuning = tuning;

        public float Value { get; private set; }

        public void Add(float stress)
        {
            if (stress <= 0f) return;

            Value = Math.Min(1f, Value + stress);
            _sinceStress = 0f;
        }

        public void Fill() => Add(1f);

        public void Clear()
        {
            Value = 0f;
            _sinceStress = 0f;
        }

        public void Tick(float deltaTime, bool inCombat)
        {
            _sinceStress += deltaTime;
            if (inCombat || _sinceStress < _tuning.DecayDelay) return;

            Value = Math.Max(0f, Value - _tuning.DecayRate * deltaTime);
        }
    }
}
