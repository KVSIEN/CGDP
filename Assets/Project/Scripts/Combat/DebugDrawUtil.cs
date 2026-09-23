using UnityEngine;

namespace CGD.Combat
{
    internal static class DebugDrawUtil
    {
        public static void Sphere(Vector3 center, float radius, Color color, float duration)
        {
            const int seg = 16;
            Circle(center, Vector3.right, Vector3.up, radius, color, duration, seg);
            Circle(center, Vector3.up, Vector3.forward, radius, color, duration, seg);
            Circle(center, Vector3.forward, Vector3.right, radius, color, duration, seg);
        }

        public static void Circle(Vector3 center, Vector3 tan, Vector3 bitan,
            float radius, Color color, float duration, int segments)
        {
            Vector3 prev = center + tan * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = center + (tan * Mathf.Cos(a) + bitan * Mathf.Sin(a)) * radius;
                Debug.DrawLine(prev, next, color, duration);
                prev = next;
            }
        }

        public static void Box(Vector3 center, Vector3 half, Quaternion rot,
            Color color, float duration)
        {
            Vector3 r = rot * Vector3.right * half.x;
            Vector3 u = rot * Vector3.up * half.y;
            Vector3 f = rot * Vector3.forward * half.z;

            Vector3[] c =
            {
                center - r - u - f, center + r - u - f,
                center + r + u - f, center - r + u - f,
                center - r - u + f, center + r - u + f,
                center + r + u + f, center - r + u + f,
            };

            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(c[i], c[(i + 1) % 4], color, duration);
                Debug.DrawLine(c[i + 4], c[(i + 1) % 4 + 4], color, duration);
                Debug.DrawLine(c[i], c[i + 4], color, duration);
            }
        }
    }
}
