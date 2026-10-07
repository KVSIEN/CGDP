using CGD.Items;

namespace CGD.Weapons
{
    // Anything that goes in a weapon slot: a firearm (WeaponInstance) or a melee weapon
    // (MeleeWeaponInstance). The loadout, weapon wheel, pickups and shops work with this,
    // and each kind keeps its own stats and its own controller.
    public abstract class WeaponItem : ItemInstance
    {
        protected WeaponItem(GearDefinition definition, ItemRoll roll) : base(definition, roll) { }

        // Hand-authored weapon placed directly in a scene: no roll, no quality, no attachment slots.
        protected WeaponItem(ItemDefinition definition) : base(definition) { }

        // Restores whatever the weapon spends (a firearm's magazine) on revive.
        public virtual void Refill() { }
    }
}
