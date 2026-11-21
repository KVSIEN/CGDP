using UnityEngine;
using UnityEngine.Rendering;

namespace CGD.Core
{
    // Visual-only pieces effects build in code on a child of their own: no collider, so they
    // never block sight, shots or movement, and no shadows cast or taken.
    public static class EffectParts
    {
        // A trail that tapers from `width` to nothing over `time` seconds, not yet emitting.
        public static TrailRenderer Trail(string name, Transform parent, Vector3 localPosition, Material material,
                                          float time, float width, float minVertexDistance)
        {
            var trail = Child(name, parent, localPosition).AddComponent<TrailRenderer>();
            trail.sharedMaterial    = material;
            trail.time              = time;
            trail.widthMultiplier   = width;
            trail.widthCurve        = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.minVertexDistance = minVertexDistance;
            trail.emitting          = false;
            NoShadows(trail);
            return trail;
        }

        public static MeshRenderer MeshPart(string name, Transform parent, Mesh mesh, Material material,
                                        Vector3 localPosition, Vector3 localScale)
        {
            GameObject part = Child(name, parent, localPosition);
            part.transform.localScale = localScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            NoShadows(renderer);
            return renderer;
        }

        public static void NoShadows(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows    = false;
        }

        private static GameObject Child(string name, Transform parent, Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            return child;
        }
    }
}
