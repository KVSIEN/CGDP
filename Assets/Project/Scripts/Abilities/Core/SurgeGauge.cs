using System;

namespace CGD.Abilities
{
    // One ability's Surge (0..1) and the rules of filling, draining and spending it. Times
    // are game time; Unity-independent so it can be tested on its own.
    public class SurgeGauge
    {
        private const float Epsilon = 0.0001f;

        private float _lastGain = float.NegativeInfinity;

        public float Value { get; private set; }

        public void Gain(float amount, float now)
        {
            if (amount <= 0f) return;
            Value     = Math.Min(1f, Value + amount);
            _lastGain = now;
        }

        public void Tick(float deltaTime, float now, SurgeSettings settings)
        {
            if (settings.DrainPerSecond <= 0f || Value <= settings.DrainFloor) return;
            if (now - _lastGain < settings.DrainDelay) return;
            Value = Math.Max(settings.DrainFloor, Value - settings.DrainPerSecond * deltaTime);
        }

        public bool Has(float amount) => Value + Epsilon >= amount;

        public bool TrySpend(float amount)
        {
            if (!Has(amount)) return false;
            Value = Math.Max(0f, Value - amount);
            return true;
        }

        public void Reset()
        {
            Value     = 0f;
            _lastGain = float.NegativeInfinity;
        }
    }
}
