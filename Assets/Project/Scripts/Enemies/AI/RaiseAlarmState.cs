namespace CGD.Enemies
{
    // Runs to an alarm panel near its post and raises the alarm (see RaiseAlarmStep). If the
    // target closes in, or the panel is disabled first, it fights instead.
    public class RaiseAlarmState : EnemyState
    {
        private readonly RaiseAlarmStep _step = new();

        public RaiseAlarmState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.RaiseAlarm;

        public override void Enter() => _step.Begin(Ai);

        public override void Tick(float deltaTime)
        {
            if (_step.Tick(Ai, deltaTime) == RaiseAlarmStep.Result.Running) return;

            if (Ai.Perception.IsTargetDetected) Ai.ChangeState(EnemyAI.AiState.Chase);
            else Ai.ChangeToSearch();
        }

        public override void Exit() => Ai.Agent.isStopped = false;
    }
}
