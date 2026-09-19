using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Traveling damage carrier. Advances under gravity each physics step and
    // swept-raycasts between the previous and next position, so fast projectiles
    // can never tunnel past thin colliders between frames. Pool-friendly: never
    // Destroys itself, always Release.
    //
    // Existing prefabs still carry a kinematic Rigidbody + trigger Collider from
    // the old OnTrigger-based flow; the Awake safety net enforces those settings
    // so a prefab left over from that setup can't fall or shove things around.
    // New prefabs don't need either component.
    public class Projectile : MonoBehaviour
    {
        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        private static readonly HitDistanceComparer _distanceComparer = new();

        private DamageInfo _hit;
        private Vector3    _velocity;
        private float      _gravity;
        private float      _lifetime;
        private float      _age;
        private Transform  _ownerRoot;

        private void Awake()
        {
            if (TryGetComponent<Rigidbody>(out var rb))     rb.isKinematic = true;
            if (TryGetComponent<Collider>(out var col))     col.isTrigger  = true;
        }

        // Launch the projectile. `velocity` is metres per second (direction × speed
        // at spawn) and `gravity` is downward acceleration in m/s² (0 = perfectly
        // straight flight). The projectile passes through its own shooter and any
        // trigger colliders, and stops on the first non-trigger it hits.
        public void Launch(DamageInfo hit, Vector3 velocity, float gravity, float lifetime)
        {
            _hit       = hit;
            _velocity  = velocity;
            _gravity   = gravity;
            _lifetime  = lifetime;
            _age       = 0f;
            _ownerRoot = hit.Source.Owner != null ? hit.Source.Owner.transform.root : null;

            FaceVelocity();
        }

        private void FixedUpdate()
        {
            _age += Time.fixedDeltaTime;
            if (_age >= _lifetime)
            {
                PrefabPool.Release(gameObject);
                return;
            }

            // Semi-implicit Euler: apply gravity first, then move by the new velocity.
            // Simpler than the exact ballistic formula and stable at the step sizes we use.
            _velocity += Vector3.down * (_gravity * Time.fixedDeltaTime);
            Vector3 step = _velocity * Time.fixedDeltaTime;

            if (TrySweep(transform.position, step, out RaycastHit hit))
            {
                Hitbox.ApplyHit(hit.collider, _hit, hit.point);
                PrefabPool.Release(gameObject);
                return;
            }

            transform.position += step;
            FaceVelocity();
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

            int count = Physics.RaycastNonAlloc(start, step / distance, _hitBuffer,
                distance, ~0, QueryTriggerInteraction.Ignore);
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
