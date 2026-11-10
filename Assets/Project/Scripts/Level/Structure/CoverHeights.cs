using UnityEngine;

namespace CGD.Level
{
    // The two heights of cover a fight can read at a glance (the AI test arena's rule): low
    // cover you crouch behind and shoot over, and high cover that hides someone standing.
    // Anything in between is neither — too tall to shoot over, too short to hide behind — so
    // rolled heights of broken columns, wall stubs and counters are pulled into one band or the
    // other, and the geometry colours low pieces apart from full-height ones.
    //
    // The bands sit inside what enemies count as cover (CoverMap: low from 0.8 m, full from
    // 1.6 m), so every low piece is low cover to them too.
    public static class CoverHeights
    {
        public const float LowMin  = 0.9f;
        public const float LowMax  = 1.2f;
        public const float HighMin = 2.2f;

        // Heights below this become low cover, the rest high.
        public const float Split = (LowMax + HighMin) * 0.5f;

        // 0 (full height) stays 0; anything else lands in the nearer band, keeping its
        // variation inside it.
        public static float Readable(float height)
        {
            if (height <= 0f) return 0f;
            return height < Split ? Mathf.Clamp(height, LowMin, LowMax) : Mathf.Max(height, HighMin);
        }

        // Whether a piece of this height (0 = full height) is low cover.
        public static bool IsLow(float height) => height > 0f && height < Split;
    }
}
