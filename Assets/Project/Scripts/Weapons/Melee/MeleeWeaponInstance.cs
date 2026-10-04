using CGD.Items;

namespace CGD.Weapons
{
    // A carried melee weapon. Its stats live on generated MeleeWeaponData, the way a
    // firearm's live on WeaponData; it spends nothing, so there is nothing to refill.
    public class MeleeWeaponInstance : WeaponItem
    {
        public MeleeWeaponData Data { get; }

        // Lives on the weapon, not the controller, so switching away and back mid-combo
        // continues the string (within the weapon's weave window).
        public ComboState Combo { get; } = new();

        public override string DisplayName => Data != null ? Data.WeaponName : "Melee Weapon";

        public MeleeWeaponInstance(MeleeWeaponData data) : base((ItemDefinition)null) => Data = data;

        internal MeleeWeaponInstance(MeleeCategoryData category, ItemRoll roll, MeleeWeaponData data)
            : base(category, roll) => Data = data;
    }
}
