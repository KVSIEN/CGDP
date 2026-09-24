using UnityEngine;
using CGD.Core;

namespace CGD.Items
{
    // Tier <-> quality mapping. Quality is always 1-100 and each tier owns an equal
    // band of it (Common 1-20 ... Legendary 81-100), matching the GDD's table.
    public static class ItemTiers
    {
        public const int MinQuality = 1;
        public const int MaxQuality = 100;

        public const int TierCount = 5;
        private const int BandSize = (MaxQuality - MinQuality + 1) / TierCount;

        public static IntRange QualityRange(ItemTier tier)
        {
            int max = ((int)tier + 1) * BandSize;
            return new IntRange(max - BandSize + 1, max);
        }

        public static int RollQuality(ItemTier tier, RandomStream random) => QualityRange(tier).Evaluate(random);

        public static ItemTier FromQuality(int quality) =>
            (ItemTier)((Clamp(quality) - 1) / BandSize);

        // Position of a quality score on the full 1-100 scale, as 0..1.
        public static float Normalize(int quality) =>
            Mathf.InverseLerp(MinQuality, MaxQuality, Clamp(quality));

        private static int Clamp(int quality) => Mathf.Clamp(quality, MinQuality, MaxQuality);
    }
}
