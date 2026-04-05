using System;
using UnityEngine;

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

    public int Evaluate()
    {
        float t   = UnityEngine.Random.value;
        float exp = Mathf.Lerp(3f, 1f / 3f, (Bias + 1f) * 0.5f);
        return Mathf.RoundToInt(Mathf.Lerp(Min, Max, Mathf.Pow(t, exp)));
    }
}
