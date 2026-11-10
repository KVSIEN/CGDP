using UnityEngine;

namespace CGD.Enemies
{
    // Falling back to regroup, behind the Regroup node: run to high cover
    // from the (believed) threat, or home to its post, then hold there facing the threat.
    internal static class RetreatStep
    {
        public static void Begin(EnemyAI ai)
        {
            var perception = ai.Perception;
            Vector3 destination = ai.Post.Position;
            if (perception.Target != null && ai.Tactics.TryTakeCover(perception.TargetPosition, CoverUse.Hide, out Vector3 cover))
                destination = cover;

            ai.Agent.isStopped = false;
            ai.Agent.SetDestination(destination);
        }

        public static void Tick(EnemyAI ai, float deltaTime, float arrivalDistance)
        {
            ai.SetSpeed(ai.Data.ChaseSpeed);
            ai.KeepEyesOnTarget(deltaTime);

            var agent = ai.Agent;
            bool arrived = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrivalDistance);
            if (arrived) agent.isStopped = true;
        }
    }
}
