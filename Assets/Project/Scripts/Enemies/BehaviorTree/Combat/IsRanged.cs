using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds when its data says it fights at range (EnemyData › Combat Type Ranged). Lets one
    // shared behavior shoot with ranged enemy types and close in with melee ones.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Is Ranged", "Succeeds when its Combat Type is Ranged.")]
    public sealed class IsRanged : EnemyCondition
    {
        protected override bool Check(BtContext context) => Ai.Data.CombatType == EnemyCombatType.Ranged;
    }
}
