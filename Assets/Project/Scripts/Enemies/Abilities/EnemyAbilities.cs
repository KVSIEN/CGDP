using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.Combat;
using CGD.Stats;

namespace CGD.Enemies
{
    // Lets an enemy cast the project's Ability assets (projectiles, shockwaves, timeline
    // attacks, buffs, heals) the way the player does: an AbilityContext with its body, its
    // health, its damage source and an aim point at its eyes that turns toward whatever it
    // aims at. Abilities that need a player-only part (dash, reflect, stealth) can't run;
    // Phase Shift, which needs to find hidden cover, only runs on enemies.
    // Each ability keeps its own cooldown per enemy; meter costs and charges don't apply.
    internal sealed class EnemyAbilities
    {
        private const float EyeHeight = 1.5f;

        private readonly EnemyAI _ai;
        private readonly Transform _aim;
        private readonly AbilityContext _context;
        private readonly Dictionary<Ability, float> _readyAt = new();

        private Ability _casting;
        private float   _castEndsAt;
        private Vector3 _castAimAt;

        public EnemyAbilities(EnemyAI ai, DamageSource source)
        {
            _ai = ai;
            _aim = new GameObject("AbilityAim").transform;
            _aim.SetParent(ai.transform, false);
            _aim.localPosition = Vector3.up * EyeHeight;

            ai.TryGetComponent(out CharacterStats stats);
            ai.TryGetComponent(out TimelineAbilityRunner timeline);
            _context = new AbilityContext
            {
                CasterTransform = ai.transform,
                CasterRigidbody = ai.GetComponent<Rigidbody>(),
                CasterCollider  = ai.GetComponent<Collider>(),
                AimTransform    = _aim,
                Health          = ai.Health,
                Source          = source,
                Stats           = stats,
                Timeline        = timeline,
                Relocator       = new EnemyPhaseShift(ai),
            };
        }

        public bool IsCasting => _casting != null;

        public bool IsReady(Ability ability) =>
            ability != null && !IsCasting && Time.time >= ReadyAt(ability) && ability.CanExecute(_context);

        // Starts the cast (its CastTime is a wind-up spent facing the aim point). False if not ready.
        public bool TryBegin(Ability ability, Vector3 aimAt)
        {
            if (!IsReady(ability)) return false;

            _casting    = ability;
            _castAimAt  = aimAt;
            _castEndsAt = Time.time + ability.CastTime;
            return true;
        }

        // Advances a cast; true once it has gone off (or was cancelled).
        public bool TickCast(Vector3 aimAt, float deltaTime)
        {
            if (_casting == null) return true;

            _castAimAt = aimAt;
            _ai.FaceTowards(aimAt, deltaTime);
            if (Time.time < _castEndsAt) return false;

            AimAt(_castAimAt);
            _casting.Execute(_context);
            _readyAt[_casting] = Time.time + _casting.Cooldown;
            _casting = null;
            return true;
        }

        public void Cancel() => _casting = null;

        private void AimAt(Vector3 point)
        {
            Vector3 direction = point - _aim.position;
            if (direction.sqrMagnitude > 0.0001f) _aim.rotation = Quaternion.LookRotation(direction);
        }

        private float ReadyAt(Ability ability) => _readyAt.TryGetValue(ability, out float at) ? at : 0f;
    }
}
