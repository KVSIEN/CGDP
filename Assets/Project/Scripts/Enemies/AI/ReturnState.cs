using UnityEngine;

namespace CGD.Enemies
{
    // Heading back to its post after giving up a search ("must've been nothing") or winning
    // its fight: first a moment standing there — staring round, or over the body — then a
    // walk back to its post or the nearest point of its route, where it resumes patrolling.
    // Anything it notices on the way takes over.
    public class ReturnState : EnemyState
    {
        private const float ArrivalDistance = 0.8f;

        private float _pauseLeft;
        private bool  _walking;

        public ReturnState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Return;

        public override void Enter()
        {
            var profile = Ai.Perception.Profile;
            _pauseLeft = (Ai.ReturnIsVictory ? profile.VictoryPause : profile.LostInterestPause).EvaluateClamped();
            _walking   = false;
            Ai.Agent.isStopped = true;
        }

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            if (perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Chase);
                return;
            }
            if (perception.ConsumeLead())
            {
                Ai.ChangeToSearch();
                return;
            }

            if (ReturnStep.Tick(Ai, deltaTime, ref _pauseLeft, ref _walking, ArrivalDistance))
            {
                Ai.FinishReturn();
                Ai.ChangeState(EnemyAI.AiState.Patrol);
            }
        }

        public override void Exit()
        {
            Ai.Agent.isStopped = false;
            Ai.FinishReturn();
        }
    }
}
