using System;
using UnityEngine;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Combat
{
    // Parent of a character's Hitboxes and the single place damage is resolved:
    // region multiplier → armor → shield → health. Subclasses only supply their stats.
    public abstract class HealthManager : MonoBehaviour, IDamageable
    {
        [Tooltip("Per-region damage multipliers. Empty = every region ×1, Head still counts as critical.")]
        [SerializeField] private HitboxProfile _hitboxProfile;

        private float _currentHealth;
        private Meter _shield;
        private float _armorReductionPercent;
        private StatusEffectController _statusEffects;
        private CharacterStats _stats;

        public abstract Team  Team      { get; }
        public abstract float MaxHealth { get; }
        public abstract float Armor     { get; }
        public abstract float MaxShield { get; }
        protected abstract float ShieldRegenDelay { get; }
        protected abstract float ShieldRegenRate  { get; }

        protected virtual float   StartingHealth  => MaxHealth;
        // Feedback position for damage without a hit point, e.g. status effect ticks.
        protected virtual Vector3 DefaultHitPoint => transform.position;

        // Shown in kill confirmations and similar feedback.
        public virtual string DisplayName => name;

        // Ignores all damage while set (dev console "god").
        public bool IsInvulnerable { get; set; }

        public float Health => _currentHealth;
        public float Shield => _shield.Current;
        public bool  IsDead => _currentHealth <= 0f;
        public float ArmorReductionPercent
        {
            get => _armorReductionPercent;
            set => _armorReductionPercent = Mathf.Clamp01(value);
        }

        public event Action        OnChanged;
        public event Action        OnDeath;
        public event Action<float> OnDamaged;
        // Every hit this character accepts (after team filtering), with its source.
        public event Action<DamageInfo> OnHit;
        // Raised by Revive(), so systems on the same character can reset themselves.
        public event Action        OnRevived;

        protected virtual void Awake()
        {
            TryGetComponent(out _statusEffects);
            TryGetComponent(out _stats);
            // The shield is a regenerating resource like any other, so it runs on Meter.
            _shield = new Meter(new MeterSettings(MaxShield, ShieldRegenRate, ShieldRegenDelay));
            ResetHealth();
        }

        private void Update()
        {
            if (_shield.Tick(Time.deltaTime)) OnChanged?.Invoke();
        }

        public void TakeDamage(DamageInfo info) => ApplyDamage(info, 1f, DefaultHitPoint, false);

        public void TakeHit(DamageInfo info, HitboxRegion region, Vector3 point)
        {
            float multiplier = HitboxProfile.Resolve(_hitboxProfile, region, info.CriticalMultiplier, out bool isCritical);
            ApplyDamage(info, multiplier, point, isCritical);
        }

        public void Heal(float amount)
        {
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, MaxHealth);
            OnChanged?.Invoke();
        }

        public void Revive()
        {
            ResetHealth();
            OnRevived?.Invoke();
        }

        // Teammates (and the source itself) are immune; teamless sources hit everyone.
        public bool CanBeDamagedBy(DamageSource source) => source.Team == Team.None || source.Team != Team;

        private void ResetHealth()
        {
            _currentHealth = StartingHealth;
            _shield.Fill();
            OnChanged?.Invoke();
        }

        protected virtual void OnDamageTaken(float amount, Vector3 point, bool isCritical) { }

        // A base value with this character's stat modifiers (buffs, difficulty) applied,
        // for subclasses' MaxHealth/Armor. Unchanged when there's no CharacterStats.
        protected float WithModifiers(ItemStat stat, float baseValue) =>
            _stats != null ? _stats.Apply(stat, baseValue) : baseValue;

        private void ApplyDamage(DamageInfo info, float multiplier, Vector3 point, bool isCritical)
        {
            if (IsDead || IsInvulnerable || !CanBeDamagedBy(info.Source)) return;

            OnHit?.Invoke(info);

            float amount = info.ResolveDamage(Armor * (1f - _armorReductionPercent)) * multiplier;
            float dealt  = amount;
            _shield.SuppressRegen();

            if (!_shield.IsEmpty)
            {
                // Lightning hits the shield harder; the extra bite is undone before
                // computing what carries over so only the shield portion is boosted.
                float shieldMult = info.Type == DamageType.Lightning ? DamageInfo.LightningShieldBonus : 1f;
                float absorbed   = Mathf.Min(_shield.Current, amount * shieldMult);
                _shield.Drain(absorbed);
                amount -= absorbed / shieldMult;
            }

            if (amount > 0f)
            {
                _currentHealth = Mathf.Max(_currentHealth - amount, 0f);
                OnDamaged?.Invoke(amount);
                OnDamageTaken(amount, point, isCritical);
            }

            OnChanged?.Invoke();
            CombatEvents.Report(new DamageReport(this, info.Source, dealt, point, isCritical, IsDead));
            if (IsDead)
            {
                OnDeath?.Invoke();
                return;
            }

            ApplyOnHitEffects(info);
        }

        private void ApplyOnHitEffects(in DamageInfo info)
        {
            if (_statusEffects == null || info.OnHitEffects == null) return;

            foreach (StatusEffectApplication application in info.OnHitEffects)
            {
                if (application.Effect != null && UnityEngine.Random.value < application.Chance)
                    _statusEffects.Apply(application.Effect, info);
            }
        }
    }
}
