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

        private readonly EnemyAI      _ai;
        private readonly DamageSource _source;

        private GrenadeData _pending;
        private Vector3     _aimAt;
        private float       _throwAt;

        public GrenadeThrower(EnemyAI ai, DamageSource source)
        {
            _ai     = ai;
            _source = source;
        }

        public bool IsThrowing => _pending != null;

        public bool CanReach(GrenadeData grenade, Vector3 target) =>
            grenade != null && grenade.GrenadePrefab != null && Solve(grenade, target, out _);

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
            if (!Solve(data, target, out Vector3 velocity)) return;

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

        private bool Solve(GrenadeData data, Vector3 target, out Vector3 velocity) =>
            ThrowArc.TrySolve(HandPosition, target, ArcAngle, data.MinThrowSpeed, data.MaxThrowSpeed, -data.Gravity.y, out velocity);

        private Vector3 HandPosition => _ai.transform.position + Vector3.up * HandHeight + _ai.transform.forward * 0.4f;
    }
}
