namespace CGD.Enemies
{
    // Entered from any state while Stunnable says so. Holds the agent still, then
    // returns to whatever the enemy was doing when the stun landed.
    public class StunnedState : EnemyState
    {
        private EnemyAI.AiState _resumeTo;

        public StunnedState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Stunned;

        public override void Enter()
        {
            _resumeTo = Ai.PreviousState;
            Ai.Agent.isStopped = true;
        }

        public override void Tick(float deltaTime)
        {
            if (!Ai.IsStunned) Ai.ChangeState(_resumeTo);
        }

        public override void Exit() => Ai.Agent.isStopped = false;
    }
}
