using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // What a blast or burst around a point reaches: every damageable inside the sphere once, however
    // many hitboxes it has there, through its collider nearest the centre. With a cover mask, a
    // collider that scenery hides from the centre doesn't count, so a target is only skipped when
    // all of it is behind cover. The raw colliders stay readable for pushing loose bodies.
    // One instance per caller, reused every query, so a blast allocates nothing.
    public sealed class AreaHits
    {
        private readonly Collider[] _colliders;
        private readonly List<AreaHit> _hits = new();
        private readonly Dictionary<IDamageable, int> _indexOf = new();

        public AreaHits(int capacity = 64) => _colliders = new Collider[capacity];

        public IReadOnlyList<AreaHit> Hits => _hits;

        // Every collider the last query overlapped, damageable or not.
        public int ColliderCount { get; private set; }
        public Collider ColliderAt(int index) => _colliders[index];

        // coverMask: what blocks the blast (nothing = it reaches through walls).
        // ignore: a body never caught, with everything under it (the blast's own owner).
        public void Gather(Vector3 center, float radius, LayerMask mask, LayerMask coverMask = default, Transform ignore = null)
        {
            _hits.Clear();
            _indexOf.Clear();
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

                bool seen = _indexOf.TryGetValue(target, out int index);
                if (seen && _hits[index].Distance <= distance) continue;
                if (coverMask.value != 0 && IsCovered(center, point, target, coverMask)) continue;

                var hit = new AreaHit(target, collider, point, distance);
                if (seen)
                {
                    _hits[index] = hit;
                    continue;
                }
                _indexOf[target] = _hits.Count;
                _hits.Add(hit);
            }
        }

        // Something solid between the centre and the point that isn't the target itself.
        private static bool IsCovered(Vector3 center, Vector3 point, IDamageable target, LayerMask coverMask) =>
            Physics.Linecast(center, point, out RaycastHit block, coverMask, QueryTriggerInteraction.Ignore)
            && Hitbox.FindDamageable(block.collider) != target;
    }
}
