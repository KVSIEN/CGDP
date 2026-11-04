using UnityEngine;

namespace CGD.Combat
{
    // The two ends of a blade's capsule at one instant of a swing. Plain maths so the swing
    // can be tested without physics.
    public readonly struct SwingBlade
    {
        public readonly Vector3 Near;
        public readonly Vector3 Far;

        private SwingBlade(Vector3 near, Vector3 far)
        {
            Near = near;
            Far  = far;
        }

        // `aim` maps the pose onto the world (forward = where the wielder looks). `nearDistance`
        // and `farDistance` are the blade's ends measured along its direction from `origin`.
        public static SwingBlade At(Vector3 origin, Quaternion aim, SwingPose pose,
                                    float nearDistance, float farDistance)
        {
            // Unity pitches downward for positive X, so negate to make Pitch read "up".
            Vector3 direction = aim * Quaternion.Euler(-pose.Pitch, pose.Yaw, 0f) * Vector3.forward;
            return new SwingBlade(
                origin + direction * (nearDistance + pose.Push),
                origin + direction * (farDistance + pose.Push));
        }

        // How many capsule queries cover the move from `from` to `to` without gaps: one per
        // `spacing` of travel by the faster end, so slow swings cost one query and fast ones a few.
        public static int StepsBetween(SwingBlade from, SwingBlade to, float spacing, int maxSteps)
        {
            float travel = Mathf.Max((to.Far - from.Far).magnitude, (to.Near - from.Near).magnitude);
            int steps = Mathf.CeilToInt(travel / Mathf.Max(spacing, 0.01f));
            return Mathf.Clamp(steps, 1, Mathf.Max(maxSteps, 1));
        }
    }
}
