using UnityEngine;

namespace CGD.Items
{
    // Base for items that roll: weapons and armor. Each instance is unique, carries
    // its own quality score, and can host attachments.
    //
    // StatRanges declares which stats this gear can roll and the window for each.
    // Weapons leave it empty — their stats live as named fields on WeaponData because
    // the firing code reads them directly — and drive those fields through the same
    // ItemRoll instead. Both paths share one quality curve.
    public abstract class GearDefinition : ItemDefinition
    {
        [Header("Rolling")]
        [Tooltip("Quality curve and tradeoff axes. Without one, stats roll uniformly and quality is ignored.")]
        [SerializeField] private StatRollProfile _rollProfile;

        [Tooltip("Stats this gear rolls, and the range each rolls within")]
        [SerializeField] private StatRange[] _statRanges;

        public StatRollProfile RollProfile => _rollProfile;
        public StatRange[]     StatRanges  => _statRanges;

        public ItemRoll Roll() =>
            _rollProfile != null ? _rollProfile.Roll(Tier) : ItemRoll.Unrolled();

        public int AttachmentSlots(ItemTier tier) =>
            _rollProfile != null ? _rollProfile.AttachmentSlots(tier) : 0;

        // Rolls this definition into a carryable instance. Weapon categories override
        // to produce a WeaponInstance carrying generated WeaponData.
        public virtual ItemInstance CreateInstance() => CreateInstance(Roll());

        public virtual ItemInstance CreateInstance(ItemRoll roll) => new(this, roll);

        // Fills a StatBlock from StatRanges using the given roll. Shared by every
        // gear type that stores its stats generically.
        public void RollStats(ItemRoll roll, StatBlock into)
        {
            if (_statRanges == null) return;

            foreach (StatRange entry in _statRanges)
            {
                if (entry.Stat == ItemStat.None) continue;
                into[entry.Stat] = roll.Sample(entry.Stat, entry.Range);
            }
        }
    }
}
