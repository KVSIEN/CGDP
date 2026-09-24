using UnityEngine;

namespace CGD.Meters
{
    // The numbers a Meter runs on. Normally read from a MeterDefinition asset, but
    // systems with their own stats (e.g. HealthManager's shield) build one directly.
    public readonly struct MeterSettings
    {
        public readonly float Max;
        public readonly float RegenRate;
        public readonly float RegenDelay;
        public readonly float StartRatio;
        public readonly float ExhaustionRecovery;

        public MeterSettings(float max, float regenRate, float regenDelay,
                             float startRatio = 1f, float exhaustionRecovery = 0f)
        {
            Max                = Mathf.Max(0f, max);
            RegenRate          = regenRate;
            RegenDelay         = Mathf.Max(0f, regenDelay);
            StartRatio         = Mathf.Clamp01(startRatio);
            ExhaustionRecovery = Mathf.Clamp01(exhaustionRecovery);
        }
    }
}
