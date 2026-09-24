using System;
using CGD.Stats;

namespace CGD.Items
{
    // One authored stat change — on an attachment, or in a StatModifierPreset. Value may
    // be negative, which is how an attachment carries a drawback alongside its benefit.
    // At runtime it becomes a Modifier in a ModifierSet (see CGD.Stats).
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
