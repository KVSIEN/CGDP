using System.Collections.Generic;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Artifacts
{
    // A carried artifact. Its passive stats are its rolled BaseStats (and passive perks), applied
    // to the wielder while it is held; its active use, if it has one, keeps its state here.
    public class ArtifactInstance : ItemInstance, IOffhand
    {
        private readonly ArtifactDefinition _definition;

        // Null for a passive-only artifact.
        public OffhandUse Use { get; }
        // How well it rolled (1 = average): scales its behavior.
        public float Potency { get; }

        public string Usage => _definition.Behavior != null ? _definition.Behavior.Usage : null;

        public MeleeWeaponData OffhandData => null;
        public bool IsShield        => false;
        public bool BlocksPassively => false;
        public bool TakesAim        => Use != null && Use.Mode != OffhandUseMode.Passive;
        public IReadOnlyList<StatModifier> MainHandPenalty => _definition.MainHandPenalty;

        internal ArtifactInstance(ArtifactDefinition definition, ItemRoll roll) : base(definition, roll)
        {
            _definition = definition;
            Use         = definition.Behavior != null ? definition.Behavior.CreateUse() : null;
            Potency     = roll.Sample(ItemStat.Damage, definition.Potency);
        }
    }
}
