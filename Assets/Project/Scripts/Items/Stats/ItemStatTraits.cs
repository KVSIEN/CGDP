using System;

namespace CGD.Items
{
    // Per-stat facts the roller needs but that don't belong on the enum itself.
    //
    // LowerIsBetter is the one that makes authoring work: ranges are written as the
    // raw quantity they measure (reload 2.2-3.0 seconds, recoil 0.9-1.5), so for
    // those stats a *good* roll has to land near Min rather than Max. The roller
    // deals in desirability and inverts here, which keeps every category asset
    // readable as plain numbers.
    public static class ItemStatTraits
    {
        public static readonly int Count = Enum.GetValues(typeof(ItemStat)).Length;

        public static bool LowerIsBetter(ItemStat stat) => stat switch
        {
            ItemStat.ReloadTime => true,
            ItemStat.Recoil     => true,
            ItemStat.Spread     => true,
            ItemStat.DrawTime   => true,
            ItemStat.Sway       => true,
            _                   => false,
        };

        // Converts a 0..1 desirability into a 0..1 position inside the stat's range.
        public static float ToRangePosition(ItemStat stat, float desirability) =>
            LowerIsBetter(stat) ? 1f - desirability : desirability;
    }
}
