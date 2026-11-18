using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;

namespace CGD.Enemies
{
    // The check every "go and stand over there" behavior needs before committing to a spot: it
    // is on the NavMesh, outside anything that would hurt the enemy, reachable, and a short
    // walk rather than the long way round. One shared path buffer: the tree runs on the main
    // thread and callers read nothing from it afterwards.
    internal static class ReachableSpot
    {
        private static readonly NavMeshPath Path = new();

        // `slack` and `allowance` bound the walk: at most slack × the straight (flat) distance
        // from `from`, plus allowance metres. `reached` is the NavMesh point nearest `spot`.
        public static bool TryFind(Vector3 from, Vector3 spot, float snapDistance, float slack, float allowance,
                                   HealthManager victim, out Vector3 reached)
        {
            reached = default;
            if (!NavMesh.SamplePosition(spot, out NavMeshHit hit, snapDistance, NavMesh.AllAreas)) return false;
            if (Hazards.DamagePerSecondAt(hit.position, victim) > 0f) return false;
            if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, Path) ||
                Path.status != NavMeshPathStatus.PathComplete) return false;

            Vector3 flat = hit.position - from;
            flat.y = 0f;
            if (RouteMetrics.Length(Path) > flat.magnitude * slack + allowance) return false;

            reached = hit.position;
            return true;
        }
    }
}
