using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Items;
using CGD.Stats;

namespace CGD.Enemies
{
    // An enemy's melee swing: a wind-up, then one blow on everything in reach in front of it
    // (or, with a Melee Combo, a run of blows each with its own wind-up and area), then a
    // cooldown before the next. Attack speed (the FireRate stat, e.g. Bio's Surge)
    // shortens wind-up and cooldown alike; an armed Ambush multiplies the blow and adds its own
    // on-hit effects (Bio's bleed). Swinging gives a hiding enemy away. Reads its EnemyData each
    // swing: rolled affixes swap it after Awake.
    internal sealed class EnemyMelee
    {
        private static readonly AreaHits Reach = new(16);

        private readonly EnemyAI _ai;
        private readonly DamageSource _source;
        private readonly LayerMask _targetMask;
        private readonly CharacterStats _stats;

        private CooldownTimer _cooldown;
        private float _windupLeft;
        private int   _blow;   // which blow of the combo it is winding up

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
            _blow       = 0;
            _windupLeft = BlowAt(data, 0).Windup * swing;
            _cooldown.Start((data.MeleeWindupTime + data.AttackCooldown) * swing);
            return true;
        }

        public void Tick(float deltaTime)
        {
            _cooldown.Tick(deltaTime);
            if (!IsAttacking) return;

            _windupLeft -= deltaTime;
            if (_windupLeft > 0f) return;

            EnemyData data = _ai.Data;
            Strike(BlowAt(data, _blow), data);
            _ai.Tactics.OnStrike();

            // The next blow of the combo winds up on the time the last one overshot by.
            int blows = data.MeleeCombo?.Length ?? 0;
            if (++_blow < blows) _windupLeft += BlowAt(data, _blow).Windup / AttackSpeed;
            else                 IsAttacking = false;
        }

        // A stagger or death cuts the swing off; the cooldown it started still runs.
        public void Interrupt() => IsAttacking = false;

        public void Reset()
        {
            IsAttacking = false;
            _cooldown.Reset();
        }

        private float AttackSpeed => _stats != null ? Mathf.Max(0.1f, _stats.Apply(ItemStat.FireRate, 1f)) : 1f;

        // The combo's blow, or the single plain blow an enemy without one makes.
        private static MeleeBlow BlowAt(EnemyData data, int index)
        {
            if (data.MeleeCombo != null && data.MeleeCombo.Length > 0) return data.MeleeCombo[index];

            return new MeleeBlow
            {
                Windup         = data.AttackWindup,
                DamageShare    = 1f,
                Reach          = data.AttackRange * 0.5f,
                Radius         = data.AttackRange * 0.6f,
                AppliesEffects = true,
            };
        }

        private void Strike(MeleeBlow blow, EnemyData data)
        {
            Transform self = _ai.transform;
            data.AttackSound.TryPlay(self.position);

            Vector3 center = self.position + Vector3.up + self.forward * blow.Reach;
            Reach.Gather(center, blow.Radius, _targetMask, ignore: self, shelterFrom: self.position + Vector3.up);

            float damage = (_stats != null ? _stats.Apply(ItemStat.Damage, data.AttackDamage) : data.AttackDamage) * blow.DamageShare;
            damage *= _ai.Ambush.Consume(out StatusEffectApplication[] ambushEffects);
            _ai.Reveal();
            var info = new DamageInfo(damage, source: _source,
                onHitEffects: blow.AppliesEffects ? StatusEffectApplication.Combine(data.MeleeEffects, ambushEffects) : ambushEffects);

            foreach (AreaHit hit in Reach.Hits)
                if (hit.Target is HealthManager target && target != _ai.Health) target.TakeDamage(info);
        }
    }
}
