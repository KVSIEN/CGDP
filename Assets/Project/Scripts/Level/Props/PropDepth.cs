using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // How far a prefab's solid colliders reach behind its origin (-Z, away from its front), in
    // metres: what decides how close to a wall a prop may be pushed before its back goes through
    // it. Read like PropHeights: once per prefab while playing, always fresh in the editor.
    internal static class PropDepth
    {
        private static readonly Dictionary<GameObject, float> Cache = new();

        public static float BehindOrigin(GameObject prefab)
        {
            if (prefab == null) return 0f;
            if (!Application.isPlaying) return Measure(prefab);

            if (!Cache.TryGetValue(prefab, out float behind))
                Cache[prefab] = behind = Measure(prefab);
            return behind;
        }

        private static float Measure(GameObject prefab)
        {
            Transform root = prefab.transform;
            Matrix4x4 toRoot = Matrix4x4.TRS(root.position, root.rotation, Vector3.one).inverse;
            float behind = 0f;
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
                    behind = Mathf.Max(behind, -toProp.MultiplyPoint3x4(center + offset).z);
                }
            }
            return behind;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();
    }
}
