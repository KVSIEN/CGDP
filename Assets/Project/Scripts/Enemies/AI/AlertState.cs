namespace CGD.Enemies
{
    // Looks into a lead — something seen, heard or felt — with EnemySearch: stops to stare,
    // goes to where it believes the thing is and searches around it. Spotting a hostile on the
    // way starts the chase; a search that turns up nothing calms it down back into patrol.
    public class AlertState : EnemyState
    {
        public AlertState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Alert;

        public override void Enter() => Ai.Search.Begin();

        public override void Tick(float deltaTime)
        {
            if (Ai.Perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Chase);
                return;
            }

            if (!Ai.Search.Tick(deltaTime)) return;

            Ai.Perception.LoseTarget();
            Ai.ChangeState(EnemyAI.AiState.Patrol);
        }

        public override void Exit() => Ai.Search.Cancel();
    }
}
