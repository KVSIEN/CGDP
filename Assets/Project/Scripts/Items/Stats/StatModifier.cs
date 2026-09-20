using System;

namespace CGD.Items
{
    // One stat change contributed by an attachment. Value may be negative, which is
    // how an attachment carries a drawback alongside its benefit.
    [Serializable]
    public struct StatModifier
    {
        public ItemStat       Stat;
        public StatModifierOp Op;
        public float          Value;

        public StatModifier(ItemStat stat, StatModifierOp op, float value)
        {
            Stat  = stat;
            Op    = op;
            Value = value;
        }
    }
}
