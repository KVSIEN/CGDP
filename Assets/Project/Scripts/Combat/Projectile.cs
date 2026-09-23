using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Swept-raycast projectile that advances per frame (not per FixedUpdate) for smooth
    // motion. Pool-friendly: always releases, never destroys.
    public class Projectile : MonoBehaviour
    {
        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        private static readonly HitDistanceComparer _distanceComparer = new();

        private ProjectileLaunch _launch;
        private Vector3       _velocity;
        private float         _age;
        private float         _distance;
        private Transform     _ownerRoot;
        private TrailRenderer _trail;

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
            _ownerRoot = launch.Damage.Source.Owner != null
                       ? launch.Damage.Source.Owner.transform.root
                       : null;

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
                Despawn();
                return;
            }

            transform.position += step;
            _distance          += spent ? remaining : length;
            FaceVelocity();

            if (spent) Despawn();
        }

        private void Despawn() => PrefabPool.Release(gameObject);

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

            int count = Physics.RaycastNonAlloc(start, step / distance, _hitBuffer,
                distance, _launch.HitMask, QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            System.Array.Sort(_hitBuffer, 0, count, _distanceComparer);

            for (int i = 0; i < count; i++)
            {
                if (_ownerRoot != null && _hitBuffer[i].collider.transform.root == _ownerRoot)
                    continue;
                hit = _hitBuffer[i];
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
