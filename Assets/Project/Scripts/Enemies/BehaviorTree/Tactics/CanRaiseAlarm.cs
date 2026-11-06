using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds when this enemy raises alarms (EnemyData), hasn't tried this fight, and knows an
    // armed panel near its post. The gate in front of Raise Alarm.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Can Raise Alarm", "Succeeds when it raises alarms, hasn't tried yet this fight and knows an armed panel near its post.")]
    public sealed class CanRaiseAlarm : EnemyCondition
    {
        protected override bool Check(BtContext context) => Ai.TryFindAlarm(out _);
    }
}
