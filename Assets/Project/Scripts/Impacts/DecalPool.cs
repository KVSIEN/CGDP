using UnityEngine;
using UnityEngine.Rendering;

namespace CGD.Impacts
{
    // A fixed number of decal quads reused oldest-first, so a long firefight never piles up
    // more than Capacity bullet holes. Plain quads rather than URP decal projectors, so they
    // work without a decal renderer feature and on any surface angle.
    public class DecalPool
    {
        // Lifted off the surface to avoid z-fighting.
        private const float SurfaceOffset = 0.01f;

        private readonly Transform _root;
        private readonly Transform[] _decals;
        private readonly MeshRenderer[] _renderers;
        private readonly Mesh _quad;
        private int _next;

        public DecalPool(Transform root, int capacity)
        {
            _root      = root;
            _decals    = new Transform[capacity];
            _renderers = new MeshRenderer[capacity];
            _quad      = BuildQuad();
        }

        public void Place(Vector3 point, Vector3 normal, Collider surface, Material material, float size)
        {
            if (_decals.Length == 0) return;

            int i = _next;
            _next = (_next + 1) % _decals.Length;

            // Destroyed along with whatever it was stuck to (a breakable crate) — make a new one.
            if (_decals[i] == null) Create(i);

            Transform decal = _decals[i];
            decal.SetParent(FollowTarget(surface), true);
            decal.SetPositionAndRotation(
                point + normal * SurfaceOffset,
                Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            SetWorldScale(decal, size);

            _renderers[i].sharedMaterial = material;
            decal.gameObject.SetActive(true);
        }

        // Decals ride along with moving things (doors, physics props); scaled parents would skew
        // the quad, so those keep the decal in world space.
        private Transform FollowTarget(Collider surface)
        {
            Transform target = surface.attachedRigidbody != null ? surface.attachedRigidbody.transform : surface.transform;
            if (target.gameObject.isStatic) return _root;

            Vector3 scale = target.lossyScale;
            bool uniform = Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.y, scale.z);
            return uniform ? target : _root;
        }

        private static void SetWorldScale(Transform decal, float size)
        {
            Vector3 parentScale = decal.parent != null ? decal.parent.lossyScale : Vector3.one;
            decal.localScale = new Vector3(size / parentScale.x, size / parentScale.y, size / parentScale.z);
        }

        private void Create(int i)
        {
            var go = new GameObject("Decal", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_root, false);
            go.GetComponent<MeshFilter>().sharedMesh = _quad;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows    = true;

            _decals[i]    = go.transform;
            _renderers[i] = renderer;
        }

        // Unit quad facing -Z, so LookRotation(-normal) makes it face out of the surface.
        private static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "DecalQuad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),   new Vector3(-0.5f, 0.5f, 0f),
            });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            mesh.SetNormals(new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
