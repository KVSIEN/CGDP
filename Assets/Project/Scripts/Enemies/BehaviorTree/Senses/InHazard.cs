using System;
using CGD.BehaviorTrees;
using CGD.Combat;

namespace CGD.Enemies
{
    // Standing in ground that hurts it right now (a hostile rift, say). Its own side's zones don't count.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/In Hazard", "Succeeds while it stands in a hostile damaging zone.")]
    public sealed class InHazard : EnemyCondition
    {
        protected override bool Check(BtContext context) =>
            Hazards.DamagePerSecondAt(Ai.transform.position, Ai.Health) > 0f;
    }
}
