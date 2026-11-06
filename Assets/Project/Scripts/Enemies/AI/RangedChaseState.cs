namespace CGD.Enemies
{
    // Ranged variant of Chase: holds the preferred distance, strafes at range and fires
    // bursts when the target is visible (see EnemyRangedCombat). Falls back to Alert when
    // the target is lost.
    public class RangedChaseState : EnemyState
    {
        public RangedChaseState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Chase;

        public override void Enter() => Ai.RangedCombat.ResetSpacing();

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            if (!perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Alert);
                return;
            }

            var combat    = Ai.RangedCombat;
            var targetPos = perception.Target.transform.position;
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            Ai.FaceTowards(targetPos, deltaTime);

            // Stand still while a burst is going out.
            if (combat.IsBursting)
            {
                Ai.Agent.isStopped = true;
                return;
            }

            if (combat.HoldPreferredRange(targetPos, deltaTime) && combat.HasLineOfSight(targetPos))
                combat.TryStartBurst();
        }

        public override void Exit()
        {
            Ai.Agent.isStopped = false;
            Ai.RangedCombat.CancelBurst();
        }
    }
}
