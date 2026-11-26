using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // How tall a prop stands above the floor, from the tops of its solid colliders: what an eye
    // sees over (SightBlocking) and what a crouching person hides behind, as enemies' sight
    // (physics lines) finds in play. Box, sphere, capsule and mesh colliders are read. A prop
    // with no solid collider stops no line in play, so it is seen through (0); a missing prefab
    // is taken to be as tall as the room.
    //
    // Asked for the same few prefabs again and again while a level is planned, so answered once
    // per prefab while playing; in the editor (Room Lab) prefabs are being changed, so it always looks.
    public static class PropHeights
    {
        private static readonly Dictionary<GameObject, float> Cache = new();

        public static float Of(GameObject prefab)
        {
            if (prefab == null) return float.PositiveInfinity;
            if (!Application.isPlaying) return Measure(prefab);

            if (!Cache.TryGetValue(prefab, out float height))
                Cache[prefab] = height = Measure(prefab);
            return height;
        }

        private static float Measure(GameObject prefab)
        {
            // From the root's place and turn, but with its scale, which a spawned prop keeps.
            Transform root = prefab.transform;
            Matrix4x4 toRoot = Matrix4x4.TRS(root.position, root.rotation, Vector3.one).inverse;
            float top = float.NegativeInfinity;
            foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || !TryGetLocalBox(collider, out Vector3 center, out Vector3 size)) continue;

                // The prop's height is along the root's up, whatever the collider's own turn.
                Matrix4x4 toProp = toRoot * collider.transform.localToWorldMatrix;
                Vector3 half = size * 0.5f;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -half.x : half.x,
                                             (corner & 2) == 0 ? -half.y : half.y,
                                             (corner & 4) == 0 ? -half.z : half.z);
                    top = Mathf.Max(top, toProp.MultiplyPoint3x4(center + offset).y);
                }
            }
            return float.IsNegativeInfinity(top) ? 0f : Mathf.Max(0f, top);
        }

        // The box round a collider in its own transform's space.
        internal static bool TryGetLocalBox(Collider collider, out Vector3 center, out Vector3 size)
        {
            switch (collider)
            {
                case BoxCollider box:
                    center = box.center;
                    size   = box.size;
                    return true;
                case SphereCollider sphere:
                    center = sphere.center;
                    size   = Vector3.one * (sphere.radius * 2f);
                    return true;
                case CapsuleCollider capsule:
                    center = capsule.center;
                    size   = Vector3.one * (capsule.radius * 2f);
                    size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2f);
                    return true;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    center = mesh.sharedMesh.bounds.center;
                    size   = mesh.sharedMesh.bounds.size;
                    return true;
                default:
                    center = size = Vector3.zero;
                    return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();
    }
}
