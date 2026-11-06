using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Shot at in the last moment or two: hit, a hostile bullet landing close, or a target it
    // can see aiming squarely at it.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Under Fire", "Succeeds for a moment after it's hit, a bullet lands close, or it sees a target aim at it.")]
    public sealed class UnderFire : EnemyCondition
    {
        protected override bool Check(BtContext context) => Ai.Perception.IsUnderFire;
    }
}
