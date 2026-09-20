namespace CGD.Enemies
{
    // One behavior of EnemyAI. States decide transitions themselves via Ai.ChangeState.
    public abstract class EnemyState
    {
        protected readonly EnemyAI Ai;

        protected EnemyState(EnemyAI ai) => Ai = ai;

        public abstract EnemyAI.AiState Id { get; }

        public virtual void Enter() { }
        public abstract void Tick(float deltaTime);
        public virtual void Exit() { }
    }
}
