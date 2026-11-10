using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;
using CGD.Combat;

namespace CGD.Enemies
{
    // Runs to the nearest walkable spot just past the edge of the hostile zones it stands in,
    // keeping its eyes on its target on the way. Succeeds once it is out; fails when there is
    // no way out it can walk to, or it isn't out in time.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Leave Hazard", "Runs to the nearest spot clear of the hostile zones it stands in. Succeeds once out.")]
    public sealed class LeaveHazard : EnemyAction
    {
        private const float SnapDistance = 1.5f;

        [Tooltip("Give up if not out after this many seconds")]
        [SerializeField, Min(0.5f)] private float _giveUpAfter = 3f;

        [NonSerialized] private bool  _moving;
        [NonSerialized] private float _givesUpAt;
        [NonSerialized] private Func<Vector3, bool> _walkable;

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _walkable = IsWalkable;
        }

        protected override void OnEnter(BtContext context)
        {
            _givesUpAt = context.Time + _giveUpAfter;
            _moving = Hazards.TryFindWayOut(Ai.transform.position, Ai.Health, _walkable, out Vector3 exit) &&
                      NavMesh.SamplePosition(exit, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas) &&
                      Ai.Agent.SetDestination(hit.position);
            if (_moving) Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!IsInHazard()) return BtStatus.Success;
            if (!_moving || context.Time >= _givesUpAt) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.LeaveHazard);
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            Ai.KeepEyesOnTarget(context.DeltaTime);
            return BtStatus.Running;
        }

        private bool IsInHazard() => Hazards.DamagePerSecondAt(Ai.transform.position, Ai.Health) > 0f;

        private bool IsWalkable(Vector3 spot) =>
            NavMesh.SamplePosition(spot, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas) &&
            Hazards.DamagePerSecondAt(hit.position, Ai.Health) <= 0f;
    }
}
