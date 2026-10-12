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

        public float Evaluate(RandomStream random) => Lerp(random.Value);

        // Samples at an explicit 0..1 position instead of a random one, applying the
        // same bias curve. Lets a caller decide where in the range a value lands
        // (see ItemRoll) while keeping authored bias meaningful.
        public float Lerp(float t) => Mathf.Lerp(Min, Max, Mathf.Pow(Mathf.Clamp01(t), BiasExponent(Bias)));

        // The power a position is raised to: 1 (uniform) at bias 0, 3 at -1 (toward Min),
        // 1/3 at +1 (toward Max), symmetric in between.
        internal static float BiasExponent(float bias) => Mathf.Pow(3f, -Mathf.Clamp(bias, -1f, 1f));

        // Clamp the result to the range (guards against floating-point edge cases)
        public float EvaluateClamped() => Mathf.Clamp(Evaluate(), Min, Max);

        public float EvaluateClamped(RandomStream random) => Mathf.Clamp(Evaluate(random), Min, Max);
    }
}
