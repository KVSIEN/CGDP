using UnityEngine;

namespace CGD.Algorithms
{
    // Easing functions: curves that turn a straight 0..1 progress into a movement with character.
    //
    // How they work: each takes t from 0 (start) to 1 (end) and returns a reshaped value that
    // also starts at 0 and ends at 1 (some overshoot on the way). Feed the result into a Lerp:
    //     door.position = Vector3.Lerp(closed, open, Easing.OutCubic(elapsed / duration));
    // "In" curves start slow and speed up, "Out" curves start fast and slow down, "InOut" do both.
    //
    // Why they're useful: linear motion looks robotic. Easing is the cheapest polish there is:
    // UI panels sliding in (OutCubic), buttons that pop (OutBack), doors and lifts (InOutCubic),
    // things landing (OutBounce), a camera settling, damage numbers rising, fades.
    // See easings.net for a picture of each.
    public static class Easing
    {
        // How far InBack pulls back and OutBack overshoots (Robert Penner's 10%).
        private const float Overshoot = 1.70158f;

        public static float Linear(float t) => t;

        // Gentle at both ends: 3t² - 2t³ (Unity's Mathf.SmoothStep uses the same curve).
        public static float SmoothStep(float t) => t * t * (3f - 2f * t);

        // Even gentler at both ends, with no jolt in speed: 6t⁵ - 15t⁴ + 10t³ (NoiseMath.Fade).
        public static float SmootherStep(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        public static float InQuad(float t)  => t * t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);

        public static float InCubic(float t)  => t * t * t;
        public static float OutCubic(float t) => 1f - Cube(1f - t);
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - 4f * Cube(1f - t);

        // Pulls back a little before setting off: a wind-up.
        public static float InBack(float t) => (Overshoot + 1f) * t * t * t - Overshoot * t * t;

        // Shoots a little past the end and settles back: a pop.
        public static float OutBack(float t) => 1f - InBack(1f - t);

        // Lands and bounces a few times, each smaller (Robert Penner's bounce).
        public static float OutBounce(float t)
        {
            const float Strength = 7.5625f, Width = 2.75f;
            if (t < 1f / Width) return Strength * t * t;
            if (t < 2f / Width) { t -= 1.5f / Width;  return Strength * t * t + 0.75f; }
            if (t < 2.5f / Width) { t -= 2.25f / Width; return Strength * t * t + 0.9375f; }
            t -= 2.625f / Width;
            return Strength * t * t + 0.984375f;
        }

        // Springs past the end and wobbles to rest, like a struck spring.
        public static float OutElastic(float t)
        {
            if (t <= 0f || t >= 1f) return Mathf.Clamp01(t);
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        private static float Cube(float value) => value * value * value;
    }
}
