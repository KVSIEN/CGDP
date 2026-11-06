using UnityEngine;

namespace CGD.Enemies
{
    // Ranged variant of Chase. Fights from cover it knows when it can: runs to a spot that
    // screens it from the target, shoots from there while the cover holds, and moves to new
    // cover every so often or once the target has worked round it. With no cover it holds
    // its preferred distance and strafes (EnemyRangedCombat). Falls back to Alert when the
    // target is lost.
    public class RangedChaseState : EnemyState
    {
        private const float ArrivalDistance = 0.6f;
        private const float ExposedRetry    = 1.5f;
        private static readonly Vector2 RepositionEvery = new(6f, 10f);

        private float _repositionAt;
        private bool  _movingToCover;

        public RangedChaseState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Chase;

        public override void Enter()
        {
            Ai.RangedCombat.ResetSpacing();
            _repositionAt  = 0f;
            _movingToCover = false;
        }

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            if (!perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Alert);
                return;
            }

            var combat    = Ai.RangedCombat;
            var tactics   = Ai.Tactics;
            var targetPos = perception.TargetPosition;
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            Ai.FaceTowards(targetPos, deltaTime);

            // Stand still while a burst is going out.
            if (combat.IsBursting)
            {
                Ai.Agent.isStopped = true;
                return;
            }

            if (Time.time >= _repositionAt) TryMoveToCover(targetPos);
            if (_movingToCover)
            {
                if (!HasArrived()) return;
                _movingToCover = false;
            }

            if (tactics.IsInCover(targetPos))
            {
                Ai.Agent.isStopped = true;
                if (combat.HasLineOfSight(targetPos)) combat.TryStartBurst();
                return;
            }

            // Out in the open (no cover known, or the target got round it): look for new cover soon.
            _repositionAt = Mathf.Min(_repositionAt, Time.time + ExposedRetry);
            if (combat.HoldPreferredRange(targetPos, deltaTime) && combat.HasLineOfSight(targetPos))
                combat.TryStartBurst();
        }

        public override void Exit()
        {
            Ai.Agent.isStopped = false;
            Ai.RangedCombat.CancelBurst();
        }

        private void TryMoveToCover(Vector3 targetPos)
        {
            _repositionAt = Time.time + Random.Range(RepositionEvery.x, RepositionEvery.y);
            if (!Ai.Tactics.TryTakeCover(targetPos, CoverUse.Fight, out Vector3 destination)) return;

            Ai.Agent.isStopped = false;
            _movingToCover = Ai.Agent.SetDestination(destination);
        }

        private bool HasArrived()
        {
            var agent = Ai.Agent;
            return !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= ArrivalDistance);
        }
    }
}
