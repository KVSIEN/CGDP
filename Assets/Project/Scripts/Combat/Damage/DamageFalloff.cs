using UnityEngine;

namespace CGD.Combat
{
    // Distance-based damage scaling. Hitscan shots evaluate it at the raycast distance and
    // projectiles at the distance they have flown, so a target takes the same damage no
    // matter which of the two resolved the shot.
    public readonly struct DamageFalloff
    {
        public readonly float Optimal;    // full damage out to here
        public readonly float FalloffEnd; // MinScale from here on
        public readonly float MinScale;

        public DamageFalloff(float optimal, float falloffEnd, float minScale)
        {
            Optimal    = optimal;
            FalloffEnd = falloffEnd;
            MinScale   = minScale;
        }

        // For damage sources that don't scale with distance at all.
        public static DamageFalloff None => new(0f, 0f, 1f);

        public float Evaluate(float distance) =>
            Mathf.Lerp(1f, MinScale, Mathf.InverseLerp(Optimal, FalloffEnd, distance));
    }
}
