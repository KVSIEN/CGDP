using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Abilities
{
    // The patch a Snare ability burst: a flat disc of barbed growth that scratches everything
    // hostile standing in it once per tick (the scratch slows it and makes it bleed) and
    // squelches the first time each one steps in, so the caster's kin hear the trap spring.
    // Characters see it as a hazard and step out. Pooled; needs a transparent material.
    [RequireComponent(typeof(Renderer))]
    public class SnareZone : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Collider[] HitBuffer = new Collider[32];
        private static readonly HashSet<HealthManager> Caught = new();

        private readonly HashSet<HealthManager> _sprung = new();
        private readonly HazardZone _hazard = new();
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private SnareAbility _snare;
        private DamageSource _source;
        private float _age;
        private float _nextTick;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block    = new MaterialPropertyBlock();
        }

        private void OnDisable() => Hazards.Remove(_hazard);

        public void Begin(SnareAbility snare, DamageSource source)
        {
            _snare    = snare;
            _source   = source;
            _age      = 0f;
            _nextTick = 0f;
            _sprung.Clear();
            // The mesh is a unit cylinder, so its scale is the diameter.
            transform.localScale = new Vector3(snare.Radius * 2f, 0.02f, snare.Radius * 2f);

            _hazard.Place(transform.position, snare.Radius, snare.DamagePerTick / snare.TickInterval, source);
            Hazards.Add(_hazard);
        }

        private void Update()
        {
            if (_snare == null) return;

            _age += Time.deltaTime;
            if (_age >= _snare.Duration)
            {
                _snare = null;
                Hazards.Remove(_hazard);
                PrefabPool.Release(gameObject);
                return;
            }

            Fade();
            if (_age < _nextTick) return;

            _nextTick = _age + _snare.TickInterval;
            Tick();
        }

        // Fades out over the last second.
        private void Fade()
        {
            Color color = _snare.Color;
            color.a *= Mathf.Clamp01(_snare.Duration - _age);
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        // Each character in the patch is scratched once per tick, however many hitboxes it has there.
        private void Tick()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _snare.Radius, HitBuffer,
                _snare.HitMask, QueryTriggerInteraction.Ignore);

            Caught.Clear();
            for (int i = 0; i < count; i++)
            {
                if (Hitbox.FindDamageable(HitBuffer[i]) is not HealthManager target) continue;
                if (!target.CanBeDamagedBy(_source) || !Caught.Add(target)) continue;

                target.TakeDamage(new DamageInfo(_snare.DamagePerTick, 0f, DamageType.True,
                    source: _source, onHitEffects: _snare.OnHitEffects));
                if (_sprung.Add(target)) Noise.Emit(target.transform.position, _snare.EntryNoiseRadius, _source);
            }
        }
    }
}
