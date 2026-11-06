using UnityEngine;
using UnityEngine.AI;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Impacts;
using CGD.Weapons;

namespace CGD.Enemies
{
    // How a ranged enemy fights: holding its preferred distance (approach, back off, strafe
    // while in the band) and firing hitscan bursts. Used by the ranged behavior tree nodes
    // (Hold Range, Shoot Burst, Suppress).
    internal sealed class EnemyRangedCombat
    {
        private const float EyeHeight = 1.5f;
        // The band around the preferred range that counts as "in position".
        private const float RangeTolerance = 0.15f;

        private readonly EnemyAI      _ai;
        private readonly DamageSource _source;
        private readonly LayerMask    _obstacleMask;

        private CooldownTimer _fireCooldown;
        private int   _burstRemaining;
        private float _burstTimer;
        private float _strafeTimer;
        private int   _strafeDir = 1;
        private bool  _hasShooterToken;

        public EnemyRangedCombat(EnemyAI ai, DamageSource source, LayerMask obstacleMask)
        {
            _ai           = ai;
            _source       = source;
            _obstacleMask = obstacleMask;
        }

        public bool IsBursting => _burstRemaining > 0;
        public bool CanFire    => !IsBursting && _fireCooldown.IsReady;

        // Called every unstunned frame by EnemyAI, so cooldowns and bursts carry on whoever is deciding.
        public void Tick(float deltaTime)
        {
            _fireCooldown.Tick(deltaTime);
            TickBurst(deltaTime);

            // Ready to fire again: let a waiting ally have the turn if it wants it.
            if (_hasShooterToken && !IsBursting && _fireCooldown.IsReady) ReleaseToken();
        }

        public void ResetSpacing() => _strafeTimer = _ai.Data.StrafeInterval;

        // Moves toward the preferred distance from the target. True while within the band
        // (strafing), which is where the enemy should shoot from.
        public bool HoldPreferredRange(Vector3 targetPos, float deltaTime)
        {
            EnemyData data     = _ai.Data;
            Vector3   toTarget = targetPos - _ai.transform.position;
            float     dist     = toTarget.magnitude;
            float     tolerance = data.PreferredRange * RangeTolerance;

            if (dist > data.PreferredRange + tolerance)
            {
                _ai.Agent.isStopped = false;
                _ai.Agent.SetDestination(targetPos);
                return false;
            }

            Vector3 dir = dist > 0.001f ? toTarget / dist : _ai.transform.forward;
            if (dist < data.PreferredRange - tolerance)
            {
                Retreat(dir, data.PreferredRange - dist + tolerance, tolerance);
                return false;
            }

            Strafe(dir, deltaTime);
            return true;
        }

        public bool HasLineOfSight(Vector3 targetPos)
        {
            Vector3 eyePos   = _ai.transform.position + Vector3.up * EyeHeight;
            Vector3 toTarget = targetPos + Vector3.up - eyePos;
            float   dist     = toTarget.magnitude;

            return dist < 0.001f || !Physics.Raycast(eyePos, toTarget / dist, dist, _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        // Fires the first shot of a burst now; the rest follow every BurstInterval.
        public bool TryStartBurst()
        {
            if (!CanFire) return false;

            // Only a few enemies fire at one target at once (EnemyData.MaxShooters).
            var target = _ai.Perception.Target;
            if (target == null) return false;
            _hasShooterToken = AttackTokens.TryAcquire(AttackTokenKind.Ranged, target, _ai, _ai.Data.MaxShooters);
            if (!_hasShooterToken) return false;

            EnemyData data = _ai.Data;
            _fireCooldown.Start(data.AttackCooldown);
            _burstRemaining = Mathf.Max(1, data.BurstCount);
            FireOneShot();
            _burstRemaining--;
            _burstTimer = data.BurstInterval;
            return true;
        }

        public void CancelBurst()
        {
            _burstRemaining = 0;
            ReleaseToken();
        }

        public void Reset()
        {
            CancelBurst();
            _fireCooldown.Reset();
        }

        private void ReleaseToken()
        {
            if (_hasShooterToken) AttackTokens.Release(AttackTokenKind.Ranged, _ai);
            _hasShooterToken = false;
        }

        private void TickBurst(float deltaTime)
        {
            if (_burstRemaining <= 0) return;

            _burstTimer -= deltaTime;
            if (_burstTimer > 0f) return;

            FireOneShot();
            _burstRemaining--;
            _burstTimer = _ai.Data.BurstInterval;
        }

        private void Retreat(Vector3 toTargetDir, float distance, float tolerance)
        {
            Vector3 retreatTarget = _ai.transform.position - toTargetDir * distance;
            if (NavMesh.SamplePosition(retreatTarget, out var hit, tolerance * 2f, NavMesh.AllAreas))
            {
                _ai.Agent.isStopped = false;
                _ai.Agent.SetDestination(hit.position);
            }
        }

        private void Strafe(Vector3 toTargetDir, float deltaTime)
        {
            EnemyData data = _ai.Data;
            _strafeTimer -= deltaTime;

            if (_strafeTimer <= 0f)
            {
                _strafeTimer = data.StrafeInterval;
                _strafeDir   = Random.value > 0.5f ? 1 : -1;
            }

            Vector3 strafeVec    = Vector3.Cross(Vector3.up, toTargetDir) * _strafeDir;
            Vector3 strafeTarget = _ai.transform.position + strafeVec * data.StrafeDistance;

            if (NavMesh.SamplePosition(strafeTarget, out var hit, data.StrafeDistance, NavMesh.AllAreas))
            {
                _ai.Agent.isStopped = false;
                _ai.Agent.SetDestination(hit.position);
            }
            else
            {
                _strafeDir = -_strafeDir;
            }
        }

        private void FireOneShot()
        {
            EnemyData data   = _ai.Data;
            var       target = _ai.Perception.Target;
            if (target == null) return;

            Vector3 eyePos       = _ai.transform.position + Vector3.up * EyeHeight;
            // Aims where it believes the target is: dead on while it sees it, a guess when not.
            Vector3 targetCenter = _ai.Perception.TargetPosition + Vector3.up;
            Vector3 dir          = (targetCenter - eyePos).normalized;

            if (data.SpreadAngle > 0f)
                dir = WeaponFireBehavior.ComputeSpreadDirection(dir, data.SpreadAngle);

            SoundBank fireSound = data.RangedAttackSound != null ? data.RangedAttackSound : data.AttackSound;
            fireSound.TryPlay(_ai.transform.position);
            _ai.NotifyFired();

            if (Physics.Raycast(eyePos, dir, out var hit, data.SightRange, ~0, QueryTriggerInteraction.Ignore))
            {
                var info = new DamageInfo(data.AttackDamage, source: _source);
                Hitbox.ApplyHit(hit.collider, info, hit.point);
                ImpactEvents.Report(hit.point, hit.normal, hit.collider, ImpactKind.Bullet, _source);
            }
        }
    }
}
