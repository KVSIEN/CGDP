using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Perks;
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

        [Tooltip("Perks this gear rolls from, on top of its stats. Empty = no perks")]
        [SerializeField] private PerkPool _perkPool;
        [Tooltip("Perks every item of this gear has, on top of the ones it rolls (e.g. the Modular Pistol's Tactical Knife)")]
        [SerializeField] private GearPerk[] _guaranteedPerks = System.Array.Empty<GearPerk>();

        public StatRollProfile RollProfile => _rollProfile;
        public StatRange[]     StatRanges  => _statRanges;
        public PerkPool        PerkPool    => _perkPool;
        public IReadOnlyList<GearPerk> GuaranteedPerks => _guaranteedPerks;

        // The seed layers an item of this definition rolls from, each rerollable on its own
        // (RerollLayer). Generators that draw from a layer of their own list it too.
        public virtual IReadOnlyList<string> SeedLayers => GearLayers;

        private static readonly string[] GearLayers = { ItemRoll.QualityLayer, ItemRoll.StatsLayer, PerkPool.PerksLayer };

        public ItemRoll Roll() => Roll(Tier, Seed.Random());

        // Rolls at a specific tier instead of the definition's own — how loot tables
        // express rarity (the same rifle can drop Common or Legendary). The same tier and
        // seed always give the same roll.
        public ItemRoll Roll(ItemTier tier, Seed seed) => Roll(tier, seed, null);

        // The same, with some layers rerolled (null = none): the same tier, seed and
        // variants always give the same roll.
        public ItemRoll Roll(ItemTier tier, Seed seed, SeedVariants variants) =>
            _rollProfile != null ? _rollProfile.Roll(tier, seed, variants) : ItemRoll.Unrolled(seed, variants);

        // The roll `item` was made from, with fresh numbers for one of SeedLayers only —
        // everything drawn from the other layers comes out exactly as it was. Build the new
        // item from it with CreateInstance (or WeaponCategory.Generate).
        public ItemRoll RerollLayer(ItemInstance item, string layer)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.Definition != this) throw new ArgumentException($"{item} isn't a {name}.", nameof(item));
            if (!SeedLayers.Contains(layer))
                throw new ArgumentException($"{name} has no seed layer '{layer}' (layers: {string.Join(", ", SeedLayers)}).", nameof(layer));

            SeedVariants variants = item.Variants;
            variants.Reroll(layer);
            return Roll(item.Tier, item.Seed, variants);
        }

        public int AttachmentSlots(ItemTier tier) =>
            _rollProfile != null ? _rollProfile.AttachmentSlots(tier) : 0;

        // Rolls this definition into a carryable instance. Weapon categories override
        // to produce a weapon carrying generated weapon data (WeaponInstance, MeleeWeaponInstance).
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
