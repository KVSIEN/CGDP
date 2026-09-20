namespace CGD.Enemies
{
    // Walks the waypoint loop (or idles without waypoints) until a hostile is detected
    // or a lead (noise, hit) comes in.
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
                Ai.Agent.SetDestination(Ai.Waypoints[_waypointIndex].position);
        }

        public override void Tick(float deltaTime)
        {
            if (Ai.Perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Chase);
                return;
            }

            if (Ai.Perception.ConsumeLead())
            {
                Ai.ChangeState(EnemyAI.AiState.Alert);
                return;
            }

            Ai.SetSpeed(Ai.Data.PatrolSpeed);
            if (Ai.Waypoints.Length == 0) return;

            var agent = Ai.Agent;
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance < ArrivalDistance)
            {
                _waypointIndex = (_waypointIndex + 1) % Ai.Waypoints.Length;
                agent.SetDestination(Ai.Waypoints[_waypointIndex].position);
            }
        }
    }
}
