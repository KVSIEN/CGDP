using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    [Serializable]
    public class SpawnProjectileEvent : IActionEvent
    {
        [SerializeField] private int _startFrame;

        [Header("Projectile")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private float _spawnOffset = 1.5f;
        [SerializeField] private float _damage = 25f;
        [SerializeField] private float _speed = 25f;
        [Min(0f)]
        [SerializeField] private float _gravity;
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private float _maxDistance = Mathf.Infinity;
        [SerializeField] private StatusEffectApplication[] _onHitEffects;

        public int StartFrame => _startFrame;
        public int EndFrame => -1;

        public void OnEnter(ActionContext ctx)
        {
            if (_prefab == null) return;

            Vector3 spawnPos = ctx.Origin + ctx.Forward * _spawnOffset;
            Quaternion rot = Quaternion.LookRotation(ctx.Forward, ctx.Up);
            var go = PrefabPool.Spawn(_prefab, spawnPos, rot);

            if (!go.TryGetComponent(out Projectile projectile)) return;

            var info = new DamageInfo(_damage, source: ctx.Source, onHitEffects: _onHitEffects);
            projectile.Launch(new ProjectileLaunch
            {
                Damage    = info,
                Velocity  = ctx.Forward * _speed,
                Gravity   = _gravity,
                Lifetime  = _lifetime,
                MaxDistance = _maxDistance,
                HitMask   = ctx.HitMask,
                Falloff   = DamageFalloff.None,
            });
        }

        public void OnTick(ActionContext ctx) { }
        public void OnExit(ActionContext ctx) { }
    }
}
