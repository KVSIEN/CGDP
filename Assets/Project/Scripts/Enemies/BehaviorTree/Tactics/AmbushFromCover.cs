using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Bio's way of lying in wait: still in its bush, eyes on its target, it lets the target
    // come. Succeeds the moment the target is within spring range, or — a little further out —
    // turns its back or reloads: put the strike after it (Ambush, then Charge and Melee Attack).
    // Fails when it is given away or its patience runs out.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Ambush From Cover", "Lies hidden, watching, until the target walks close or turns its back: then succeeds (spring!). Fails when given away or out of patience.")]
    public sealed class AmbushFromCover : HiddenWait
    {
        [Tooltip("Metres: a target this close is sprung on at once")]
        [SerializeField, Min(0f)] private float _springRange = 5f;
        [Tooltip("Metres: a target this close that turns its back or reloads is sprung on too")]
        [SerializeField, Min(0f)] private float _openingRange = 12f;

        protected override BtStatus Watch(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (perception.Target == null) return PatienceSpent(context) ? BtStatus.Failure : BtStatus.Running;

            Ai.Motion.KeepEyesOnTarget(context.DeltaTime);
            float sqrDistance = (perception.TargetPosition - Ai.transform.position).sqrMagnitude;
            if (sqrDistance <= _springRange * _springRange) return BtStatus.Success;
            if (Ai.Readings.IsExposed && sqrDistance <= _openingRange * _openingRange) return BtStatus.Success;

            return PatienceSpent(context) ? BtStatus.Failure : BtStatus.Running;
        }
    }
}
