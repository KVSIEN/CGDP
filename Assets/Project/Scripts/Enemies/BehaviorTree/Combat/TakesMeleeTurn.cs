using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while it has — or can take — its turn to melee the target (only a few allies
    // melee one target at once; see EnemyData.MaxMeleeAttackers). Taking the turn is part of
    // asking, so put it right before Melee Attack: [Target In Range → Takes Melee Turn → Melee Attack].
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Takes Melee Turn", "Succeeds while it has (or can take) its turn to melee the target.")]
    public sealed class TakesMeleeTurn : EnemyCondition
    {
        protected override bool Check(BtContext context)
        {
            var target = Ai.Perception.Target;
            return target != null && Ai.Tactics.TryTakeMeleeTurn(target);
        }
    }
}
