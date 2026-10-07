using CGD.Core;
using CGD.Items;

namespace CGD.Weapons
{
    // A weapon type that rolls into individual weapons: a firearm category
    // (WeaponCategoryData) or a melee one (MeleeCategoryData). Shops, starter kits, random
    // pickups and the dev console take either, so melee weapons turn up wherever guns do.
    public abstract class WeaponCategory : GearDefinition
    {
        public abstract WeaponItem Generate(ItemRoll roll);

        public WeaponItem Generate(ItemTier tier, Seed seed) => Generate(Roll(tier, seed));

        public sealed override ItemInstance CreateInstance(ItemRoll roll) => Generate(roll);
    }
}
