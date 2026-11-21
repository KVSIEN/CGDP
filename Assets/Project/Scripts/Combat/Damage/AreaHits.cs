using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // What a blast or burst around a point reaches: every damageable inside the sphere once, however
    // many hitboxes it has there, through its collider nearest the centre. The raw colliders stay
    // readable for pushing loose bodies.
    //
    // With a cover mask, only a target wholly behind cover is spared. Its body (every hitbox of it
    // inside the sphere) is looked at from the centre at up to five points: the nearest point,
    // the middle, the top and the two sides as seen from the centre. One clear line is enough,
    // so a head over a low wall or a shoulder round a corner still gets caught. Only scenery is
    // cover: other characters, breakables and loose physics objects never shield anyone. A wall
    // within CoverGrace of the point aimed at doesn't count either, so a body pressed against a
    // wall or standing in a doorway's edge is still reached.
    //
    // One instance per caller, reused every query, so a blast allocates nothing.
    public sealed class AreaHits
    {
        private const float CoverGrace = 0.25f;
        // Side and top points sit this share of the way out from the body's middle, inside its outline.
        private const float EdgeReach = 0.8f;

        private readonly Collider[] _colliders;
        private readonly RaycastHit[] _rays = new RaycastHit[16];
        private readonly List<AreaHit> _hits = new();
        // Per hit: the bounds of every collider of it inside the sphere.
        private readonly List<Bounds> _bodies = new();
        private readonly Dictionary<IDamageable, int> _indexOf = new();

        private Vector3   _center;
        private LayerMask _coverMask;

        public AreaHits(int capacity = 64) => _colliders = new Collider[capacity];

        public IReadOnlyList<AreaHit> Hits => _hits;

        // Every collider the last query overlapped, damageable or not, covered or not.
        public int ColliderCount { get; private set; }
        public Collider ColliderAt(int index) => _colliders[index];

        // coverMask: what blocks the blast (nothing = it reaches through walls).
        // ignore: a body never caught, with everything under it (the blast's own owner).
        public void Gather(Vector3 center, float radius, LayerMask mask, LayerMask coverMask = default, Transform ignore = null)
        {
            _hits.Clear();
            _bodies.Clear();
            _indexOf.Clear();
            _center    = center;
            _coverMask = coverMask;
            ColliderCount = Physics.OverlapSphereNonAlloc(center, radius, _colliders, mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < ColliderCount; i++)
            {
                Collider collider = _colliders[i];
                if (ignore != null && collider.transform.IsChildOf(ignore)) continue;

                IDamageable target = Hitbox.FindDamageable(collider);
                if (target == null) continue;

                Vector3 point = collider.ClosestPoint(center);
                float distance = Vector3.Distance(center, point);
                if (distance > radius) continue;

                if (_indexOf.TryGetValue(target, out int index))
                {
                    Bounds body = _bodies[index];
                    body.Encapsulate(collider.bounds);
                    _bodies[index] = body;
                    if (distance < _hits[index].Distance) _hits[index] = new AreaHit(target, collider, point, distance);
                    continue;
                }

                _indexOf[target] = _hits.Count;
                _hits.Add(new AreaHit(target, collider, point, distance));
                _bodies.Add(collider.bounds);
            }

            if (coverMask.value == 0) return;

            for (int i = _hits.Count - 1; i >= 0; i--)
            {
                if (IsExposed(_hits[i].Point, _bodies[i])) continue;
                _hits.RemoveAt(i);
                _bodies.RemoveAt(i);
            }
        }

        // Whether the last query's blast reaches this collider past its cover (always, without a
        // cover mask): for pushing the loose bodies it overlapped.
        public bool Reaches(Collider collider) =>
            _coverMask.value == 0 || IsExposed(collider.ClosestPoint(_center), collider.bounds);

        // Nearest point first: most targets in the open are settled by that one line.
        private bool IsExposed(Vector3 nearest, Bounds body)
        {
            if (IsClear(nearest) || IsClear(body.center)) return true;

            Vector3 toBody = body.center - _center;
            toBody.y = 0f;
            Vector3 side = toBody.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, toBody).normalized : Vector3.right;
            Vector3 extents = body.extents;
            float halfWidth = (Mathf.Abs(side.x) * extents.x + Mathf.Abs(side.z) * extents.z) * EdgeReach;

            return IsClear(body.center + Vector3.up * (extents.y * EdgeReach))
                || IsClear(body.center + side * halfWidth)
                || IsClear(body.center - side * halfWidth);
        }

        private bool IsClear(Vector3 point)
        {
            Vector3 line = point - _center;
            float length = line.magnitude;
            if (length <= CoverGrace) return true;

            int count = Physics.RaycastNonAlloc(_center, line / length, _rays, length - CoverGrace, _coverMask,
                                                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (IsCover(_rays[i].collider)) return false;
            return true;
        }

        private static bool IsCover(Collider collider)
        {
            if (Hitbox.FindDamageable(collider) != null) return false;
            Rigidbody body = collider.attachedRigidbody;
            return body == null || body.isKinematic;
        }
    }
}
