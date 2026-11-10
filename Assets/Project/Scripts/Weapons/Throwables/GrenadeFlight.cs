using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // Where a thrown grenade goes off. Steps its flight the way Grenade moves it — the grenade's
    // own gravity, its bounces (GrenadeData.BounceVelocity), then sliding and rolling to a stop
    // under its ground drag — until the fuse runs out, or for an impact fuse until it first hits
    // something. Lets a thrower aim for where the grenade will explode rather than where it
    // first touches down, which a bouncy grenade or a sloping floor can carry metres away.
    // Characters don't stop the simulated flight: the throw is aimed at one, and what it does
    // on hitting them can't be known in advance.
    public static class GrenadeFlight
    {
        private const float Step          = 0.04f;
        private const float SurfaceOffset = 0.02f;
        // How far below a rolling grenade the ground may drop before it rolls off an edge.
        private const float GroundProbe   = 0.3f;

        // `landedAfter` is the seconds until it first touched a surface (its whole flight time if
        // it never did).
        public static Vector3 Detonation(Vector3 origin, Vector3 velocity, GrenadeData data, LayerMask surfaces,
                                         out float landedAfter)
        {
            Vector3 gravity  = data.Gravity;
            Vector3 position = origin;
            bool    rolling  = false;
            landedAfter = -1f;

            for (float time = 0f; time < data.FuseTime; time += Step)
            {
                if (rolling)
                {
                    if (!Roll(ref position, ref velocity, data, surfaces, out rolling)) break;
                    continue;
                }

                Vector3 next = position + velocity * Step + 0.5f * Step * Step * gravity;
                if (!CastPastCharacters(position, next, surfaces, out RaycastHit hit))
                {
                    velocity += gravity * Step;
                    position  = next;
                    continue;
                }

                if (landedAfter < 0f) landedAfter = time;
                if (data.Fuse == FuseMode.OnImpact) return hit.point;

                float   share = hit.distance / Mathf.Max(0.0001f, Vector3.Distance(position, next));
                Vector3 atHit = velocity + gravity * (Step * share);
                velocity = data.BounceVelocity(atHit, hit.normal);
                position = hit.point + hit.normal * SurfaceOffset;

                // No rebound off something flat enough to stand on: it slides along the ground.
                rolling = hit.normal.y >= Grenade.GroundNormalY && data.Settles(atHit, hit.normal);
            }

            if (landedAfter < 0f) landedAfter = data.FuseTime;
            return position;
        }

        // One step of sliding along the ground, slowed by its drag. False once it has come to rest.
        private static bool Roll(ref Vector3 position, ref Vector3 velocity, GrenadeData data, LayerMask surfaces, out bool rolling)
        {
            rolling = true;
            float speed  = velocity.magnitude;
            float slowed = speed - data.GroundDrag * Step;
            if (slowed <= Grenade.StopSpeed) return false;

            velocity *= slowed / speed;
            Vector3 next = position + velocity * Step;

            // Into a wall or a step: it bounces off it.
            if (CastPastCharacters(position, next, surfaces, out RaycastHit wall))
            {
                velocity = data.BounceVelocity(velocity, wall.normal);
                position = wall.point + wall.normal * SurfaceOffset;
                return true;
            }

            // Off an edge: it falls again.
            if (!Physics.Raycast(next + Vector3.up * 0.1f, Vector3.down, out RaycastHit ground, GroundProbe + 0.1f,
                    surfaces, QueryTriggerInteraction.Ignore) || ground.normal.y < Grenade.GroundNormalY)
            {
                rolling  = false;
                position = next;
                return true;
            }

            position = new Vector3(next.x, ground.point.y + SurfaceOffset, next.z);
            return true;
        }

        private static bool CastPastCharacters(Vector3 from, Vector3 to, LayerMask surfaces, out RaycastHit hit) =>
            Physics.Linecast(from, to, out hit, surfaces, QueryTriggerInteraction.Ignore) &&
            Hitbox.FindDamageable(hit.collider) == null;
    }
}
