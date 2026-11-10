using System;
using CGD.BehaviorTrees;
using UnityEngine;

namespace CGD.Enemies
{
    // Shot at in the last moment or two: hit, a hostile bullet landing close, or a target it
    // can see aiming squarely at it. Warnings Only leaves out being hit, so a dodge under it
    // reads the shot coming instead of flinching after it lands.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Under Fire", "Succeeds for a moment after it's hit, a bullet lands close, or it sees a target aim at it.")]
    public sealed class UnderFire : EnemyCondition
    {
        [Tooltip("Only a bullet landing close or a target aiming at it count, not being hit")]
        [SerializeField] private bool _warningsOnly;

        public override string Summary => _warningsOnly ? "warnings only" : base.Summary;

        protected override bool Check(BtContext context) =>
            _warningsOnly ? Ai.Perception.IsFireIncoming : Ai.Perception.IsUnderFire;
    }
}
