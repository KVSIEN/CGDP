using System.Collections.Generic;
using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Items;
using CGD.Stats;

namespace CGD.Enemies
{
    // An enemy's melee swing: a wind-up, then one blow on everything in reach in front of it,
    // then a cooldown before the next. Attack speed (the FireRate stat, e.g. Bio's Surge)
    // shortens wind-up and cooldown alike; an armed Ambush multiplies the blow. Swinging gives
    // a hiding enemy away. Reads its EnemyData each swing: rolled affixes swap it after Awake.
    internal sealed class EnemyMelee
    {
        private static readonly Collider[] HitBuffer = new Collider[16];

        private readonly EnemyAI _ai;
        private readonly DamageSource _source;
        private readonly LayerMask _targetMask;
        private readonly CharacterStats _stats;
        private readonly HashSet<HealthManager> _hits = new();

        private CooldownTimer _cooldown;
        private float _windupLeft;

        public EnemyMelee(EnemyAI ai, DamageSource source, LayerMask targetMask, CharacterStats stats)
        {
            _ai         = ai;
            _source     = source;
            _targetMask = targetMask;
            _stats      = stats;
        }

        public bool IsAttacking { get; private set; }
        public bool CanAttack   => !IsAttacking && _cooldown.IsReady;

        // Starts a swing (the blow lands after the wind-up). False while swinging or cooling down.
        public bool TryStart()
        {
            if (!CanAttack) return false;

            EnemyData data = _ai.Data;
            float swing = 1f / AttackSpeed;
            IsAttacking = true;
            _windupLeft = data.AttackWindup * swing;
            _cooldown.Start((data.AttackWindup + data.AttackCooldown) * swing);
            return true;
        }

        public void Tick(float deltaTime)
        {
            _cooldown.Tick(deltaTime);
            if (!IsAttacking) return;

            _windupLeft -= deltaTime;
            if (_windupLeft > 0f) return;

            IsAttacking = false;
            Strike();
            _ai.Tactics.OnStrike();
        }

        // A stagger or death cuts the swing off; the cooldown it started still runs.
        public void Interrupt() => IsAttacking = false;

        public void Reset()
        {
            IsAttacking = false;
            _cooldown.Reset();
        }

        private float AttackSpeed => _stats != null ? Mathf.Max(0.1f, _stats.Apply(ItemStat.FireRate, 1f)) : 1f;

        private void Strike()
        {
            EnemyData data = _ai.Data;
            Transform self = _ai.transform;
            data.AttackSound.TryPlay(self.position);

            float   range  = data.AttackRange;
            Vector3 center = self.position + Vector3.up + self.forward * (range * 0.5f);
            int count = Physics.OverlapSphereNonAlloc(center, range * 0.6f, HitBuffer, _targetMask, QueryTriggerInteraction.Ignore);

            float damage = _stats != null ? _stats.Apply(ItemStat.Damage, data.AttackDamage) : data.AttackDamage;
            damage *= _ai.Ambush.Consume();
            _ai.Reveal();
            var info = new DamageInfo(damage, source: _source, onHitEffects: data.MeleeEffects);

            _hits.Clear();
            for (int i = 0; i < count; i++)
            {
                if (Hitbox.FindDamageable(HitBuffer[i]) is not HealthManager target) continue;
                if (target == _ai.Health || !_hits.Add(target)) continue;
                target.TakeDamage(info);
            }
        }
    }
}
