using CGD.Core;

namespace CGD.Items
{
    // The outcome of rolling one item: its quality, and where each stat landed
    // expressed as a 0..1 desirability. Nothing here knows about concrete ranges,
    // so the same roll can drive a weapon's 50 authored fields and an armor piece's
    // handful of stats alike — each consumer samples its own ranges through it.
    //
    // Seed is what the roll was made from. Rolling the same definition at the same tier
    // with the same seed gives the same item, and consumers draw any extra randomness
    // (a weapon's name, its non-quality stats) from streams derived from it.
    public sealed class ItemRoll
    {
        private readonly float[] _desirability;

        public int      Quality { get; }
        public ItemTier Tier    { get; }
        public Seed     Seed    { get; }

        internal ItemRoll(int quality, float[] desirability, Seed seed)
        {
            Quality       = quality;
            Tier          = ItemTiers.FromQuality(quality);
            _desirability = desirability;
            Seed          = seed;
        }

        // A roll with no quality behind it: every stat lands at a uniformly random
        // position. Used for items generated without a roll profile, which reproduces
        // the pre-quality behaviour exactly.
        public static ItemRoll Unrolled(Seed seed)
        {
            RandomStream random = seed.Derive(StatsLayer).Stream();

            var desirability = new float[ItemStatTraits.Count];
            for (int i = 0; i < desirability.Length; i++)
                desirability[i] = random.Value;

            return new ItemRoll(ItemTiers.MinQuality, desirability, seed);
        }

        // Seed layers a roll is built from. Consumers add their own (see WeaponGenerator).
        internal const string QualityLayer = "quality";
        internal const string StatsLayer   = "stats";

        // How good this stat rolled, 0..1, before the range is consulted.
        public float Desirability(ItemStat stat) => _desirability[(int)stat];

        public float Sample(ItemStat stat, FloatRange range) =>
            range.Lerp(ItemStatTraits.ToRangePosition(stat, Desirability(stat)));

        public int Sample(ItemStat stat, IntRange range) =>
            range.Lerp(ItemStatTraits.ToRangePosition(stat, Desirability(stat)));
    }
}
