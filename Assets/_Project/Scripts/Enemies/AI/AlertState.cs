namespace CGD.Enemies
{
    // Investigates the last known position (lost target, noise, hit) for AlertDuration,
    // then gives up and returns to patrol.
    public class AlertState : EnemyState
    {
        private const float ArrivalDistance = 0.5f;

        private float _timer;

        public AlertState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Alert;

        public override void Enter() => Investigate();

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;

            if (perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Chase);
                return;
            }

            if (perception.ConsumeLead()) Investigate();

            Ai.SetSpeed(Ai.Data.PatrolSpeed);
            _timer -= deltaTime;

            var agent = Ai.Agent;
            bool arrived = !agent.pathPending && agent.hasPath && agent.remainingDistance < ArrivalDistance;
            if (_timer > 0f && !arrived) return;

            perception.LoseTarget();
            Ai.ChangeState(EnemyAI.AiState.Patrol);
        }

        private void Investigate()
        {
            _timer = Ai.Data.AlertDuration;
            Ai.Agent.isStopped = false;
            Ai.Agent.SetDestination(Ai.Perception.LastKnownPosition);
        }
    }
}
