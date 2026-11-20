using UnityEngine;

namespace CGD.Enemies
{
    // Falling back to regroup, behind the Regroup node: run to where its doctrine says (MoraleDoctrine
    // › Retreat To) — a bush away from the threat, its kin, the best place it knows (its faction's
    // kind of ground, EnemyGround) or high cover from the (believed) threat, or home to its post —
    // then hold there facing the threat.
    internal static class RetreatStep
    {
        // Kin it rallies on stand this far from it when it arrives.
        private const float KinStandOff = 2.5f;
        // A bush to regroup in: at least this far from the threat, at most this far to run.
        private const float BushFromThreat = 9f;
        private const float BushMaxRun     = 25f;

        public static void Begin(EnemyAI ai)
        {
            Vector3 destination = ai.Post.Position;
            RetreatDestination to = ai.Doctrine.Morale.RetreatTo;

            if (to == RetreatDestination.Kin && TryFindRally(ai, out Vector3 rally)) destination = rally;
            else if (to == RetreatDestination.Concealment && TryFindBush(ai, out Vector3 bush)) destination = bush;
            else if (to != RetreatDestination.Den && TryFindKnownGround(ai, out Vector3 ground)) destination = ground;
            else if (to != RetreatDestination.Den && TryFindCover(ai, out Vector3 cover)) destination = cover;

            ai.Agent.isStopped = false;
            ai.Routing.SetDestination(destination);
        }

        // A bush to lie low in, away from the (believed) threat.
        private static bool TryFindBush(EnemyAI ai, out Vector3 bush)
        {
            bush = default;
            var perception = ai.Perception;
            return perception.Target != null &&
                   HidingBush.TryPick(ai, perception.TargetPosition, BushFromThreat, BushMaxRun, out bush);
        }

        // The best place it has learned for its faction (GroundDoctrine), away from the threat.
        private static bool TryFindKnownGround(EnemyAI ai, out Vector3 point)
        {
            point = default;
            var perception = ai.Perception;
            return perception.Target != null && ai.Ground.TryFallBack(perception.TargetPosition, out point);
        }

        private static bool TryFindCover(EnemyAI ai, out Vector3 cover)
        {
            cover = default;
            var perception = ai.Perception;
            return perception.Target != null && ai.Tactics.TryTakeCover(perception.TargetPosition, CoverUse.Hide, out cover);
        }

        // The nearest of its kin that still has its nerve: the squad leader first if it has one.
        private static bool TryFindRally(EnemyAI ai, out Vector3 point)
        {
            point = default;
            EnemyAI leader = ai.Squad != null ? ai.Squad.Leader : null;
            EnemyAI rally  = leader != null && leader != ai && EnemyRoster.IsKin(ai, leader) && !leader.Morale.IsShaken
                ? leader
                : NearestSteadyKin(ai);
            if (rally == null) return false;

            Vector3 toRally = rally.transform.position - ai.transform.position;
            toRally.y = 0f;
            Vector3 standOff = toRally.sqrMagnitude > KinStandOff * KinStandOff ? toRally.normalized * KinStandOff : Vector3.zero;
            point = rally.transform.position - standOff;
            return true;
        }

        private static EnemyAI NearestSteadyKin(EnemyAI ai)
        {
            EnemyAI best = null;
            float bestDistance = float.MaxValue;
            foreach (EnemyAI other in EnemyRoster.All)
            {
                if (!EnemyRoster.IsKin(ai, other) || other.Morale.IsShaken) continue;
                float distance = (other.transform.position - ai.transform.position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = other;
            }
            return best;
        }

        public static void Tick(EnemyAI ai, float deltaTime, float arrivalDistance)
        {
            ai.Motion.SetSpeed(ai.Data.ChaseSpeed * ai.Doctrine.Morale.RetreatSpeedScale);
            ai.Motion.KeepEyesOnTarget(deltaTime);

            var agent = ai.Agent;
            bool arrived = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrivalDistance);
            if (arrived) agent.isStopped = true;
        }
    }
}
