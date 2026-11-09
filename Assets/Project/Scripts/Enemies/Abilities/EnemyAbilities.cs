using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.Combat;
using CGD.Stats;
using CGD.Stealth;

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
        private readonly Dictionary<Ability, float> _effectEndsAt = new();
        // Every ability its tree has asked about so far, in the order it first did (for the
        // spectator's inspector: cooldowns of abilities it hasn't cast yet show as ready).
        private readonly List<Ability> _known = new();

        private Ability _casting;
        private float   _castEndsAt;
        private Vector3 _castAimAt;

        public EnemyAbilities(EnemyAI ai, DamageSource source, Stealthable stealth)
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
                Stealth         = stealth,
                Ambush          = ai.Ambush,
            };
        }

        public bool IsCasting => _casting != null;
        public Ability Casting => _casting;
        public IReadOnlyList<Ability> Known => _known;

        public bool IsReady(Ability ability)
        {
            if (ability == null) return false;
            Remember(ability);
            return !IsCasting && Time.time >= ReadyAt(ability) && ability.CanExecute(_context);
        }

        public float CooldownLeft(Ability ability) => Mathf.Max(0f, ReadyAt(ability) - Time.time);

        // Seconds left of what its last cast left on it (a buff); 0 when nothing lasts.
        public float EffectLeft(Ability ability) =>
            _effectEndsAt.TryGetValue(ability, out float endsAt) ? Mathf.Max(0f, endsAt - Time.time) : 0f;

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
            Ability ability = _casting;
            _casting = null;
            ability.Execute(_context);
            _readyAt[ability] = Time.time + ability.Cooldown;
            if (ability.EffectDuration > 0f) _effectEndsAt[ability] = Time.time + ability.EffectDuration;
            _ai.NotifyAbilityUsed(ability);
            return true;
        }

        public void Cancel() => _casting = null;

        // Back from the dead: buffs ended with it (CharacterStats clears them on revive).
        public void Reset()
        {
            _casting = null;
            _effectEndsAt.Clear();
        }

        private void AimAt(Vector3 point)
        {
            Vector3 direction = point - _aim.position;
            if (direction.sqrMagnitude > 0.0001f) _aim.rotation = Quaternion.LookRotation(direction);
        }

        private void Remember(Ability ability)
        {
            if (_readyAt.ContainsKey(ability)) return;
            _readyAt[ability] = 0f;
            _known.Add(ability);
        }

        private float ReadyAt(Ability ability) => _readyAt.TryGetValue(ability, out float at) ? at : 0f;
    }
}
