using System;
using System.Collections.Generic;
using CGD.Items;

namespace CGD.Weapons
{
    // A carried melee weapon. Its stats live on generated MeleeWeaponData, the way a
    // firearm's live on WeaponData; it spends nothing, so there is nothing to refill.
    public class MeleeWeaponInstance : WeaponItem, IOffhand
    {
        public MeleeWeaponData Data { get; }

        // Lives on the weapon, not the controller, so switching away and back mid-combo
        // continues the string (within the weapon's weave window).
        public ComboState Combo { get; } = new();

        public MeleeWeaponData OffhandData => Data;
        public bool IsShield        => false;
        public bool BlocksPassively => false;
        public bool TakesAim        => false;
        public Artifacts.OffhandUse Use => null;
        public IReadOnlyList<StatModifier> MainHandPenalty =>
            Definition is MeleeCategoryData category && category.OffhandPenalty != null
                ? category.OffhandPenalty
                : Array.Empty<StatModifier>();

        public override string DisplayName => Data != null ? Data.WeaponName : "Melee Weapon";

        public MeleeWeaponInstance(MeleeWeaponData data) : base((ItemDefinition)null) => Data = data;

        internal MeleeWeaponInstance(MeleeCategoryData category, ItemRoll roll, MeleeWeaponData data)
            : base(category, roll) => Data = data;
    }
}
