using System.Collections.Generic;
using UnityEngine;

namespace CGD.Stealth
{
    // Cover a character can hide in: a bush, tall grass, a smoke cloud. Needs a Box,
    // Sphere or Capsule collider (set to trigger) giving its shape. Smoke also blocks
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _active.Clear();

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnEnable()  => _active.Add(this);
        private void OnDisable() => _active.Remove(this);

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

        // A sight-blocking zone lies between the points and doesn't contain either end
        // (standing inside smoke is handled by the shared-cover rule instead).
        public static bool BlocksLine(Vector3 from, Vector3 to)
        {
            Vector3 delta    = to - from;
            float   distance = delta.magnitude;
            if (distance < 0.001f) return false;

            var ray = new Ray(from, delta / distance);
            for (int i = 0; i < _active.Count; i++)
            {
                ConcealmentZone zone = _active[i];
                if (!zone._blocksSight || zone.Contains(from) || zone.Contains(to)) continue;
                if (zone._collider.bounds.IntersectRay(ray, out float hit) && hit <= distance) return true;
            }
            return false;
        }
    }
}
