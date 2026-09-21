using UnityEngine;

namespace CGD.UI
{
    // Where each hit in a group lands, kept free of pooling and rendering so the shape a burst
    // builds can be reasoned about (and changed) on its own.
    public static class DamageNumberLayout
    {
        // A point somewhere in a small patch around the impact, widening a little as the group
        // fills. Deliberately scattered: hits should read as a loose cluster on the target, and
        // any fixed ordering — however tidy on paper — lines the numbers up into rows or trails
        // that the eye follows instead of reading the group as a whole.
        public static Vector2 ScatterOffset(int indexInCluster)
        {
            float growth = Mathf.Min(indexInCluster, DamageNumberStyle.ScatterGrowthMax)
                         * DamageNumberStyle.ScatterGrowthPx;

            Vector2 unit = Random.insideUnitCircle;
            return new Vector2(unit.x * (DamageNumberStyle.ScatterXPx + growth),
                               unit.y * (DamageNumberStyle.ScatterYPx + growth * 0.5f));
        }

        // The direction and speed a number is thrown at. Every one differs, so two numbers that
        // happen to land on the same spot still separate as they rise.
        public static Vector2 LaunchVelocity()
        {
            float tilt  = Random.Range(-DamageNumberStyle.LaunchTiltDeg, DamageNumberStyle.LaunchTiltDeg)
                        * Mathf.Deg2Rad;
            float speed = DamageNumberStyle.RiseSpeedPx
                        * Random.Range(1f - DamageNumberStyle.SpeedVariance, 1f + DamageNumberStyle.SpeedVariance);

            return new Vector2(Mathf.Sin(tilt), Mathf.Cos(tilt)) * speed;
        }
    }
}
