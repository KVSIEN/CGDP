using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // After a search that found nothing (or a fight it won): a moment standing there —
    // staring round, or over the body, or feeding on it for a faction that feeds — then back
    // to its post or the nearest point of its route. Fails straight away when there's nothing
    // to return from, so it can sit just before Patrol.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Return To Post", "After giving up a search or winning a fight: pause, then walk back to its post. Fails if not due back.")]
    public sealed class ReturnToPost : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;

        [NonSerialized] private float _pauseLeft;
        [NonSerialized] private bool  _walking;
        [NonSerialized] private FeedStep _feed;
        [NonSerialized] private bool     _isFeeding;

        protected override void OnEnter(BtContext context)
        {
            var profile = Ai.Perception.Profile;
            _pauseLeft = (Ai.ReturnIsVictory ? profile.VictoryPause : profile.LostInterestPause).EvaluateClamped();
            // Standing over a kill is the faction's own ritual: Void stares on, Bio sniffs round it.
            if (Ai.ReturnIsVictory) _pauseLeft *= Ai.Doctrine.Aftermath.VictoryPauseScale;
            _walking   = false;
            Ai.Idle.BeginAftermath(UnityEngine.Time.time);

            // Feeding takes the place of the pause over the body.
            _isFeeding = Ai.ReturnIsVictory && FeedStep.WantsToFeed(Ai, Ai.ReturnBody);
            if (!_isFeeding) return;

            _feed ??= new FeedStep();
            _feed.Begin(Ai.ReturnBody, Ai.Doctrine.Aftermath.FeedSeconds, UnityEngine.Time.time);
            _pauseLeft = 0f;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!Ai.ReturnPending) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Return);
            if (_isFeeding)
            {
                if (!_feed.Tick(Ai, context.DeltaTime, UnityEngine.Time.time)) return BtStatus.Running;
                StopFeeding();
            }
            if (!ReturnStep.Tick(Ai, context.DeltaTime, ref _pauseLeft, ref _walking, ArrivalDistance)) return BtStatus.Running;

            Ai.FinishReturn();
            return BtStatus.Success;
        }

        protected override void OnExit(BtContext context)
        {
            StopFeeding();
            Ai.Agent.isStopped = false;
        }

        private void StopFeeding()
        {
            if (!_isFeeding) return;
            _isFeeding = false;
            _feed.End(Ai);
        }
    }
}
