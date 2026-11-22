using UnityEngine;
using UnityEngine.AI;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Impacts;
using CGD.Items;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.Enemies
{
    // How a ranged enemy fights: holding its preferred distance (approach, back off, strafe
    // while in the band) and firing bursts — hitscan, or visible projectiles when its data has
    // a ShotProjectile. Used by the ranged behavior tree nodes
    // (Hold Range, Shoot Burst, Suppress).
    internal sealed class EnemyRangedCombat
    {
        // The band around the preferred range that counts as "in position".
        private const float RangeTolerance = 0.15f;
        private const float MuzzleOffset   = 0.6f;
        // A shot that stops this close to where it was aimed counts as reaching it (cover the
        // target crouches behind); any earlier and it is a shot into a wall.
        private const float ClearShotSlack = 2.5f;
        // A blast bursting on scenery still reaches a target within its radius less its body, but never less than this.
        private const float MinBlastSlack     = 0.5f;
        private const float TargetBodyRadius  = 0.5f;

        private readonly EnemyAI      _ai;
        private readonly DamageSource _source;
        private readonly LayerMask    _obstacleMask;
        // Its damage buffs (a rallying roar, an enchantment) count for its shots as for its strikes.
        private readonly CharacterStats _stats;

        private CooldownTimer _fireCooldown;
        private int   _burstRemaining;
        private float _burstTimer;
        private float _strafeTimer;
        private int   _strafeDir = 1;
        private bool  _hasShooterToken;

        public EnemyRangedCombat(EnemyAI ai, DamageSource source, LayerMask obstacleMask, CharacterStats stats)
        {
            _ai           = ai;
            _source       = source;
            _obstacleMask = obstacleMask;
            _stats        = stats;
        }

        public bool IsBursting => _burstRemaining > 0;
        public bool CanFire    => !IsBursting && _fireCooldown.IsReady && !_ai.Tells.IsCommitting;

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
                _ai.Routing.SetDestination(targetPos);
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
            Vector3 eyePos = _ai.transform.position + Vector3.up * EnemyEyes.Height;
            Vector3 aimAt  = targetPos + Vector3.up;
            if ((aimAt - eyePos).sqrMagnitude < 0.000001f) return true;

            HealthManager target = _ai.Perception.Target;
            return SightLines.IsClear(eyePos, aimAt, _obstacleMask, target != null ? target.transform : null);
        }

        // Fires the first shot of a burst now; the rest follow every BurstInterval.
        public bool TryStartBurst()
        {
            if (!CanFire) return false;

            // Only a few enemies fire at one target at once (EnemyData.MaxShooters, scaled by pacing).
            var target = _ai.Perception.Target;
            if (target == null) return false;
            _hasShooterToken = AttackTokens.TryAcquire(AttackTokenKind.Ranged, target, _ai,
                EnemyPacing.Current.ScaleAttackers(_ai.Data.MaxShooters));
            if (!_hasShooterToken) return false;

            EnemyData data = _ai.Data;
            _burstRemaining = Mathf.Max(1, data.BurstCount);
            // A blocked first shot isn't taken at all: no cooldown spent, no token held.
            if (!FireOneShot())
            {
                CancelBurst();
                return false;
            }
            _fireCooldown.Start(data.RangedAttackCooldown > 0f ? data.RangedAttackCooldown : data.AttackCooldown);
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

            // The target may have stepped behind scenery since the first shot: drop the rest.
            if (!FireOneShot())
            {
                CancelBurst();
                return;
            }
            _burstRemaining--;
            _burstTimer = _ai.Data.BurstInterval;
        }

        private void Retreat(Vector3 toTargetDir, float distance, float tolerance)
        {
            Vector3 retreatTarget = _ai.transform.position - toTargetDir * distance;
            if (NavMesh.SamplePosition(retreatTarget, out var hit, tolerance * 2f, NavMesh.AllAreas))
            {
                _ai.Agent.isStopped = false;
                _ai.Routing.SetDestination(hit.position);
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
                _ai.Routing.SetDestination(hit.position);
            }
            else
            {
                _strafeDir = -_strafeDir;
            }
        }

        // Whether a shot at this point would reach it rather than hit scenery on the way —
        // a wall in front of the shooter, a corner the target is round.
        public bool HasClearShot(Vector3 aimPoint) =>
            SightLines.ReachesWithin(_ai.transform.position + Vector3.up * EnemyEyes.Height, aimPoint, ShotSlack(), _obstacleMask, ShotWidth());

        // How wide its shot flies: a projectile has a body, a hitscan shot none.
        private float ShotWidth() => HasProjectile() ? _ai.Data.ShotRadius : 0f;

        // How short of its aim point a shot may burst on scenery and still count: a blast reaches a target
        // standing that close to the impact (less its body), but a shot that dies further off is wasted on the wall.
        private float ShotSlack()
        {
            float blast = HasProjectile() ? _ai.Data.ShotBlastRadius : 0f;
            return blast > 0f ? Mathf.Min(ClearShotSlack, Mathf.Max(MinBlastSlack, blast - TargetBodyRadius)) : ClearShotSlack;
        }

        private bool HasProjectile() => _ai.Data.ShotProjectile != null && _ai.Data.ShotProjectile.TryGetComponent(out Projectile _);

        // False when the shot isn't taken because scenery blocks it.
        private bool FireOneShot()
        {
            EnemyData data   = _ai.Data;
            var       target = _ai.Perception.Target;
            if (target == null) return false;
            if (data.ShotGrenade != null) return LobGrenade(data);

            Vector3 eyePos       = _ai.transform.position + Vector3.up * EnemyEyes.Height;
            // Aims where it believes the target is: dead on while it sees it, a guess when not.
            // A projectile takes time to arrive, so it aims ahead of a target it sees moving.
            Vector3 targetCenter = _ai.Perception.TargetPosition + Vector3.up;
            bool    projectile   = HasProjectile();
            if (projectile)
            {
                float flightTime = Vector3.Distance(eyePos, targetCenter) / data.ShotSpeed;
                targetCenter += _ai.Perception.TargetVelocity * flightTime;
            }
            Vector3 dir = (targetCenter - eyePos).normalized;

            if (data.SpreadAngle > 0f)
                dir = WeaponFireBehavior.ComputeSpreadDirection(dir, data.SpreadAngle);

            // Checks the shot as it will really fly (lead and spread included), not just the line to the target.
            float reach = Vector3.Distance(eyePos, targetCenter);
            if (!SightLines.ReachesWithin(eyePos, eyePos + dir * reach, ShotSlack(), _obstacleMask, ShotWidth())) return false;

            SoundBank fireSound = data.RangedAttackSound != null ? data.RangedAttackSound : data.AttackSound;
            fireSound.TryPlay(_ai.transform.position);
            _ai.NotifyFired();

            float damage = data.RangedAttackDamage > 0f ? data.RangedAttackDamage : data.AttackDamage;
            if (_stats != null) damage = _stats.Apply(ItemStat.Damage, damage);
            var info = new DamageInfo(damage, source: _source,
                                  onHitEffects: data.ShotEffects);
            if (projectile)
            {
                LaunchProjectile(data, eyePos, dir, info);
                return true;
            }

            if (ShotRay.Cast(eyePos, dir, out var hit, data.SightRange, ~0))
            {
                Hitbox.ApplyHit(hit.collider, info, hit.point);
                ImpactEvents.Report(hit.point, hit.normal, hit.collider, ImpactKind.Bullet, _source);
            }
            return true;
        }

        // A mortar round: the grenade goes up and comes down on where the target is believed to be,
        // as far off as the Spread Angle would carry a straight shot at that distance. False when
        // the lob can't be made (out of throwing range, a wall in the way).
        private bool LobGrenade(EnemyData data)
        {
            EnemyPerception perception = _ai.Perception;
            Vector3 spot    = perception.TargetPosition;
            float   scatter = Vector3.Distance(_ai.transform.position, spot) * Mathf.Tan(data.SpreadAngle * Mathf.Deg2Rad);
            // The throw plays its own sound and notifies the enemy it threw.
            return _ai.Grenades.TryLob(data.ShotGrenade, spot, perception.TargetVelocity, scatter);
        }

        // Starts just outside its own body (the projectile ignores the shooter anyway) and
        // flies as far as the enemy can see.
        private static void LaunchProjectile(EnemyData data, Vector3 eyePos, Vector3 dir, DamageInfo info)
        {
            var go = PrefabPool.Spawn(data.ShotProjectile, eyePos + dir * MuzzleOffset, Quaternion.LookRotation(dir));
            go.GetComponent<Projectile>().Launch(new ProjectileLaunch
            {
                Damage      = info,
                Velocity    = dir * data.ShotSpeed,
                Lifetime    = data.SightRange / data.ShotSpeed + 0.5f,
                MaxDistance = data.SightRange,
                HitMask     = ~0,
                Falloff     = DamageFalloff.None,
                HitRadius   = data.ShotRadius,
                BlastRadius = data.ShotBlastRadius,
            });
        }
    }
}
