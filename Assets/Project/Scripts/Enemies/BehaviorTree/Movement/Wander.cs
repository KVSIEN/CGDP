using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Walks to a random reachable spot near its post (or itself), then succeeds — wrap it in
    // a Repeat with a Wait for an idle roam that never takes the same route twice.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Wander", "Walks to a random reachable spot near its post (or itself), then succeeds.")]
    public sealed class Wander : EnemyAction
    {
        private const int Attempts = 4;

        [SerializeField] private WanderAround _around = WanderAround.Post;
        [SerializeField, Min(1f)] private float _radius = 6f;
        [SerializeField] private bool _run;

        [NonSerialized] private bool _moving;

        public override string Summary => $"{_radius:0.#} m around {_around}";

        protected override void OnEnter(BtContext context)
        {
            _moving = false;
            Vector3 center = _around == WanderAround.Post ? Ai.Post.Position : Ai.transform.position;
            for (int i = 0; i < Attempts && !_moving; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * _radius;
                Vector3 spot   = center + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(spot, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                    _moving = Ai.Routing.SetDestination(hit.position);
            }
            if (_moving) Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Patrol);
            Ai.SetSpeed(_run ? Ai.Data.ChaseSpeed : Ai.Data.PatrolSpeed);
            return HasArrived(Ai.Agent, 0.6f) ? BtStatus.Success : BtStatus.Running;
        }
    }
}
