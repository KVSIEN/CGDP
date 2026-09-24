using CGD.Core;

namespace CGD.Enemies
{
    // One behavior of EnemyAI's state machine. States decide most transitions themselves
    // via Ai.ChangeState; being stunned or killed is handled by EnemyAI for every state.
    public abstract class EnemyState : IState
    {
        protected readonly EnemyAI Ai;

        protected EnemyState(EnemyAI ai) => Ai = ai;

        public abstract EnemyAI.AiState Id { get; }

        public virtual void Enter() { }
        public abstract void Tick(float deltaTime);
        public virtual void Exit() { }
    }
}
