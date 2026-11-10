using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while enough of its kin (same team, or a pact) are within a radius: the herd is close.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Kin Near", "Succeeds while at least this many of its kin are within the radius.")]
    public sealed class KinNear : EnemyCondition
    {
        [Tooltip("Metres")]
        [SerializeField, Min(1f)] private float _radius = 20f;
        [SerializeField, Min(1)] private int _atLeast = 1;

        public override string Summary => $"{_atLeast} within {_radius:0.#} m";

        protected override bool Check(BtContext context) => EnemyRoster.KinWithin(Ai, _radius) >= _atLeast;
    }
}
