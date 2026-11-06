namespace CGD.Enemies
{
    // Morale broke: it falls back — to high cover screened from the target if it knows some,
    // otherwise to its post — and stays there facing the threat while it recovers. Once it
    // has rallied it goes back to the fight (or a hunt, if it lost the target meanwhile).
    // Cornered, it fights.
    public class RetreatState : EnemyState
    {
        private const float GiveUpAfter      = 12f;
        private const float ArrivalDistance  = 0.8f;

        private float _elapsed;

        public RetreatState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Retreat;

        public override void Enter()
        {
            _elapsed = 0f;
            RetreatStep.Begin(Ai);
        }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            var perception = Ai.Perception;

            if (!Ai.Morale.IsShaken || Ai.IsCornered || _elapsed >= GiveUpAfter)
            {
                if (perception.IsTargetDetected) Ai.ChangeState(EnemyAI.AiState.Chase);
                else Ai.ChangeToSearch();
                return;
            }

            RetreatStep.Tick(Ai, deltaTime, ArrivalDistance);
        }

        public override void Exit()
        {
            Ai.Agent.isStopped = false;
            Ai.Tactics.ReleaseCover();
        }
    }
}
