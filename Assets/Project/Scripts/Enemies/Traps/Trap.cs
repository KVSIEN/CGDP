using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // A trap a character lays (TrapAbility): a proximity mine, a jaw snare. Once armed it blinks
    // slowly; a hostile stepping within its trigger radius sets it off — after a short beeping
    // fuse, if it has one — hitting every hostile within its blast radius with its damage and
    // status effects (a Restrain holds the player in place), and making a noise its layer's kin
    // hear. It belongs to its layer's side, so that side walks past it, keeps clear of it as a
    // hazard, and can't break it; anyone else can shoot it (a mine then goes off where it lies).
    // Single use; fizzles out after its lifetime. Pooled.
    [RequireComponent(typeof(DeployableHealth))]
    public class Trap : MonoBehaviour, IPoolable
    {
        private const float CheckInterval = 0.1f;
        private const float SlowBlink     = 1.2f;
        private const float FastBlink     = 0.12f;

        // Its own, not shared: a blast can set off a hostile mine, which then gathers its own blast mid-loop.
        private readonly AreaHits _area = new(16);
        // Every trap that is down, oldest first.
        private static readonly List<Trap> Active = new();

        private readonly HazardZone _hazard = new();
        private DeployableHealth _health;
        private Renderer _renderer;
        private TrapSettings _settings;
        private DamageSource _source;
        private LayerMask _hitMask;
        private StatusEffectApplication[] _effects;
        private float _age;
        private float _nextCheck;
        private float _goesOffAt = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();

        private void Awake()
        {
            _health   = GetComponent<DeployableHealth>();
            _renderer = GetComponentInChildren<Renderer>();
            _health.OnDeath += OnBroken;
        }

        private void OnDestroy() => _health.OnDeath -= OnBroken;

        private void OnDisable() => Unlist();

        void IPoolable.OnDespawned() => _settings = null;

        // How many traps `owner` has down right now.
        public static int CountOwnedBy(GameObject owner)
        {
            int count = 0;
            foreach (Trap trap in Active)
                if (trap._source.Owner == owner) count++;
            return count;
        }

        // Takes up the oldest trap `owner` has down, to make room for a new one.
        public static void RemoveOldestOf(GameObject owner)
        {
            foreach (Trap trap in Active)
            {
                if (trap._source.Owner != owner) continue;
                trap.Finish();
                return;
            }
        }

        // Whether `owner` already has a trap down within `radius` of `position`.
        public static bool AnyOwnedNear(GameObject owner, Vector3 position, float radius)
        {
            foreach (Trap trap in Active)
            {
                if (trap._source.Owner != owner) continue;
                Vector3 offset = trap.transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        public void Deploy(TrapSettings settings, DamageSource source, LayerMask hitMask, StatusEffectApplication[] effects)
        {
            _settings  = settings;
            _source    = source;
            _hitMask   = hitMask;
            _effects   = effects;
            _age       = 0f;
            _nextCheck = settings.ArmDelay;
            _goesOffAt = -1f;
            _health.Deploy(source.Team);

            _hazard.Place(transform.position, settings.TriggerRadius, settings.Damage, source);
            Hazards.Add(_hazard);
            Active.Add(this);
            Tint(false);
        }

        private void Update()
        {
            if (_settings == null || _health.IsDead) return;

            _age += Time.deltaTime;
            if (_goesOffAt >= 0f)
            {
                Tint(Blink(FastBlink));
                if (_age >= _goesOffAt) GoOff();
                return;
            }

            if (_age >= _settings.Lifetime)
            {
                Finish();
                return;
            }

            bool armed = _age >= _settings.ArmDelay;
            Tint(armed && Blink(SlowBlink));
            if (!armed || _age < _nextCheck) return;

            _nextCheck = _age + CheckInterval;
            if (!HostileWithin(_settings.TriggerRadius)) return;

            if (_settings.Fuse <= 0f) GoOff();
            else                      _goesOffAt = _age + _settings.Fuse;
        }

        // Teamless props (crates) never set it off; neither does its own side.
        private bool HostileWithin(float radius)
        {
            _area.Gather(transform.position, radius, _hitMask);
            foreach (AreaHit hit in _area.Hits)
                if (hit.Target is HealthManager target && !target.IsDead && target.Team != Team.None && target.CanBeDamagedBy(_source))
                    return true;
            return false;
        }

        private void GoOff()
        {
            Vector3 center = transform.position;
            _area.Gather(center, _settings.BlastRadius, _hitMask);
            var hit = new DamageInfo(_settings.Damage, _settings.ArmorPenetration, _settings.DamageType,
                                     source: _source, onHitEffects: _effects);
            foreach (AreaHit target in _area.Hits) target.Target.TakeDamage(hit);

            Noise.Emit(center, _settings.NoiseRadius, _source);
            ExplosionFlash.Spawn(_settings.Flash, center + Vector3.up * 0.3f, _settings.BlastRadius, _settings.Color);
            Finish();
        }

        private void OnBroken()
        {
            if (_settings == null) return;
            if (_settings.GoesOffWhenShot) GoOff();
            else                           Finish();
        }

        private void Finish()
        {
            if (_settings == null) return;

            _settings = null;
            Unlist();
            PrefabPool.Release(gameObject);
        }

        private void Unlist()
        {
            Hazards.Remove(_hazard);
            Active.Remove(this);
        }

        private bool Blink(float period) => _age % period < period * 0.3f;

        private void Tint(bool lit)
        {
            if (_renderer == null) return;
            Color color = lit ? _settings.Color : _settings.Color * 0.3f;
            color.a = 1f;
            _renderer.SetTint(color);
        }
    }
}
