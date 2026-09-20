using System.Collections.Generic;

namespace CGD.Items
{
    // One unique, carryable piece of gear: what it is, how well it rolled, and what
    // is currently fitted to it.
    //
    // Attachment deltas are resolved on top of BaseStats rather than baked into
    // them, so fitting stays reversible and the original roll is never lost. The
    // resolved totals are cached and only rebuilt when the attachment set changes.
    public class ItemInstance
    {
        private readonly List<AttachmentDefinition> _attachments = new();
        private readonly StatBlock _additive       = new();
        private readonly StatBlock _multiplicative = new();

        private bool _modifiersDirty = true;

        public ItemDefinition Definition { get; }
        public int            Quality    { get; }
        public ItemTier       Tier       { get; }

        // The item's own rolled stats. Weapons leave this empty and keep their stats
        // on generated WeaponData instead; see WeaponInstance.
        public StatBlock BaseStats { get; } = new();

        public IReadOnlyList<AttachmentDefinition> Attachments => _attachments;
        public int AttachmentSlots { get; }
        public bool HasFreeSlot => _attachments.Count < AttachmentSlots;

        public ItemInstance(GearDefinition definition, ItemRoll roll)
        {
            Definition      = definition;
            Quality         = roll.Quality;
            Tier            = roll.Tier;
            AttachmentSlots = definition.AttachmentSlots(roll.Tier);

            definition.RollStats(roll, BaseStats);
        }

        // For gear built outside the roll pipeline, such as a hand-authored weapon
        // placed in a scene. It has no quality behind it and hosts no attachments.
        protected ItemInstance(ItemDefinition definition)
        {
            Definition = definition;
            Quality    = ItemTiers.MinQuality;
            Tier       = definition != null ? definition.Tier : ItemTier.Common;
        }

        public bool TryAttach(AttachmentDefinition attachment)
        {
            if (attachment == null || !HasFreeSlot) return false;

            _attachments.Add(attachment);
            _modifiersDirty = true;
            return true;
        }

        public bool Detach(AttachmentDefinition attachment)
        {
            if (!_attachments.Remove(attachment)) return false;

            _modifiersDirty = true;
            return true;
        }

        // Applies fitted attachments to a base value: flat deltas first, then
        // percentages, so two attachments giving +10% combine to +20% rather than
        // compounding order-dependently.
        public float Modify(ItemStat stat, float baseValue)
        {
            if (_modifiersDirty) RebuildModifiers();

            return (baseValue + _additive[stat]) * (1f + _multiplicative[stat]);
        }

        // Final value of a stat this instance rolled itself, attachments included.
        public float GetStat(ItemStat stat) => Modify(stat, BaseStats[stat]);

        private void RebuildModifiers()
        {
            _additive.Clear();
            _multiplicative.Clear();

            foreach (AttachmentDefinition attachment in _attachments)
            {
                if (attachment.Modifiers == null) continue;

                foreach (StatModifier modifier in attachment.Modifiers)
                {
                    if (modifier.Stat == ItemStat.None) continue;

                    if (modifier.Op == StatModifierOp.Additive)
                        _additive.Add(modifier.Stat, modifier.Value);
                    else
                        _multiplicative.Add(modifier.Stat, modifier.Value);
                }
            }

            _modifiersDirty = false;
        }

        public override string ToString() =>
            Definition != null ? $"{Definition.DisplayName} ({Tier}, Q{Quality})" : "Empty";
    }
}
