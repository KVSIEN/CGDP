using UnityEngine;
using UnityEngine.AI;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // Ranged variant of Chase: maintains preferred distance, strafes at range,
    // fires hitscan bursts when the target is visible. Falls back to Alert when
    // the target is lost.
    public class RangedChaseState : EnemyState
    {
        private const float EyeHeight = 1.5f;

        private readonly DamageSource _source;
        private readonly LayerMask _obstacleMask;

        private CooldownTimer _fireCooldown;
        private int   _burstRemaining;
        private float _burstTimer;
        private float _strafeTimer;
        private int   _strafeDir = 1;

        public RangedChaseState(EnemyAI ai, DamageSource source, LayerMask obstacleMask) : base(ai)
        {
            _source = source;
            _obstacleMask = obstacleMask;
        }

        public override EnemyAI.AiState Id => EnemyAI.AiState.Chase;

        public override void Enter()
        {
            _burstRemaining = 0;
            _strafeTimer    = Ai.Data.StrafeInterval;
        }

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;

            if (!perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Alert);
                return;
            }

            _fireCooldown.Tick(deltaTime);

            var data      = Ai.Data;
            var targetPos = perception.Target.transform.position;
            Ai.SetSpeed(data.ChaseSpeed);
            Ai.FaceTowards(targetPos, deltaTime);

            if (_burstRemaining > 0)
            {
                Ai.Agent.isStopped = true;
                TickBurst(deltaTime);
                return;
            }

            Vector3 toTarget = targetPos - Ai.transform.position;
            float dist       = toTarget.magnitude;
            float preferred  = data.PreferredRange;
            float tolerance  = preferred * 0.15f;

            if (dist > preferred + tolerance)
            {
                Ai.Agent.isStopped = false;
                Ai.Agent.SetDestination(targetPos);
            }
            else if (dist < preferred - tolerance)
            {
                Retreat(toTarget.normalized, preferred - dist + tolerance);
            }
            else
            {
                Strafe(toTarget.normalized, deltaTime);

                if (HasLineOfSight(targetPos))
                    TryFire();
            }
        }

        public override void Exit()
        {
            Ai.Agent.isStopped = false;
            _burstRemaining = 0;
        }

        private void Retreat(Vector3 toTargetDir, float distance)
        {
            Vector3 retreatTarget = Ai.transform.position - toTargetDir * distance;
            float tolerance = Ai.Data.PreferredRange * 0.15f;

            if (NavMesh.SamplePosition(retreatTarget, out var hit, tolerance * 2f, NavMesh.AllAreas))
            {
                Ai.Agent.isStopped = false;
                Ai.Agent.SetDestination(hit.position);
            }
        }

        private void Strafe(Vector3 toTargetDir, float deltaTime)
        {
            var data = Ai.Data;
            _strafeTimer -= deltaTime;

            if (_strafeTimer <= 0f)
            {
                _strafeTimer = data.StrafeInterval;
                _strafeDir = Random.value > 0.5f ? 1 : -1;
            }

            Vector3 strafeVec = Vector3.Cross(Vector3.up, toTargetDir) * _strafeDir;
            Vector3 strafeTarget = Ai.transform.position + strafeVec * data.StrafeDistance;

            if (NavMesh.SamplePosition(strafeTarget, out var hit, data.StrafeDistance, NavMesh.AllAreas))
            {
                Ai.Agent.isStopped = false;
                Ai.Agent.SetDestination(hit.position);
            }
            else
            {
                _strafeDir = -_strafeDir;
            }
        }

        private void TryFire()
        {
            if (!_fireCooldown.IsReady) return;

            var data = Ai.Data;
            _fireCooldown.Start(data.AttackCooldown);
            _burstRemaining = Mathf.Max(1, data.BurstCount);
            _burstTimer = 0f;
            FireOneShot();
            _burstRemaining--;
        }

        private void TickBurst(float deltaTime)
        {
            if (_burstRemaining <= 0) return;

            _burstTimer -= deltaTime;
            if (_burstTimer > 0f) return;

            FireOneShot();
            _burstRemaining--;
            _burstTimer = Ai.Data.BurstInterval;
        }

        private void FireOneShot()
        {
            var data = Ai.Data;
            var target = Ai.Perception.Target;
            if (target == null) return;

            Vector3 eyePos = Ai.transform.position + Vector3.up * EyeHeight;
            Vector3 targetCenter = target.transform.position + Vector3.up;
            Vector3 dir = (targetCenter - eyePos).normalized;

            if (data.SpreadAngle > 0f)
                dir = WeaponFireBehavior.ComputeSpreadDirection(dir, data.SpreadAngle);

            SoundBank fireSound = data.RangedAttackSound != null ? data.RangedAttackSound : data.AttackSound;
            fireSound.TryPlay(Ai.transform.position);

            if (Physics.Raycast(eyePos, dir, out var hit, data.SightRange, ~0, QueryTriggerInteraction.Ignore))
            {
                var info = new DamageInfo(data.AttackDamage, source: _source);
                Hitbox.ApplyHit(hit.collider, info, hit.point);
            }
        }

        private bool HasLineOfSight(Vector3 targetPos)
        {
            Vector3 eyePos = Ai.transform.position + Vector3.up * EyeHeight;
            Vector3 toTarget = targetPos + Vector3.up - eyePos;
            float dist = toTarget.magnitude;

            return !Physics.Raycast(eyePos, toTarget / dist, dist, _obstacleMask, QueryTriggerInteraction.Ignore);
        }
    }
}
