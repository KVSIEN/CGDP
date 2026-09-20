namespace CGD.Enemies
{
    // Closes in on the detected target and attacks at range; drops to Alert when the
    // target is no longer detected (unless an attack is already winding up).
    public class ChaseState : EnemyState
    {
        public ChaseState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Chase;

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            var agent      = Ai.Agent;

            if (Ai.IsAttacking)
            {
                if (perception.Target != null) Ai.FaceTowards(perception.Target.transform.position, deltaTime);
                return;
            }

            if (!perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Alert);
                return;
            }

            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            var targetPos = perception.Target.transform.position;
            float range   = Ai.Data.AttackRange;

            if ((Ai.transform.position - targetPos).sqrMagnitude <= range * range)
            {
                agent.isStopped = true;
                Ai.FaceTowards(targetPos, deltaTime);
                Ai.TryStartAttack();
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(targetPos);
            }
        }

        public override void Exit() => Ai.Agent.isStopped = false;
    }
}
