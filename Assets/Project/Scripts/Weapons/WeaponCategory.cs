using UnityEngine;
using CGD.Core;
using CGD.Items;

namespace CGD.Weapons
{
    // A weapon type that rolls into individual weapons: a firearm category
    // (WeaponCategoryData) or a melee one (MeleeCategoryData). Shops, starter kits, random
    // pickups and the dev console take either, so melee weapons turn up wherever guns do.
    public abstract class WeaponCategory : GearDefinition
    {
        [Header("Perks")]
        [Tooltip("Perks this category's weapons roll from. Empty = no perks")]
        [SerializeField] private WeaponPerkPool _perkPool;

        public WeaponItem Generate(ItemRoll roll)
        {
            WeaponItem weapon = Build(roll);
            if (_perkPool != null) weapon.SetPerks(_perkPool.Roll(weapon, roll));
            return weapon;
        }

        public WeaponItem Generate(ItemTier tier, Seed seed) => Generate(Roll(tier, seed));

        public sealed override ItemInstance CreateInstance(ItemRoll roll) => Generate(roll);

        // The weapon itself from the roll: its kind's stats; perks are added on top.
        protected abstract WeaponItem Build(ItemRoll roll);
    }
}
