using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    // Ground that keeps hurting where a grenade went off: a flat glowing disc that damages
    // everything standing in it and applies the grenade's On Hit effects, then fades. One
    // prefab serves every such grenade (an incendiary's fire, Bio's acid pool): size, timing,
    // damage, element and colour all come from the GrenadeData that spawned it, which also
    // passes its owner so the thrower's team stays safe. Pooled; needs a transparent material.
    [RequireComponent(typeof(Renderer))]
    public class DamageZone : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Collider[] HitBuffer = new Collider[32];
        private static readonly HashSet<IDamageable> HitThisTick = new();

        private readonly HazardZone _hazard = new();
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private GrenadeData _data;
        private DamageSource _source;
        private float _radius;
        private float _age;
        private float _nextTick;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block    = new MaterialPropertyBlock();
        }

        private void OnDisable() => Hazards.Remove(_hazard);

        // The mesh is a unit cylinder, so its scale is the diameter. Listed in Hazards while it
        // lasts, so characters keep out of it.
        public void Begin(DamageSource source, GrenadeData data)
        {
            _source   = source;
            _data     = data;
            _radius   = data.ZoneRadiusOrBlast;
            _age      = 0f;
            _nextTick = 0f;
            transform.localScale = new Vector3(_radius * 2f, 0.02f, _radius * 2f);

            _hazard.Place(transform.position, _radius, data.ZoneTickDamage / data.ZoneTickInterval, source);
            Hazards.Add(_hazard);
        }

        private void Update()
        {
            if (_data == null) return;

            _age += Time.deltaTime;
            if (_age >= _data.ZoneDuration)
            {
                _data = null;
                Hazards.Remove(_hazard);
                PrefabPool.Release(gameObject);
                return;
            }

            Flicker();
            if (_age < _nextTick) return;

            _nextTick = _age + _data.ZoneTickInterval;
            Tick();
        }

        // Flickers while active and fades out over the last second.
        private void Flicker()
        {
            Color color = _data.ZoneColor;
            float flicker = 0.8f + 0.2f * Mathf.Sin(_age * 14f);
            color.a *= flicker * Mathf.Clamp01(_data.ZoneDuration - _age);
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        // Each character in the zone is hit once per tick, however many hitboxes it has there.
        private void Tick()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, HitBuffer,
                _data.HitMask, QueryTriggerInteraction.Ignore);

            HitThisTick.Clear();
            for (int i = 0; i < count; i++)
            {
                IDamageable target = Hitbox.FindDamageable(HitBuffer[i]);
                if (target == null || !HitThisTick.Add(target)) continue;

                target.TakeDamage(new DamageInfo(_data.ZoneTickDamage, _data.ArmorPenetration, _data.DamageType,
                    source: _source, onHitEffects: _data.OnHitEffects));
            }
        }
    }
}
