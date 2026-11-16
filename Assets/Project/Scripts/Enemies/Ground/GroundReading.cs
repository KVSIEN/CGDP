using UnityEngine;

namespace CGD.Enemies
{
    // Reads a spot from eight rays sent out round an enemy at eye height (every 45°, starting
    // straight ahead of the world's +Z): how far each one got before it hit something. Pure
    // arithmetic, so it can be tested without physics; EnemyGround casts the rays.
    internal static class GroundReading
    {
        public const int Rays = 8;
        // A lane this long makes a vantage, with something solid this close beside it.
        public const float VantageLane  = 15f;
        public const float CoverBeside  = 1.5f;
        // Opposite rays adding up to no more than this: a corridor.
        public const float TightAcross  = 4.5f;

        public static Vector3 Direction(int ray)
        {
            float angle = ray * (360f / Rays) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }

        // `reach[i]`: how far ray i got. `lane`: the ray that got furthest.
        public static PlaceTraits Classify(float[] reach, bool inConcealment, out int lane)
        {
            lane = 0;
            float shortest = float.MaxValue;
            for (int i = 0; i < Rays; i++)
            {
                if (reach[i] > reach[lane]) lane = i;
                shortest = Mathf.Min(shortest, reach[i]);
            }

            PlaceTraits traits = inConcealment ? PlaceTraits.Hidden : PlaceTraits.None;
            if (reach[lane] >= VantageLane && shortest <= CoverBeside) traits |= PlaceTraits.Vantage;
            for (int i = 0; i < Rays / 2; i++)
                if (reach[i] + reach[i + Rays / 2] <= TightAcross)
                {
                    traits |= PlaceTraits.Tight;
                    break;
                }
            return traits;
        }
    }
}
