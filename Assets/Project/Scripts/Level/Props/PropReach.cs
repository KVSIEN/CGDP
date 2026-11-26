using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // How far a prefab's solid colliders reach across the floor from its origin, in metres: what
    // decides how close to a wall a prop may stand before it goes through it. Read like
    // PropHeights: once per prefab while playing, always fresh in the editor.
    internal static class PropReach
    {
        private static readonly Dictionary<GameObject, Vector2[]> Cache = new();

        // Behind its origin (-Z, away from its front).
        public static float BehindOrigin(GameObject prefab) =>
            TryGetExtent(prefab, 0f, Vector2.one, out Rect extent) ? Mathf.Max(0f, -extent.yMin) : 0f;

        // The floor it covers round its origin once scaled by `stretch` (across, front to back)
        // and turned by `yaw` degrees; x is across, y front to back. False when nothing is solid.
        public static bool TryGetExtent(GameObject prefab, float yaw, Vector2 stretch, out Rect extent)
        {
            extent = default;
            if (prefab == null) return false;

            Vector2[] corners = CornersOf(prefab);
            if (corners.Length == 0) return false;

            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            foreach (Vector2 corner in corners)
            {
                Vector3 turned = turn * new Vector3(corner.x * stretch.x, 0f, corner.y * stretch.y);
                var at = new Vector2(turned.x, turned.z);
                min = Vector2.Min(min, at);
                max = Vector2.Max(max, at);
            }
            extent = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private static Vector2[] CornersOf(GameObject prefab)
        {
            if (!Application.isPlaying) return Measure(prefab);

            if (!Cache.TryGetValue(prefab, out Vector2[] corners))
                Cache[prefab] = corners = Measure(prefab);
            return corners;
        }

        // The corners of every solid collider's box, seen from above, from the root's place and
        // turn but with its scale, which a spawned prop keeps.
        private static Vector2[] Measure(GameObject prefab)
        {
            Transform root = prefab.transform;
            Matrix4x4 toRoot = Matrix4x4.TRS(root.position, root.rotation, Vector3.one).inverse;
            var corners = new List<Vector2>();
            foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || !PropHeights.TryGetLocalBox(collider, out Vector3 center, out Vector3 size)) continue;

                Matrix4x4 toProp = toRoot * collider.transform.localToWorldMatrix;
                Vector3 half = size * 0.5f;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -half.x : half.x,
                                             (corner & 2) == 0 ? -half.y : half.y,
                                             (corner & 4) == 0 ? -half.z : half.z);
                    Vector3 at = toProp.MultiplyPoint3x4(center + offset);
                    corners.Add(new Vector2(at.x, at.z));
                }
            }
            return corners.Count == 0 ? Array.Empty<Vector2>() : corners.ToArray();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();
    }
}
