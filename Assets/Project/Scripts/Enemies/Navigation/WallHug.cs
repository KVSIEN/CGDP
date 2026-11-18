using UnityEngine;

namespace CGD.Enemies
{
    // The arithmetic of walking along walls (RouteStyle.AlongWalls): the planned route is
    // walked in legs of a few metres, and a leg's end is pulled sideways to just inside the
    // nearest wall edge when one is close. No NavMesh queries here, so it can be tested alone.
    internal static class WallHug
    {
        // How far along the planned route each leg ends, and how close a wall must be to pull it.
        public const float StepLength = 6f;
        public const float WallReach  = 4f;

        public static float Length(Vector3[] corners, int count)
        {
            float length = 0f;
            for (int i = 0; i + 1 < count; i++) length += Vector3.Distance(corners[i], corners[i + 1]);
            return length;
        }

        // The point `distance` along the route from its first corner, or its end when shorter.
        public static Vector3 PointAlong(Vector3[] corners, int count, float distance)
        {
            for (int i = 0; i + 1 < count; i++)
            {
                float leg = Vector3.Distance(corners[i], corners[i + 1]);
                if (distance <= leg && leg > 0f) return Vector3.Lerp(corners[i], corners[i + 1], distance / leg);
                distance -= leg;
            }
            return corners[count - 1];
        }

        // `clearance` metres in from the wall edge, on the side `from` is on (the open floor).
        public static Vector3 BesideWall(Vector3 edge, Vector3 from, float clearance)
        {
            Vector3 inward = from - edge;
            inward.y = 0f;
            if (inward.sqrMagnitude < 0.0001f) return edge;
            return edge + inward.normalized * clearance;
        }
    }
}
