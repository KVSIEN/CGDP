using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

namespace CGD.Stealth
{
    // Cover a character can hide in: a bush, tall grass, a smoke cloud. Needs a Box,
    // Sphere or Capsule collider (set to trigger) giving its shape — a box's rectangle is the
    // patch of ground characters remember it by (Footprint), so size it to the foliage. Smoke also blocks
    // sight lines that pass through it. Active zones register themselves, so a smoke
    // grenade's pooled cloud works the moment it spawns.
    [RequireComponent(typeof(Collider))]
    public class ConcealmentZone : MonoBehaviour
    {
        private static readonly List<ConcealmentZone> _active = new();

        [SerializeField] private ConcealmentKind _kind = ConcealmentKind.Bush;
        [Tooltip("Enemies can't see through it (smoke)")]
        [SerializeField] private bool _blocksSight;
        [Tooltip("Only hides characters that crouch (tall grass, low hedges)")]
        [SerializeField] private bool _requiresCrouch;

        private Collider _collider;

        public static IReadOnlyList<ConcealmentZone> Active => _active;

        public ConcealmentKind Kind           => _kind;
        public bool            BlocksSight    => _blocksSight;
        public bool            RequiresCrouch => _requiresCrouch;
        // A wall to the eye: nobody outside sees through it or into it. Smoke, and any bush that
        // hides a standing character (crouch-only grass is too low to).
        public bool            BlocksView     => _blocksSight || (_kind == ConcealmentKind.Bush && !_requiresCrouch);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _active.Clear();

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            // Walked through, never around: keep it out of any NavMesh built after it appears
            // (a generated level bakes once its props are placed).
            if (!TryGetComponent(out NavMeshModifier _))
                gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        private void OnEnable()  => _active.Add(this);
        private void OnDisable() => _active.Remove(this);

        // The middle of its footprint, on the ground: where a searcher walks to look inside.
        public Vector3 GroundCenter
        {
            get
            {
                Bounds bounds = _collider.bounds;
                return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
        }

        // The whole patch of ground it covers (a box collider's own rectangle, other shapes by their
        // bounds), recomputed on each call so a zone that moves stays right.
        public BushFootprint Footprint
        {
            get
            {
                Bounds bounds = _collider.bounds;
                var ground = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                if (_collider is not BoxCollider box)
                    return new BushFootprint(ground, new Vector2(bounds.extents.x, bounds.extents.z), Vector3.right);

                Transform t = box.transform;
                Vector3 scale = t.lossyScale;
                Vector3 centre = t.TransformPoint(box.center);
                ground = new Vector3(centre.x, bounds.min.y, centre.z);
                var half = new Vector2(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.z * scale.z)) * 0.5f;
                return new BushFootprint(ground, half, t.right);
            }
        }

        // Primitive colliders only: ClosestPoint returns the point itself when it's inside.
        public bool Contains(Vector3 point) => (_collider.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;

        // Any cover containing the point (optionally one that hides a crouching/standing character).
        public static bool AnyContains(Vector3 point, bool crouching)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                ConcealmentZone zone = _active[i];
                if (zone._requiresCrouch && !crouching) continue;
                if (zone.Contains(point)) return true;
            }
            return false;
        }

        // Both points inside the same piece of cover.
        public static bool Share(Vector3 a, Vector3 b)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Contains(a) && _active[i].Contains(b)) return true;
            return false;
        }

        // A sight-blocking zone (smoke, a bush) lies between the points and doesn't contain either
        // end: from inside one it can be seen out of and into, from outside never through.
        public static bool BlocksLine(Vector3 from, Vector3 to)
        {
            Vector3 delta    = to - from;
            float   distance = delta.magnitude;
            if (distance < 0.001f) return false;

            var ray = new Ray(from, delta / distance);
            for (int i = 0; i < _active.Count; i++)
            {
                ConcealmentZone zone = _active[i];
                if (!zone.BlocksView || zone.Contains(from) || zone.Contains(to)) continue;
                if (zone._collider.bounds.IntersectRay(ray, out float hit) && hit <= distance) return true;
            }
            return false;
        }
    }
}
