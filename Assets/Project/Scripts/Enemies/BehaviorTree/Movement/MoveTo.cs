using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Walks (or runs) to a position stored on the blackboard (see Remember Position).
    // Succeeds on arrival; fails if the key isn't set or the spot can't be reached.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Move To", "Walks or runs to a blackboard position. Fails if it's missing or unreachable.")]
    public sealed class MoveTo : EnemyAction
    {
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Vector)] private string _key = "Spot";
        [Tooltip("Chase speed instead of patrol speed")]
        [SerializeField] private bool _run;
        [SerializeField, Min(0.1f)] private float _arrivalDistance = 0.8f;

        [NonSerialized] private bool _moving;

        public override string Summary => (_run ? "Run to " : "Walk to ") + _key;

        protected override void OnEnter(BtContext context)
        {
            _moving = false;
            if (!context.Blackboard.Has(_key)) return;

            Vector3 goal = context.Blackboard.GetVector(_key);
            if (!NavMesh.SamplePosition(goal, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return;

            Ai.Agent.isStopped = false;
            _moving = Ai.Routing.SetDestination(hit.position);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving) return BtStatus.Failure;

            Ai.SetSpeed(_run ? Ai.Data.ChaseSpeed : Ai.Data.PatrolSpeed);
            var agent = Ai.Agent;
            if (agent.pathPending) return BtStatus.Running;
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return BtStatus.Failure;
            return HasArrived(agent, _arrivalDistance) || !agent.hasPath ? BtStatus.Success : BtStatus.Running;
        }
    }
}
