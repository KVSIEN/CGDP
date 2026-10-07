using System.Collections.Generic;
using CGD.Items;

namespace CGD.Weapons
{
    // A carried shield. Its guard and bash live on generated MeleeWeaponData, so the
    // MeleeController blocks and bashes with it like with a melee weapon's guard.
    public class ShieldInstance : ItemInstance, IOffhand
    {
        private readonly ShieldDefinition _definition;

        public MeleeWeaponData Data { get; }

        public MeleeWeaponData OffhandData => Data;
        public bool IsShield        => true;
        public bool BlocksPassively => _definition.BlocksPassively;
        public IReadOnlyList<StatModifier> MainHandPenalty => _definition.MainHandPenalty;

        public override string DisplayName => Data != null ? Data.WeaponName : "Shield";

        internal ShieldInstance(ShieldDefinition definition, ItemRoll roll, MeleeWeaponData data) : base(definition, roll)
        {
            _definition = definition;
            Data        = data;
        }
    }
}
