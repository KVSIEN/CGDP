using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // A lingering patch of harmful ground: a flat glowing disc that hits everything hostile
    // standing in it once per tick, then fades. One prefab (Prefabs/VFX/DamageZone) serves every
    // such patch: an incendiary's fire, Bio's acid pool and barbed snare, Void's rift. What it
    // does comes from the ZoneSettings its owner passes in. Listed in Hazards while it lasts, so
    // characters keep out of it. Pooled; needs a transparent material.
    [RequireComponent(typeof(Renderer))]
    public class DamageZone : MonoBehaviour
    {
        private static readonly AreaHits Area = new(32);

        private readonly HashSet<HealthManager> _entered = new();
        private readonly HazardZone _hazard = new();
        private Renderer _renderer;
        private ZoneSettings _zone;
        private StatusEffectApplication[] _onHitEffects;
        private DamageSource _source;
        private LayerMask _hitMask;
        private HealthManager _caster;
        private float _radius;
        private float _age;
        private float _nextTick;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void OnDisable() => Hazards.Remove(_hazard);

        // `radius` is passed separately so an owner can resolve its own default (a grenade's
        // blast radius). `caster` is who a Shield Siphon pays back; null for none.
        public void Begin(ZoneSettings zone, float radius, DamageSource source, LayerMask hitMask,
            StatusEffectApplication[] onHitEffects, HealthManager caster = null)
        {
            _zone         = zone;
            _radius       = radius;
            _source       = source;
            _hitMask      = hitMask;
            _onHitEffects = onHitEffects;
            _caster       = caster;
            _age          = 0f;
            _nextTick     = 0f;
            _entered.Clear();

            // The mesh is a unit cylinder, so its scale is the diameter.
            transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            _hazard.Place(transform.position, radius, zone.DamagePerSecond, source);
            Hazards.Add(_hazard);
        }

        // Ends the patch early (the totem it belongs to fell).
        public void End()
        {
            if (_zone == null) return;

            _zone = null;
            Hazards.Remove(_hazard);
            PrefabPool.Release(gameObject);
        }

        private void Update()
        {
            if (_zone == null) return;

            _age += Time.deltaTime;
            if (_age >= _zone.Duration)
            {
                End();
                return;
            }

            Tint();
            if (_age < _nextTick) return;

            _nextTick = _age + _zone.TickInterval;
            Tick();
        }

        private void Tint()
        {
            Color color = _zone.Color;
            color.a *= GlowStrength() * Mathf.Clamp01(_zone.Duration - _age);
            _renderer.SetTint(color);
        }

        private float GlowStrength() => _zone.Glow switch
        {
            ZoneGlow.Flicker => 0.8f + 0.2f * Mathf.Sin(_age * 14f),
            ZoneGlow.Pulse   => 0.75f + 0.25f * Mathf.Sin(_age * 5f),
            _                => 1f
        };

        // Each character in the zone is hit once per tick, however many hitboxes it has there.
        private void Tick()
        {
            Area.Gather(transform.position, _radius, _hitMask);
            var hit = new DamageInfo(_zone.DamagePerTick, _zone.ArmorPenetration, _zone.DamageType,
                source: _source, onHitEffects: _onHitEffects);

            float siphoned = 0f;
            foreach (AreaHit area in Area.Hits)
            {
                if (area.Target is not HealthManager target || !target.CanBeDamagedBy(_source)) continue;

                float before = target.Health + target.Shield + target.Plating;
                target.TakeDamage(hit);
                siphoned += Mathf.Max(0f, before - (target.Health + target.Shield + target.Plating));

                if (_zone.EntryNoiseRadius > 0f && _entered.Add(target))
                    Noise.Emit(target.transform.position, _zone.EntryNoiseRadius, _source);
            }

            if (_caster != null && siphoned > 0f && _zone.ShieldSiphon > 0f)
                _caster.RestoreShield(siphoned * _zone.ShieldSiphon);
        }
    }
}
