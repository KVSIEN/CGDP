using System;
using CGD.Core;

namespace CGD.Items
{
    // Pairs a stat with the range it rolls in. Gear definitions list these to
    // declare which stats they can roll and how wide each one's window is.
    [Serializable]
    public struct StatRange
    {
        public ItemStat   Stat;
        public FloatRange Range;

        public StatRange(ItemStat stat, FloatRange range)
        {
            Stat  = stat;
            Range = range;
        }
    }
}
