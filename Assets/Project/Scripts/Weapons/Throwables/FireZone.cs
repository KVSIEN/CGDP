using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    // Ground that keeps burning where an incendiary went off: a flat glowing disc the size of the
    // blast that hurts and ignites everything standing in it, then goes out. The grenade that
    // spawned it passes its owner (so the thrower's team stays safe) and its data; the burn
    // applied is that grenade's On Hit effects. Pooled; needs a transparent material.
    [RequireComponent(typeof(Renderer))]
    public class FireZone : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Collider[] HitBuffer = new Collider[32];
        private static readonly HashSet<IDamageable> Burning = new();

        [SerializeField, Min(0.5f)] private float _duration = 6f;
        [Tooltip("Seconds between burns")]
        [SerializeField, Min(0.1f)] private float _interval = 0.5f;
        [Tooltip("Damage of each burn, on top of the burn status")]
        [SerializeField, Min(0f)] private float _tickDamage = 5f;
        [SerializeField] private Color _color = new(1f, 0.4f, 0.1f, 0.5f);

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private GrenadeData _data;
        private DamageSource _source;
        private float _age;
        private float _nextBurn;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block    = new MaterialPropertyBlock();
        }

        // Sized by the grenade's blast radius; the mesh is a unit cylinder, so its scale is the diameter.
        public void Begin(DamageSource source, GrenadeData data)
        {
            _source   = source;
            _data     = data;
            _age      = 0f;
            _nextBurn = 0f;
            transform.localScale = new Vector3(data.ExplosionRadius * 2f, 0.02f, data.ExplosionRadius * 2f);
        }

        private void Update()
        {
            if (_data == null) return;

            _age += Time.deltaTime;
            if (_age >= _duration)
            {
                _data = null;
                PrefabPool.Release(gameObject);
                return;
            }

            Flicker();
            if (_age < _nextBurn) return;

            _nextBurn = _age + _interval;
            Burn();
        }

        // Flickers while burning and fades out over the last second.
        private void Flicker()
        {
            Color color = _color;
            float flicker = 0.8f + 0.2f * Mathf.Sin(_age * 14f);
            color.a *= flicker * Mathf.Clamp01(_duration - _age);
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        // Each character in the fire is hurt once per burn, however many hitboxes it has there.
        private void Burn()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _data.ExplosionRadius, HitBuffer,
                _data.HitMask, QueryTriggerInteraction.Ignore);

            Burning.Clear();
            for (int i = 0; i < count; i++)
            {
                IDamageable target = Hitbox.FindDamageable(HitBuffer[i]);
                if (target == null || !Burning.Add(target)) continue;

                target.TakeDamage(new DamageInfo(_tickDamage, _data.ArmorPenetration, _data.DamageType,
                    source: _source, onHitEffects: _data.OnHitEffects));
            }
        }
    }
}
