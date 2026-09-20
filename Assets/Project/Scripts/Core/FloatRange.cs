using System;
using UnityEngine;

namespace CGD.Core
{
    // Uniformly random by default. Bias < 0 skews toward Min, bias > 0 toward Max.
    [Serializable]
    public struct FloatRange
    {
        public float Min;
        public float Max;
        [Range(-1f, 1f), Tooltip("Negative skews toward Min, positive toward Max")]
        public float Bias;

        public FloatRange(float min, float max, float bias = 0f)
        {
            Min = min; Max = max; Bias = bias;
        }

        public float Evaluate() => Lerp(UnityEngine.Random.value);

        // Samples at an explicit 0..1 position instead of a random one, applying the
        // same bias curve. Lets a caller decide where in the range a value lands
        // (see ItemRoll) while keeping authored bias meaningful.
        public float Lerp(float t)
        {
            float exp = Mathf.Lerp(3f, 1f / 3f, (Bias + 1f) * 0.5f);
            return Mathf.Lerp(Min, Max, Mathf.Pow(Mathf.Clamp01(t), exp));
        }

        // Clamp the result to the range (guards against floating-point edge cases)
        public float EvaluateClamped() => Mathf.Clamp(Evaluate(), Min, Max);
    }
}
