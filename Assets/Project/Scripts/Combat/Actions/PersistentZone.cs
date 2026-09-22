using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    public class PersistentZone : MonoBehaviour
    {
        private static readonly Collider[] OverlapBuffer = new Collider[32];

        [Header("Zone")]
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private float _radius = 3f;
        [SerializeField] private float _tickInterval = 0.5f;

        [Header("Damage")]
        [SerializeField] private float _damage = 10f;
        [Range(0f, 1f)]
        [SerializeField] private float _armorPenetration;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private StatusEffectApplication[] _onHitEffects;

        [Header("Behavior")]
        [Tooltip("True = trap (one-shot, releases after first hit). False = lingering AOE.")]
        [SerializeField] private bool _triggerOnce;

        private DamageSource _source;
        private LayerMask _hitMask;
        private float _lifeTimer;
        private float _tickTimer;
        private bool _initialized;

        private readonly HashSet<IDamageable> _hitPerTick = new();

        public void Init(DamageSource source, LayerMask hitMask)
        {
            _source = source;
            _hitMask = hitMask;
            _lifeTimer = _lifetime;
            _tickTimer = 0f;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Release();
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = _tickInterval;

            TickDamage();
        }

        private void TickDamage()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _radius, OverlapBuffer,
                _hitMask, QueryTriggerInteraction.Ignore);

            var info = new DamageInfo(
                _damage, _armorPenetration, _damageType,
                source: _source, onHitEffects: _onHitEffects);

            _hitPerTick.Clear();

            for (int i = 0; i < count; i++)
            {
                IDamageable target = Hitbox.FindDamageable(OverlapBuffer[i]);
                if (target == null || !_hitPerTick.Add(target)) continue;

                target.TakeDamage(info);

                if (_triggerOnce)
                {
                    Release();
                    return;
                }
            }
        }

        private void Release()
        {
            _initialized = false;
            PrefabPool.Release(gameObject);
        }
    }
}
