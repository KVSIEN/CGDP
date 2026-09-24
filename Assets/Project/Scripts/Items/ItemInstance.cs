using System.Collections.Generic;
using CGD.Core;
using CGD.Stats;

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
        private readonly ModifierSet<ItemStat>      _modifiers   = new();

        private bool _modifiersDirty = true;

        public ItemDefinition Definition { get; }
        public int            Quality    { get; }
        public ItemTier       Tier       { get; }
        // What the roll was made from — the gear definition's Roll(Tier, Seed) recreates it.
        // Default for hand-authored gear that was never rolled.
        public Seed           Seed       { get; }

        // Overridable so subclasses like WeaponInstance can present a rolled name
        // (e.g. the individual gun's WeaponName) instead of the category label.
        public virtual string DisplayName => Definition != null ? Definition.DisplayName : "Unknown";
        public virtual float  Weight      => Definition != null ? Definition.Weight       : 0f;

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
            Seed            = roll.Seed;
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

        // Applies fitted attachments to a base value with the shared modifier formula
        // (StatModifierOp): flat deltas first, then percentages, so two attachments giving
        // +10% combine to +20% rather than compounding order-dependently.
        public float Modify(ItemStat stat, float baseValue)
        {
            if (_modifiersDirty) RebuildModifiers();

            return _modifiers.Apply(stat, baseValue);
        }

        // Final value of a stat this instance rolled itself, attachments included.
        public float GetStat(ItemStat stat) => Modify(stat, BaseStats[stat]);

        private void RebuildModifiers()
        {
            _modifiers.Clear();

            foreach (AttachmentDefinition attachment in _attachments)
            {
                if (attachment.Modifiers == null) continue;

                foreach (StatModifier modifier in attachment.Modifiers)
                    if (modifier.Stat != ItemStat.None)
                        _modifiers.Add(modifier.Stat, new Modifier(modifier.Op, modifier.Value, attachment));
            }

            _modifiersDirty = false;
        }

        public override string ToString() =>
            Definition != null ? $"{Definition.DisplayName} ({Tier}, Q{Quality})" : "Empty";
    }
}
