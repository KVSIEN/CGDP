using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // If this enemy raises alarms (EnemyData) and knows a panel near its post, runs there and
    // sounds it — once per fight. Fails at once otherwise, so it can sit at the start of a
    // fight branch. Succeeds when raised; fails if the target closes in or the panel is
    // disabled first.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Raise Alarm", "Runs to an alarm panel near its post and sounds it, once per fight. Fails if it can't.")]
    public sealed class RaiseAlarm : EnemyAction
    {
        [NonSerialized] private RaiseAlarmStep _step;
        [NonSerialized] private bool _started;

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _step = new RaiseAlarmStep();
        }

        protected override void OnEnter(BtContext context) =>
            _started = Ai.TryFindAlarm(out _) && _step.Begin(Ai);

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_started) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.RaiseAlarm);
            return _step.Tick(Ai, context.DeltaTime) switch
            {
                RaiseAlarmStep.Result.Running => BtStatus.Running,
                RaiseAlarmStep.Result.Raised  => BtStatus.Success,
                _                             => BtStatus.Failure,
            };
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
