using UnityEngine;

namespace CGD.Enemies
{
    // The arithmetic behind interest points, apart from any scene: how much a point is wanted,
    // and where its users stand.
    internal static class InterestRules
    {
        // Appeal, fading with distance to nothing at the kind's reach, and put off by fresh danger
        // near it. 0 means it will not go.
        public static float Weight(float appeal, float distance, float reach, float danger)
        {
            if (appeal <= 0f || distance > reach) return 0f;
            float nearness = 1f - 0.5f * Mathf.Clamp01(distance / reach);
            return appeal * nearness * (1f - Mathf.Clamp01(danger));
        }

        // Slot `index` of `count` round `centre`: one stands right on it, several are spread evenly round it.
        public static Vector3 SpotAround(Vector3 centre, int index, int count, float radius)
        {
            if (count <= 1) return centre;
            float angle = index * 360f / count;
            return centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
        }
    }
}
