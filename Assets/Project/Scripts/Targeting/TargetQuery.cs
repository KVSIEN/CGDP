using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Targeting
{
    // Physics-backed target searches shared by abilities, AI and effects. Every query
    // resolves colliders to their owning character (hitboxes collapse onto one
    // HealthManager), applies a TargetFilter, and writes into a caller-owned list, so
    // nothing allocates per call.
    public static class TargetQuery
    {
        private const int   BufferSize        = 64;
        private const float GroundProbeHeight = 50f;

        private static readonly Collider[]   _overlapBuffer = new Collider[BufferSize];
        private static readonly RaycastHit[] _rayBuffer     = new RaycastHit[16];
        private static readonly DistanceComparer   _byDistance    = new();
        private static readonly HitDistanceComparer _hitsByDistance = new();

        // Accepted characters with any collider inside the sphere, each once. Line of
        // sight (when the filter asks for it) is checked from the centre.
        public static int Sphere(Vector3 center, float radius, in TargetFilter filter, List<HealthManager> results) =>
            Collect(center, radius, Vector3.forward, -1f, filter, results);

        // Accepted characters within range and inside a cone of the given full angle.
        public static int Cone(Vector3 origin, Vector3 forward, float range, float angle,
                               in TargetFilter filter, List<HealthManager> results)
        {
            float cosHalfAngle = Mathf.Cos(Mathf.Clamp(angle, 0f, 360f) * 0.5f * Mathf.Deg2Rad);
            return Collect(origin, range, forward.normalized, cosHalfAngle, filter, results);
        }

        // The closest accepted character within radius, or null.
        public static HealthManager Nearest(Vector3 origin, float radius, in TargetFilter filter, List<HealthManager> scratch)
        {
            if (Sphere(origin, radius, filter, scratch) == 0) return null;

            SortByDistance(scratch, origin);
            return scratch[0];
        }

        // First solid thing along the ray, skipping the filter's Self. Returns false when
        // nothing was hit; target is the character hit if the filter accepts it.
        public static bool Raycast(Vector3 origin, Vector3 direction, float range, in TargetFilter filter,
                                   out HealthManager target, out RaycastHit hit)
        {
            target = null;
            hit    = default;

            int count = Physics.RaycastNonAlloc(origin, direction.normalized, _rayBuffer, range,
                filter.Mask, QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            System.Array.Sort(_rayBuffer, 0, count, _hitsByDistance);

            for (int i = 0; i < count; i++)
            {
                HealthManager owner = OwnerOf(_rayBuffer[i].collider);
                if (owner != null && owner == filter.Self) continue;

                hit = _rayBuffer[i];
                if (filter.Accepts(owner)) target = owner;
                return true;
            }
            return false;
        }

        // Where an aimed ray meets the ground: the surface it hits within range, or the
        // ground below the ray's end point when it hits nothing (aiming at the sky).
        public static bool GroundPoint(Vector3 origin, Vector3 direction, float range, LayerMask groundMask, out Vector3 point)
        {
            direction = direction.normalized;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, groundMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }

            Vector3 end = origin + direction * range;
            if (Physics.Raycast(end + Vector3.up * 0.5f, Vector3.down, out hit, GroundProbeHeight, groundMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }

            point = end;
            return false;
        }

        public static void SortByDistance(List<HealthManager> targets, Vector3 origin)
        {
            _byDistance.Origin = origin;
            targets.Sort(_byDistance);
        }

        public static bool HasLineOfSight(Vector3 from, Vector3 to, HealthManager target, LayerMask obstacleMask)
        {
            Vector3 delta    = to - from;
            float   distance = delta.magnitude;
            if (distance < 0.001f) return true;

            if (!Physics.Raycast(from, delta / distance, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
                return true;

            // Running into the target's own collider on the way in still counts as seeing it.
            return OwnerOf(hit.collider) == target;
        }

        private static int Collect(Vector3 origin, float radius, Vector3 forward, float cosHalfAngle,
                                   in TargetFilter filter, List<HealthManager> results)
        {
            results.Clear();

            int count = Physics.OverlapSphereNonAlloc(origin, radius, _overlapBuffer, filter.Mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider      col       = _overlapBuffer[i];
                HealthManager candidate = OwnerOf(col);
                if (!filter.Accepts(candidate) || results.Contains(candidate)) continue;

                // The self is always "in view"; everyone else is tested at the collider that
                // overlapped, so a visible head counts even when the feet are behind cover.
                if (candidate != filter.Self && !IsVisible(origin, col, candidate, forward, cosHalfAngle, filter)) continue;

                results.Add(candidate);
            }

            return results.Count;
        }

        private static bool IsVisible(Vector3 origin, Collider col, HealthManager candidate, Vector3 forward,
                                      float cosHalfAngle, in TargetFilter filter)
        {
            Vector3 point = col.bounds.center;

            if (cosHalfAngle > -1f)
            {
                Vector3 toPoint = point - origin;
                float   sqrMag  = toPoint.sqrMagnitude;
                if (sqrMag > 0.0001f && Vector3.Dot(forward, toPoint) < cosHalfAngle * Mathf.Sqrt(sqrMag)) return false;
            }

            return !filter.RequireLineOfSight || HasLineOfSight(origin, point, candidate, filter.ObstacleMask);
        }

        private static HealthManager OwnerOf(Collider col) => Hitbox.FindDamageable(col) as HealthManager;

        private sealed class DistanceComparer : IComparer<HealthManager>
        {
            public Vector3 Origin;

            public int Compare(HealthManager a, HealthManager b) =>
                (a.transform.position - Origin).sqrMagnitude.CompareTo((b.transform.position - Origin).sqrMagnitude);
        }

        private sealed class HitDistanceComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
