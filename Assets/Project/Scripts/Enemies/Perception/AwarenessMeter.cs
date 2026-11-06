using UnityEngine;

namespace CGD.Enemies
{
    // An enemy's awareness of a hostile as one 0..1 value: stimuli raise it, time without
    // any lowers it. Full means certain — the enemy engages. The threshold is where it
    // turns suspicious. Kept free of Unity physics so the rules can be tested.
    public sealed class AwarenessMeter
    {
        private readonly float _threshold;

        public AwarenessMeter(float suspiciousThreshold) => _threshold = Mathf.Clamp01(suspiciousThreshold);

        public float Value        { get; private set; }
        public bool  IsFull       => Value >= 1f;
        public bool  IsSuspicious => Value >= _threshold;

        // Raises toward the ceiling; a weak stimulus (hearing) can't make the enemy certain.
        public void Raise(float amount, float ceiling = 1f)
        {
            if (amount <= 0f || Value >= ceiling) return;
            Value = Mathf.Min(Mathf.Clamp01(ceiling), Value + amount);
        }

        // At least suspicious, e.g. on hearing an alarm.
        public void RaiseToSuspicious(float margin = 0.05f) => Raise(_threshold + margin - Value);

        public void Fill() => Value = 1f;

        public void Decay(float amount) => Value = Mathf.Max(0f, Value - Mathf.Max(0f, amount));

        public void Clear() => Value = 0f;
    }
}
