using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Abilities
{
    // The ground a Rift ability opened: a flat glowing disc the size of its radius that hurts
    // everything hostile standing in it once per tick, slows it, and pays the caster back in
    // shield, then closes. Pooled; needs a transparent material.
    [RequireComponent(typeof(Renderer))]
    public class RiftZone : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Collider[] HitBuffer = new Collider[32];
        private static readonly HashSet<HealthManager> Caught = new();

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private RiftAbility _rift;
        private DamageSource _source;
        private HealthManager _siphoner;
        private float _age;
        private float _nextTick;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block    = new MaterialPropertyBlock();
        }

        // `siphoner` is who recovers shield from the damage (the caster).
        public void Begin(RiftAbility rift, DamageSource source, HealthManager siphoner)
        {
            _rift     = rift;
            _source   = source;
            _siphoner = siphoner;
            _age      = 0f;
            _nextTick = 0f;
            // The mesh is a unit cylinder, so its scale is the diameter.
            transform.localScale = new Vector3(rift.Radius * 2f, 0.02f, rift.Radius * 2f);
        }

        private void Update()
        {
            if (_rift == null) return;

            _age += Time.deltaTime;
            if (_age >= _rift.Duration)
            {
                _rift = null;
                PrefabPool.Release(gameObject);
                return;
            }

            Pulse();
            if (_age < _nextTick) return;

            _nextTick = _age + _rift.TickInterval;
            Tick();
        }

        // Breathes while open and fades out over the last second.
        private void Pulse()
        {
            Color color = _rift.Color;
            color.a *= (0.75f + 0.25f * Mathf.Sin(_age * 5f)) * Mathf.Clamp01(_rift.Duration - _age);
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        // Each character in the rift is hurt once per tick, however many hitboxes it has there.
        private void Tick()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _rift.Radius, HitBuffer,
                _rift.HitMask, QueryTriggerInteraction.Ignore);

            Caught.Clear();
            float siphoned = 0f;
            for (int i = 0; i < count; i++)
            {
                if (Hitbox.FindDamageable(HitBuffer[i]) is not HealthManager target) continue;
                if (!target.CanBeDamagedBy(_source) || !Caught.Add(target)) continue;

                float before = target.Health + target.Shield;
                target.TakeDamage(new DamageInfo(_rift.DamagePerTick, _rift.ArmorPenetration, _rift.DamageType,
                    source: _source, onHitEffects: _rift.OnHitEffects));
                siphoned += Mathf.Max(0f, before - (target.Health + target.Shield));
            }

            if (_siphoner != null && siphoned > 0f) _siphoner.RestoreShield(siphoned * _rift.ShieldSiphon);
        }
    }
}
