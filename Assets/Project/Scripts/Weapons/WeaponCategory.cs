using CGD.Core;
using UnityEngine;
using CGD.Items;

namespace CGD.Weapons
{
    // A weapon type that rolls into individual weapons: a firearm category
    // (WeaponCategoryData) or a melee one (MeleeCategoryData). Shops, starter kits, random
    // pickups and the dev console take either, so melee weapons turn up wherever guns do.
    public abstract class WeaponCategory : GearDefinition
    {
        [Tooltip("Held in one hand: leaves the offhand free for a shield or an offhand weapon, and (melee) can itself be held in the offhand")]
        [SerializeField] private bool _oneHanded;

        public bool OneHanded => _oneHanded;

        public abstract WeaponItem Generate(ItemRoll roll);

        public WeaponItem Generate(ItemTier tier, Seed seed) => Generate(Roll(tier, seed));

        public sealed override ItemInstance CreateInstance(ItemRoll roll) => Generate(roll);
    }
}
