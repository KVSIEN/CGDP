using UnityEngine;
using CGD.Core;
using CGD.Stealth;

namespace CGD.Abilities
{
    // Tech's Smoke Screen: bursts a smoke cloud where the caster stands (a ConcealmentZone that
    // blocks sight), then dashes off inside it in a random direction, a share of the cloud's
    // radius, so whoever was watching loses it in the smoke and can't tell which way it went.
    // It never dashes out of its own cloud: the dash is shorter than the radius. Needs a caster
    // that can dash on its own (enemies).
    [CreateAssetMenu(fileName = "SmokeDashAbility", menuName = "CGD/Abilities/Smoke Dash")]
    public class SmokeDashAbility : Ability
    {
        // Directions it tries, evenly spread from a random start; one that runs into a wall
        // after a step gives way to the next.
        private const int   Tries     = 6;
        private const float FullReach = 0.8f;

        [Header("Smoke")]
        [Tooltip("Pooled prefab with a ConcealmentZone (Prefabs/VFX/SmokeCloud) and a sphere, capsule or box collider giving its size")]
        public GameObject CloudPrefab;

        [Header("Dash")]
        [Tooltip("Share of the cloud's radius it dashes (0.5 = half way to the edge)")]
        [Range(0.1f, 0.9f)] public float ReachShare = 0.5f;
        [Tooltip("Seconds the dash takes")]
        [Min(0.05f)] public float Duration = 0.3f;

        // Vanishing is the point: the cloud hides it, so the cast shouldn't give it away.
        public override bool GivesAway => false;

        public override bool CanExecute(AbilityContext ctx) =>
            ctx.Dasher != null && !ctx.Dasher.IsDashing && ctx.CasterTransform != null && CloudRadius() > 0f;

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.Dasher == null || ctx.CasterTransform == null) return;

            float radius = CloudRadius();
            if (radius <= 0f) return;

            PrefabPool.Spawn(CloudPrefab, ctx.CasterTransform.position, Quaternion.identity);
            float distance = radius * ReachShare;
            ctx.Dasher.Begin(PickDirection(ctx.Dasher, distance), distance, Duration, distance, null);
        }

        // The cloud's radius on the ground; 0 without a cloud that hides.
        public float CloudRadius()
        {
            if (CloudPrefab == null || !CloudPrefab.TryGetComponent(out ConcealmentZone _)) return 0f;

            Vector3 scale = CloudPrefab.transform.localScale;
            float   flat  = Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            if (CloudPrefab.TryGetComponent(out SphereCollider sphere))   return sphere.radius * flat;
            if (CloudPrefab.TryGetComponent(out CapsuleCollider capsule)) return capsule.radius * flat;
            if (CloudPrefab.TryGetComponent(out BoxCollider box))
                return Mathf.Min(box.size.x * Mathf.Abs(scale.x), box.size.z * Mathf.Abs(scale.z)) * 0.5f;
            return 0f;
        }

        // Any direction: the first, from a random start, that isn't walled off soon after it sets
        // off; hemmed in on every side, the one that gets furthest.
        private static Vector3 PickDirection(IDasher dasher, float distance)
        {
            float start = Random.Range(0f, 360f);
            Vector3 best = Vector3.forward;
            float bestReach = -1f;
            for (int i = 0; i < Tries; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, start + i * (360f / Tries), 0f) * Vector3.forward;
                float reach = dasher.Reach(direction, distance);
                if (reach >= distance * FullReach) return direction;
                if (reach <= bestReach) continue;

                bestReach = reach;
                best      = direction;
            }
            return best;
        }
    }
}
