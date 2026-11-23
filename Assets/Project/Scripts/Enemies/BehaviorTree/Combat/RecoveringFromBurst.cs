using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // The decompress after a burst (EnemyData › Burst Recovery, or a ranged combo step's
    // Recovery): free to move but casting nothing. Guards a branch that spends that time
    // repositioning — the Tech Tank finds a better spot after every step of its combo.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Recovering From Burst", "Succeeds while it winds down after a burst (after each step of a ranged combo too).")]
    public sealed class RecoveringFromBurst : EnemyCondition
    {
        protected override bool Check(BtContext context) => Ai.RangedCombat.IsRecovering;
    }
}
