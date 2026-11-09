using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // An enemy lobbing a grenade (any GrenadeData) onto where it believes its target is —
    // to flush it out of cover it can't see past. A short wind-up facing the spot, then the
    // throw along a high arc; if no arc within the grenade's throw speeds lands there, it
    // doesn't throw. Grenades never hurt the thrower's own side.
    internal sealed class GrenadeThrower
    {
        private const float WindUp      = 0.5f;
        private const float HandHeight  = 1.6f;
        private const float ArcAngle    = 40f;
        // Flight simulation for the clear-arc check.
        private const float ArcStep      = 0.04f;
        private const int   ArcMaxSteps  = 150;
        // A grenade stopping this close to the target counts as landing on it.
        private const float LandingSlack = 3f;

        private readonly EnemyAI      _ai;
        private readonly DamageSource _source;
        private readonly LayerMask    _obstacleMask;

        private GrenadeData _pending;
        private Vector3     _aimAt;
        private float       _throwAt;

        public GrenadeThrower(EnemyAI ai, DamageSource source, LayerMask obstacleMask)
        {
            _ai     = ai;
            _source = source;
            _obstacleMask = obstacleMask;
        }

        public bool IsThrowing => _pending != null;

        public bool CanReach(GrenadeData grenade, Vector3 target) =>
            grenade != null && grenade.GrenadePrefab != null && TrySolveClear(grenade, target, out _);

        public bool TryBegin(GrenadeData grenade, Vector3 target)
        {
            if (IsThrowing || !CanReach(grenade, target)) return false;

            _pending = grenade;
            _aimAt   = target;
            _throwAt = Time.time + WindUp;
            return true;
        }

        // True once thrown (or given up).
        public bool Tick(float deltaTime)
        {
            if (_pending == null) return true;

            _ai.Agent.isStopped = true;
            _ai.FaceTowards(_aimAt, deltaTime);
            if (Time.time < _throwAt) return false;

            Throw(_pending, _aimAt);
            _pending = null;
            return true;
        }

        public void Cancel() => _pending = null;

        private void Throw(GrenadeData data, Vector3 target)
        {
            if (!TrySolveClear(data, target, out Vector3 velocity)) return;

            GameObject grenade = PrefabPool.Spawn(data.GrenadePrefab, HandPosition, Quaternion.identity);
            if (grenade.TryGetComponent(out Rigidbody body))
            {
                body.linearVelocity  = velocity;
                body.angularVelocity = Vector3.zero;
            }
            if (grenade.TryGetComponent(out Collider collider) && _ai.TryGetComponent(out Collider own))
                Physics.IgnoreCollision(collider, own);
            if (grenade.TryGetComponent(out Grenade fuse)) fuse.Init(data, _source);

            data.ThrowSound.TryPlay(_ai.transform.position);
            _ai.NotifyThrew();
        }

        // A throw that lands on the target and doesn't hit a wall, ceiling or corner on the way:
        // a lob at a wall in front of the thrower would drop the grenade at its own feet.
        private bool TrySolveClear(GrenadeData data, Vector3 target, out Vector3 velocity) =>
            Solve(data, target, out velocity) && IsHandClear() && IsArcClear(data, target, velocity);

        private bool IsHandClear() =>
            !Physics.Linecast(_ai.transform.position + Vector3.up, HandPosition, _obstacleMask, QueryTriggerInteraction.Ignore);

        private bool IsArcClear(GrenadeData data, Vector3 target, Vector3 velocity)
        {
            Vector3 position = HandPosition;
            Vector3 gravity  = data.Gravity;
            for (int i = 0; i < ArcMaxSteps; i++)
            {
                Vector3 next = position + velocity * ArcStep + 0.5f * ArcStep * ArcStep * gravity;
                velocity += gravity * ArcStep;

                if (Physics.Linecast(position, next, out RaycastHit hit, _obstacleMask, QueryTriggerInteraction.Ignore) &&
                    Hitbox.FindDamageable(hit.collider) == null)
                    return (hit.point - target).sqrMagnitude <= LandingSlack * LandingSlack;

                if (next.y < target.y && velocity.y < 0f) return true;
                position = next;
            }
            return true;
        }

        private bool Solve(GrenadeData data, Vector3 target, out Vector3 velocity) =>
            ThrowArc.TrySolve(HandPosition, target, ArcAngle, data.MinThrowSpeed, data.MaxThrowSpeed, -data.Gravity.y, out velocity);

        private Vector3 HandPosition => _ai.transform.position + Vector3.up * HandHeight + _ai.transform.forward * 0.4f;
    }
}
