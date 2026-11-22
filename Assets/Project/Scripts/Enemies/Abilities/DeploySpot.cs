using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;

namespace CGD.Enemies
{
    // Where something a caster puts down lands (a beacon, a trap): toward its aim point, at most
    // `maxDistance` away, on walkable floor — never inside a wall or on someone's head; at the
    // caster's feet when there is no floor near the spot.
    internal static class DeploySpot
    {
        public static Vector3 Toward(AbilityContext ctx, float maxDistance)
        {
            Transform caster = ctx.CasterTransform;
            Vector3 toward = ctx.HasAimPoint ? ctx.AimPoint - caster.position : caster.forward * maxDistance;
            toward.y = 0f;
            Vector3 spot = caster.position + Vector3.ClampMagnitude(toward, maxDistance) + Vector3.up;
            return NavMesh.SamplePosition(spot, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : caster.position;
        }
    }
}
