using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A quick sidestep across the line of fire — whichever side is open, chosen at random
    // when both are. Succeeds once there (or after a moment); fails with nowhere to go.
    // Put Under Fire, a Cooldown and a Chance above it, or it dodges every time it's shot at.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Dodge", "Sidesteps across the line of fire, to whichever side is open. Fails with nowhere to go.")]
    public sealed class Dodge : EnemyAction
    {
        [SerializeField, Min(0.5f)] private float _distance = 3f;
        [Tooltip("Speed while dodging, as a multiple of chase speed")]
        [SerializeField, Min(1f)] private float _speed = 2.2f;
        [SerializeField, Min(0.1f)] private float _maxTime = 0.8f;

        [NonSerialized] private bool  _moving;
        [NonSerialized] private float _endsAt;
        [NonSerialized] private float _acceleration;

        public override string Summary => $"{_distance:0.#} m";

        protected override void OnEnter(BtContext context)
        {
            _endsAt = context.Time + _maxTime;
            _moving = TryPickSide(out Vector3 destination) && Ai.Agent.SetDestination(destination);
            if (!_moving) return;

            _acceleration = Ai.Agent.acceleration;
            Ai.Agent.acceleration = 60f;
            Ai.Agent.isStopped    = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.Dodge);
            Ai.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            return context.Time >= _endsAt || HasArrived(Ai.Agent, 0.4f) ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnExit(BtContext context)
        {
            if (_moving) Ai.Agent.acceleration = _acceleration;
        }

        private bool TryPickSide(out Vector3 destination)
        {
            destination = default;
            Vector3 threat = Ai.Perception.ThreatDirection;
            if (threat == Vector3.zero) threat = Ai.transform.forward;

            Vector3 across = Vector3.Cross(Vector3.up, threat).normalized;
            int first = UnityEngine.Random.value < 0.5f ? 1 : -1;
            for (int i = 0; i < 2; i++)
            {
                Vector3 from = Ai.transform.position;
                Vector3 to   = from + across * (_distance * (i == 0 ? first : -first));
                if (NavMesh.Raycast(from, to, out _, NavMesh.AllAreas)) continue;   // blocked that way
                if (!NavMesh.SamplePosition(to, out NavMeshHit hit, 1f, NavMesh.AllAreas)) continue;

                destination = hit.position;
                return true;
            }
            return false;
        }
    }
}
