using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Impacts;

namespace CGD.Combat
{
    // Swept-raycast projectile that advances per frame (not per FixedUpdate) for smooth
    // motion. Pool-friendly: always releases, never destroys.
    public class Projectile : MonoBehaviour
    {
        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        private static readonly Collider[] _blastBuffer = new Collider[16];
        private static readonly HashSet<IDamageable> _splashed = new();
        private static readonly HitDistanceComparer _distanceComparer = new();

        private ProjectileLaunch _launch;
        private Vector3       _velocity;
        private float         _age;
        private float         _distance;
        private Transform     _owner;
        private TrailRenderer _trail;

        // It struck something, at this point with this surface normal — for effects on the shot
        // itself (a burst where it lands). Raised just before it returns to the pool.
        public event System.Action<Vector3, Vector3> Impacted;

        private void Awake()
        {
            if (TryGetComponent<Rigidbody>(out var rb))     rb.isKinematic = true;
            if (TryGetComponent<Collider>(out var col))     col.isTrigger  = true;
            TryGetComponent(out _trail);
        }

        // Starts a flight. The projectile passes through its own shooter and any trigger
        // colliders, and stops on the first non-trigger it hits.
        public void Launch(in ProjectileLaunch launch)
        {
            _launch    = launch;
            _velocity  = launch.Velocity;
            _age       = 0f;
            _distance  = launch.DistanceTravelled;
            _owner     = OwnerBody(launch.Damage.Source.Owner);

            // A pooled instance still holds the trail of its last flight, which would
            // otherwise streak across the map from wherever that round died.
            if (_trail != null) _trail.Clear();

            FaceVelocity();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            _age += dt;
            if (_age >= _launch.Lifetime)
            {
                Despawn();
                return;
            }

            // Semi-implicit Euler: apply gravity first, then move by the new velocity.
            // Simpler than the exact ballistic formula and stable at the step sizes we use.
            _velocity += Vector3.down * (_launch.Gravity * dt);

            Vector3 step      = _velocity * dt;
            float   length    = step.magnitude;
            float   remaining = _launch.MaxDistance - _distance;

            // Stop at max range itself rather than wherever the next frame boundary lands.
            bool spent = length >= remaining;
            if (spent && length > 0f) step *= remaining / length;

            if (TrySweep(transform.position, step, out RaycastHit hit))
            {
                float scale = _launch.Falloff.Evaluate(_distance + hit.distance);
                Hitbox.ApplyHit(hit.collider, _launch.Damage.WithDamageScale(scale), hit.point);
                ImpactEvents.Report(hit.point, hit.normal, hit.collider, ImpactKind.Bullet, _launch.Damage.Source);
                if (_launch.BlastRadius > 0f) Splash(hit);
                Impacted?.Invoke(hit.point, hit.normal);
                Despawn();
                return;
            }

            transform.position += step;
            _distance          += spent ? remaining : length;
            FaceVelocity();

            if (spent) Despawn();
        }

        // Hurts everyone else within the blast radius of the impact, unless scenery shields them:
        // a round that lands beside its target still counts.
        private void Splash(RaycastHit hit)
        {
            Vector3 center = hit.point + hit.normal * 0.1f;
            int count = Physics.OverlapSphereNonAlloc(center, _launch.BlastRadius, _blastBuffer,
                _launch.HitMask, QueryTriggerInteraction.Ignore);

            _splashed.Clear();
            IDamageable direct = Hitbox.FindDamageable(hit.collider);
            if (direct != null) _splashed.Add(direct);

            for (int i = 0; i < count; i++)
            {
                Collider col = _blastBuffer[i];
                if (_owner != null && col.transform.IsChildOf(_owner)) continue;

                IDamageable target = Hitbox.FindDamageable(col);
                if (target == null || !_splashed.Add(target)) continue;
                if (Physics.Linecast(center, col.ClosestPoint(center), out RaycastHit block, _launch.HitMask, QueryTriggerInteraction.Ignore)
                    && Hitbox.FindDamageable(block.collider) != target) continue;

                target.TakeDamage(_launch.Damage);
            }
        }

        private void Despawn() => PrefabPool.Release(gameObject);

        // The shooter's whole character (its HealthManager and everything under it), so a
        // round fired from a weapon or hand never hits its own body. Not the hierarchy root:
        // characters grouped under a shared parent (a scene's Enemies object) must still be
        // able to hit each other.
        private static Transform OwnerBody(GameObject owner)
        {
            if (owner == null) return null;
            var health = owner.GetComponentInParent<HealthManager>();
            return health != null ? health.transform : owner.transform;
        }

        private void FaceVelocity()
        {
            if (_velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_velocity);
        }

        // Casts along the intended step and returns the first hit that is not the shooter,
        // ignoring trigger volumes so pickups and interaction zones don't stop the round.
        private bool TrySweep(Vector3 start, Vector3 step, out RaycastHit hit)
        {
            hit = default;
            float distance = step.magnitude;
            if (distance <= 0f) return false;

            int count = _launch.HitRadius > 0f
                ? Physics.SphereCastNonAlloc(start, _launch.HitRadius, step / distance, _hitBuffer, distance, _launch.HitMask, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(start, step / distance, _hitBuffer,
                distance, _launch.HitMask, QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            System.Array.Sort(_hitBuffer, 0, count, _distanceComparer);

            for (int i = 0; i < count; i++)
            {
                if (_owner != null && _hitBuffer[i].collider.transform.IsChildOf(_owner))
                    continue;
                hit = _hitBuffer[i];
                // A sphere that starts inside something reports no point: use where it was.
                if (hit.distance <= 0f)
                {
                    hit.point  = start;
                    hit.normal = -step / distance;
                }
                return true;
            }
            return false;
        }

        private class HitDistanceComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
