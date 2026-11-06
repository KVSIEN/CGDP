using UnityEngine;

namespace CGD.Enemies
{
    // A melee enemy's fight. Each tick it picks one engage step:
    //   strike     in range and it's its turn (only a few allies melee one target at once)
    //   circle     close, but others are already on the target: wait its turn at a distance
    //   flank      still some way off: now and then circle round by the least visible route
    //   hold       the target is somewhere it can't walk to: stand at the end of the path, facing it
    //   approach   otherwise, close in
    // Shaken morale sends it to Retreat; losing the target sends it searching; if it raises
    // alarms and knows a panel nearby, it runs to that first.
    public class ChaseState : EnemyState
    {
        private const float FlankMinDistance = 7f;
        private const float FlankChance      = 0.4f;
        private const float ArrivalDistance  = 0.8f;
        private const float TurnRing         = 3f;     // metres beyond attack range where it needs its turn
        private const float CircleRadius     = 2.5f;   // metres beyond attack range it waits at
        private static readonly Vector2 FlankCheckEvery = new(5f, 9f);

        private float _nextFlankCheck;
        private bool  _flanking;

        public ChaseState(EnemyAI ai) : base(ai) { }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Chase;

        public override void Enter()
        {
            _flanking       = false;
            _nextFlankCheck = Time.time + Random.Range(0f, FlankCheckEvery.x);
        }

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            var agent      = Ai.Agent;

            if (Ai.IsAttacking)
            {
                if (perception.Target != null) Ai.FaceTowards(perception.TargetPosition, deltaTime);
                Ai.ReportStep(EngageStep.Strike);
                return;
            }

            if (Ai.ShouldRetreat)                   { Ai.ChangeState(EnemyAI.AiState.Retreat);    return; }
            if (!perception.IsTargetDetected)       { Ai.ChangeToSearch();                       return; }
            if (Ai.TryFindAlarm(out _))             { Ai.ChangeState(EnemyAI.AiState.RaiseAlarm); return; }

            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            var   target    = perception.Target;
            var   targetPos = perception.TargetPosition;
            float range     = Ai.Data.AttackRange;
            float distance  = Vector3.Distance(Ai.transform.position, targetPos);

            if (distance <= range + TurnRing && !Ai.Tactics.TryTakeMeleeTurn(target))
            {
                _flanking = false;
                Ai.Tactics.CircleTarget(targetPos, range + CircleRadius);
                Ai.FaceTowards(targetPos, deltaTime);
                Ai.ReportStep(EngageStep.Circle);
                return;
            }

            if (distance <= range)
            {
                _flanking = false;
                agent.isStopped = true;
                Ai.FaceTowards(targetPos, deltaTime);
                Ai.TryStartAttack();
                Ai.ReportStep(EngageStep.Strike);
                return;
            }

            agent.isStopped = false;
            if (TickFlank(targetPos, distance))
            {
                Ai.ReportStep(EngageStep.Flank);
                return;
            }

            agent.SetDestination(targetPos);
            if (Ai.IsPathBlocked)
            {
                agent.isStopped = true;
                Ai.FaceTowards(targetPos, deltaTime);
                Ai.ReportStep(EngageStep.Hold);
                return;
            }
            Ai.ReportStep(EngageStep.Approach);
        }

        public override void Exit() => Ai.Agent.isStopped = false;

        // True while it is circling round rather than heading straight for the target.
        private bool TickFlank(Vector3 targetPos, float distance)
        {
            if (_flanking)
            {
                var agent = Ai.Agent;
                bool arrived = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= ArrivalDistance);
                if (arrived || distance < Ai.Data.AttackRange * 2f) _flanking = false;
                return _flanking;
            }

            if (Time.time < _nextFlankCheck || distance < FlankMinDistance) return false;
            _nextFlankCheck = Time.time + Random.Range(FlankCheckEvery.x, FlankCheckEvery.y);

            // A squad's flankers always go round, from the side the squad gives them.
            bool squadFlanker = Ai.SquadRole == SquadRole.Flank;
            if (!squadFlanker && Random.value >= FlankChance) return false;
            int side = squadFlanker ? Ai.Squad.NextFlankSide() : 0;
            if (!Ai.Tactics.TryPlanFlank(targetPos, Ai.Data.AttackRange + 2f, out Vector3 destination, side)) return false;

            _flanking = Ai.Agent.SetDestination(destination);
            return _flanking;
        }
    }
}
