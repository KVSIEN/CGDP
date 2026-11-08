using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Its target was fighting someone else when it last saw it — another faction's enemy
    // engaged elsewhere, not watching this one. The player never counts as distracted.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Target Distracted", "Succeeds while its target was last seen fighting someone else (never the player).")]
    public sealed class TargetDistracted : EnemyCondition
    {
        protected override bool Check(BtContext context) =>
            Ai.Perception.Target != null && Ai.Perception.IsTargetDistracted;
    }
}
