using System;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while the ability is off cooldown and usable by this enemy — the condition
    // to put in front of Use Ability so the branch only interrupts when it can cast. Like Use
    // Ability, it can check a slot of the enemy's kit instead of a named ability; an empty
    // slot is never ready.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Ability Ready", "Succeeds while the ability is off cooldown and usable by this enemy.")]
    public sealed class AbilityReady : EnemyCondition
    {
        [Tooltip("Own: the Ability below. Signature / Escape / Summon / Boost / Support: that slot of the enemy's kit (EnemyData › Kit)")]
        [SerializeField] private AbilitySlot _slot;
        [SerializeField] private Ability _ability;

        [NonSerialized] private Ability _resolved;

        public override string Summary => _slot != AbilitySlot.Own ? $"Kit: {_slot}" : _ability != null ? _ability.name : "(no ability)";

        internal AbilitySlot Slot => _slot;
        // The ability named on the node itself (used only with the Own slot).
        internal Ability Named => _ability;

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _resolved = _slot == AbilitySlot.Own ? _ability : Ai.Data.KitAbility(_slot);
            Ai.Abilities.Register(_resolved);
        }

        protected override bool Check(BtContext context) => Ai.Abilities.IsReady(_resolved);
    }
}
