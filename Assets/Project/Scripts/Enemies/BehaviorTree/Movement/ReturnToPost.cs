using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // After a search that found nothing (or a fight it won): a moment standing there —
    // staring round, or over the body — then back to its post or the nearest point of its
    // route. Fails straight away when there's nothing to return from, so it can sit just
    // before Patrol.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Return To Post", "After giving up a search or winning a fight: pause, then walk back to its post. Fails if not due back.")]
    public sealed class ReturnToPost : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;

        [NonSerialized] private float _pauseLeft;
        [NonSerialized] private bool  _walking;

        protected override void OnEnter(BtContext context)
        {
            var profile = Ai.Perception.Profile;
            _pauseLeft = (Ai.ReturnIsVictory ? profile.VictoryPause : profile.LostInterestPause).EvaluateClamped();
            _walking   = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!Ai.ReturnPending) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Return);
            if (!ReturnStep.Tick(Ai, context.DeltaTime, ref _pauseLeft, ref _walking, ArrivalDistance)) return BtStatus.Running;

            Ai.FinishReturn();
            return BtStatus.Success;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
