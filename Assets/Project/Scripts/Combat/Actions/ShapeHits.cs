using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // The hit shapes melee swings and action timelines strike with — a cast, a fan of casts, a
    // sphere, a box — and what each touched. Every query skips the attacker's own body, keeps
    // only damageables, and lists each target once between Clear calls (and never one already
    // in `alreadyHit`, which the query adds it to, so a swing hits a target once across ticks).
    // Damage is the caller's: a point hit through Hitbox.ApplyHit, or an area hit.
    // One instance per caller, reused every tick, so a strike allocates nothing.
    public sealed class ShapeHits
    {
        private readonly RaycastHit[] _casts;
        private readonly Collider[]   _overlaps;
        private readonly List<ShapeHit> _hits = new();
        private readonly HashSet<IDamageable> _listed = new();

        public ShapeHits(int capacity = 16)
        {
            _casts    = new RaycastHit[capacity];
            _overlaps = new Collider[capacity];
        }

        public IReadOnlyList<ShapeHit> Hits => _hits;

        public void Clear()
        {
            _hits.Clear();
            _listed.Clear();
        }

        // The direction of ray `index` of `rays` spread evenly across `arcDeg` around `up`.
        public static Vector3 FanDirection(Vector3 forward, Vector3 up, float arcDeg, int rays, int index)
        {
            rays = Mathf.Max(rays, 2);
            float angle = Mathf.Lerp(-arcDeg * 0.5f, arcDeg * 0.5f, (float)index / (rays - 1));
            return Quaternion.AngleAxis(angle, up) * forward;
        }

        // A sphere (a ray when radius is 0) swept `range` along `direction`. `source` is the
        // attacker's root: never hit, and where a contact that starts inside a collider is measured from.
        public void Cast(Vector3 origin, Vector3 direction, float radius, float range, LayerMask mask,
                         Transform source, HashSet<IDamageable> alreadyHit = null)
        {
            int count = radius > 0f
                ? Physics.SphereCastNonAlloc(origin, radius, direction, _casts, range, mask, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(origin, direction, _casts, range, mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider collider = _casts[i].collider;
                if (!TryList(collider, source, alreadyHit, out IDamageable target)) continue;

                Vector3 point  = _casts[i].point;
                Vector3 normal = _casts[i].normal;
                // A cast that starts overlapping reports zero: take the collider's surface nearest the attacker.
                if (point == Vector3.zero)
                {
                    point  = collider.ClosestPoint(source.position);
                    normal = source.position - point;
                }
                _hits.Add(new ShapeHit(collider, target, point, normal));
            }
        }

        // `rays` casts spread evenly across `arcDeg` around `up` — a slash.
        public void Fan(Vector3 origin, Vector3 forward, Vector3 up, float arcDeg, int rays, float radius, float range,
                        LayerMask mask, Transform source, HashSet<IDamageable> alreadyHit = null)
        {
            int count = Mathf.Max(rays, 2);
            for (int i = 0; i < count; i++)
                Cast(origin, FanDirection(forward, up, arcDeg, count, i), radius, range, mask, source, alreadyHit);
        }

        // Everything inside the sphere; each contact is its collider's surface nearest `pointFrom`.
        public void Sphere(Vector3 center, float radius, Vector3 pointFrom, LayerMask mask,
                           Transform source, HashSet<IDamageable> alreadyHit = null)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, _overlaps, mask, QueryTriggerInteraction.Ignore);
            AddOverlaps(count, pointFrom, source, alreadyHit);
        }

        // Everything inside the box; each contact is its collider's surface nearest `pointFrom`.
        public void Box(Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 pointFrom, LayerMask mask,
                        Transform source, HashSet<IDamageable> alreadyHit = null)
        {
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlaps, orientation, mask, QueryTriggerInteraction.Ignore);
            AddOverlaps(count, pointFrom, source, alreadyHit);
        }

        private void AddOverlaps(int count, Vector3 pointFrom, Transform source, HashSet<IDamageable> alreadyHit)
        {
            for (int i = 0; i < count; i++)
            {
                Collider collider = _overlaps[i];
                if (!TryList(collider, source, alreadyHit, out IDamageable target)) continue;

                Vector3 point = collider.ClosestPoint(pointFrom);
                _hits.Add(new ShapeHit(collider, target, point, pointFrom - point));
            }
        }

        private bool TryList(Collider collider, Transform source, HashSet<IDamageable> alreadyHit, out IDamageable target)
        {
            target = null;
            if (source != null && collider.transform.root == source) return false;

            target = Hitbox.FindDamageable(collider);
            if (target == null || !_listed.Add(target)) return false;
            return alreadyHit == null || alreadyHit.Add(target);
        }
    }
}
