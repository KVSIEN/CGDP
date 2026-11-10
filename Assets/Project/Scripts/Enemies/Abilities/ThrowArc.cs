using UnityEngine;

namespace CGD.Enemies
{
    // The launch velocity that lobs something from one point onto another under gravity, at
    // a chosen upward angle: the arc an enemy throws a grenade along, and how far to lead a
    // moving target. Pure maths, no physics queries, so it can be tested.
    public static class ThrowArc
    {
        // False when no throw at that angle lands there within the speed limits.
        public static bool TrySolve(Vector3 from, Vector3 to, float angleDegrees, float minSpeed, float maxSpeed,
                                    float gravity, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            Vector3 flat = new(to.x - from.x, 0f, to.z - from.z);
            float distance = flat.magnitude;
            float height   = to.y - from.y;
            if (distance < 0.01f || gravity <= 0f) return false;

            float angle = angleDegrees * Mathf.Deg2Rad;
            float cos   = Mathf.Cos(angle);
            float tan   = Mathf.Tan(angle);

            // distance·tanθ − height = g·distance² / (2·v²·cos²θ), solved for v.
            float lift = distance * tan - height;
            if (lift <= 0f) return false;

            float speed = Mathf.Sqrt(gravity * distance * distance / (2f * cos * cos * lift));
            if (speed < minSpeed || speed > maxSpeed) return false;

            Vector3 horizontal = flat / distance;
            velocity = horizontal * (speed * cos) + Vector3.up * (speed * Mathf.Sin(angle));
            return true;
        }

        // Seconds a throw at this velocity takes to cover the flat distance to `to`.
        public static float FlightTime(Vector3 from, Vector3 to, Vector3 velocity)
        {
            float flatSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            return flatSpeed > 0.01f ? new Vector2(to.x - from.x, to.z - from.z).magnitude / flatSpeed : 0f;
        }

        // Where a target moving along the ground at `velocity` will be after `seconds`, never
        // more than `maxLead` metres from where it is now (it may turn).
        public static Vector3 Lead(Vector3 position, Vector3 velocity, float seconds, float maxLead)
        {
            Vector3 flat = new(velocity.x, 0f, velocity.z);
            return position + Vector3.ClampMagnitude(flat * Mathf.Max(0f, seconds), Mathf.Max(0f, maxLead));
        }
    }
}
