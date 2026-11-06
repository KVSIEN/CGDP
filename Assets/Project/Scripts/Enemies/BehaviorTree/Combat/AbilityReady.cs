using System;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while the ability is off cooldown and usable by this enemy — the condition
    // to put in front of Use Ability so the branch only interrupts when it can cast.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Ability Ready", "Succeeds while the ability is off cooldown and usable by this enemy.")]
    public sealed class AbilityReady : EnemyCondition
    {
        [SerializeField] private Ability _ability;

        public override string Summary => _ability != null ? _ability.name : "(no ability)";

        protected override bool Check(BtContext context) => Ai.Abilities.IsReady(_ability);
    }
}
