using UnityEngine;

namespace CGD.Enemies
{
    // Walks the waypoint loop until a hostile is detected or a lead comes in. Without a
    // route it guards its post instead, looking around now and then. On edge after a
    // fruitless search it walks faster and, guarding, looks around twice as often.
    public class PatrolState : EnemyState
    {
        private const float ArrivalDistance = 0.4f;

        private int _waypointIndex;

        public PatrolState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Patrol;

        public override void Enter()
        {
            Ai.Agent.isStopped = false;
            if (Ai.Waypoints.Length > 0)
                Ai.Agent.SetDestination(Ai.Waypoints[_waypointIndex % Ai.Waypoints.Length].position);
        }

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            if (perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Chase);
                return;
            }

            if (perception.ConsumeLead())
            {
                Ai.ChangeToSearch();
                return;
            }

            float speed = Ai.Data.PatrolSpeed;
            if (perception.IsCautious) speed *= perception.Profile.CautionPatrolSpeed;
            Ai.SetSpeed(speed);

            var waypoints = Ai.Waypoints;
            if (waypoints.Length == 0)
            {
                Ai.Post.TickGuard(deltaTime, Time.time);
                return;
            }

            var agent = Ai.Agent;
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance < ArrivalDistance)
            {
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[_waypointIndex].position);
            }
        }

        public override void Exit() => Ai.Agent.isStopped = false;
    }
}
