using UnityEngine;

namespace CGD.Enemies
{
    // Closes in on the detected target and attacks at range; drops to Alert when the target
    // is no longer detected (unless an attack is already winding up). Now and then, when the
    // target is still some way off, it circles round by the route the target is least likely
    // to see instead of charging straight in (EnemyTactics).
    public class ChaseState : EnemyState
    {
        private const float FlankMinDistance = 7f;
        private const float FlankChance      = 0.4f;
        private const float ArrivalDistance  = 0.8f;
        private static readonly Vector2 FlankCheckEvery = new(5f, 9f);

        private float _nextFlankCheck;
        private bool  _flanking;

        public ChaseState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Chase;

        public override void Enter()
        {
            _flanking       = false;
            _nextFlankCheck = Time.time + Random.Range(0f, FlankCheckEvery.x);
        }

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            var agent      = Ai.Agent;

            if (Ai.IsAttacking)
            {
                if (perception.Target != null) Ai.FaceTowards(perception.TargetPosition, deltaTime);
                return;
            }

            if (!perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Alert);
                return;
            }

            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            var   targetPos = perception.TargetPosition;
            float range     = Ai.Data.AttackRange;
            float distance  = Vector3.Distance(Ai.transform.position, targetPos);

            if (distance <= range)
            {
                _flanking = false;
                agent.isStopped = true;
                Ai.FaceTowards(targetPos, deltaTime);
                Ai.TryStartAttack();
                return;
            }

            agent.isStopped = false;
            if (TickFlank(targetPos, distance)) return;
            agent.SetDestination(targetPos);
        }

        public override void Exit() => Ai.Agent.isStopped = false;

        // True while it is circling round rather than heading straight for the target.
        private bool TickFlank(Vector3 targetPos, float distance)
        {
            if (_flanking)
            {
                var agent = Ai.Agent;
                bool arrived = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= ArrivalDistance);
                if (arrived || distance < Ai.Data.AttackRange * 2f) _flanking = false;
                return _flanking;
            }

            if (Time.time < _nextFlankCheck || distance < FlankMinDistance) return false;
            _nextFlankCheck = Time.time + Random.Range(FlankCheckEvery.x, FlankCheckEvery.y);

            if (Random.value >= FlankChance) return false;
            if (!Ai.Tactics.TryPlanFlank(targetPos, Ai.Data.AttackRange + 2f, out Vector3 destination)) return false;

            _flanking = Ai.Agent.SetDestination(destination);
            return _flanking;
        }
    }
}
