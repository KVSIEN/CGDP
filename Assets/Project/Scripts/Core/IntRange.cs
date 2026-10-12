using System;
using UnityEngine;

namespace CGD.Core
{
    [Serializable]
    public struct IntRange
    {
        public int Min;
        public int Max;
        [Range(-1f, 1f), Tooltip("Negative skews toward Min, positive toward Max")]
        public float Bias;

        public IntRange(int min, int max, float bias = 0f)
        {
            Min = min; Max = max; Bias = bias;
        }

        public int Evaluate() => Lerp(UnityEngine.Random.value);

        public int Evaluate(RandomStream random) => Lerp(random.Value);

        // Samples at an explicit 0..1 position instead of a random one, applying the
        // same bias curve. See FloatRange.Lerp.
        public int Lerp(float t) =>
            Mathf.RoundToInt(Mathf.Lerp(Min, Max, Mathf.Pow(Mathf.Clamp01(t), FloatRange.BiasExponent(Bias))));
    }
}
