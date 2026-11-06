using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Base for behavior tree conditions that ask about an EnemyAI. The tree must run on an enemy.
    [Serializable]
    public abstract class EnemyCondition : BtCondition
    {
        [NonSerialized] protected EnemyAI Ai;

        protected override void OnInitialize(BtContext context)
        {
            Ai = context.Agent as EnemyAI
                 ?? throw new InvalidOperationException($"{GetType().Name} needs an EnemyAI to ask about, not {context.Agent?.GetType().Name ?? "nothing"}.");
        }
    }
}
