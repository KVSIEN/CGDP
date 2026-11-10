using System;
using UnityEngine;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Combat
{
    // Parent of a character's Hitboxes and the single place damage is resolved:
    // region multiplier → armor → interceptors (a raised guard) → shield → health.
    // Subclasses only supply their stats.
    public abstract class HealthManager : MonoBehaviour, IDamageable
    {
        [Tooltip("Per-region damage multipliers. Empty = every region ×1, Head still counts as critical.")]
        [SerializeField] private HitboxProfile _hitboxProfile;

        private float _currentHealth;
        private Meter _shield;
        private float _armorReductionPercent;
        private StatusEffectController _statusEffects;
        private IDamageInterceptor[]   _interceptors = System.Array.Empty<IDamageInterceptor>();
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

        // Brief invulnerability (dodge i-frames). Counted, so overlapping sources don't end
        // each other's, and kept apart from IsInvulnerable so god mode survives a dodge.
        private int _invulnerabilityHolds;
        public void AddInvulnerability() => _invulnerabilityHolds++;
        public void RemoveInvulnerability() => _invulnerabilityHolds = Mathf.Max(0, _invulnerabilityHolds - 1);

        public float Health => _currentHealth;
        public float Shield => _shield.Current;
        public bool  IsDead => _currentHealth <= 0f;

        // The latest hit that got through (team and invulnerability checks) — lets death
        // reactions such as a ragdoll fall away from whoever landed it.
        public DamageSource LastHitSource { get; private set; }
        public Vector3      LastHitPoint  { get; private set; }

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
        // Health restored by Heal() (not by regeneration), with the amount actually restored.
        public event Action<float> OnHealed;

        protected virtual void Awake()
        {
            TryGetComponent(out _statusEffects);
            TryGetComponent(out _stats);
            _interceptors = GetComponents<IDamageInterceptor>();
            System.Array.Sort(_interceptors, (a, b) => a.Order.CompareTo(b.Order));
            // The shield is a regenerating resource like any other, so it runs on Meter.
            _shield = new Meter(new MeterSettings(MaxShield, ShieldRegenRate, ShieldRegenDelay));
            if (_stats != null) _stats.Modifiers.Changed += OnStatChanged;
            ResetHealth();
        }

        protected virtual void OnDestroy()
        {
            if (_stats != null) _stats.Modifiers.Changed -= OnStatChanged;
        }

        // Gear and buffs that add shield capacity take effect at once, keeping what's charged.
        private void OnStatChanged(ItemStat stat)
        {
            if (stat == ItemStat.Shield) _shield.SetMax(MaxShield, keepRatio: false);
        }

        protected virtual void Update()
        {
            if (TickShield(Time.deltaTime)) OnChanged?.Invoke();
            Regenerate(Time.deltaTime);
        }

        // The shield's own regen, plus what gear and buffs add to its rate (ShieldRegenRate
        // stat) while it is recharging — the delay after a hit stays as it is.
        private bool TickShield(float deltaTime)
        {
            float before = _shield.Current;
            if (!_shield.Tick(deltaTime)) return false;

            float bonus = WithModifiers(ItemStat.ShieldRegenRate, ShieldRegenRate) - ShieldRegenRate;
            if (bonus > 0f) _shield.Restore(bonus * deltaTime);
            if (_shield.Current > before) OnRestored(0f, _shield.Current - before);
            return true;
        }

        // Health regeneration per second from gear and buffs (HealthRegen stat).
        private void Regenerate(float deltaTime)
        {
            if (_stats == null || IsDead || _currentHealth >= MaxHealth) return;

            float regen = _stats.Apply(ItemStat.HealthRegen, 0f);
            if (regen <= 0f) return;

            float before = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + regen * deltaTime, MaxHealth);
            OnChanged?.Invoke();
            OnRestored(_currentHealth - before, 0f);
        }

        public void TakeDamage(DamageInfo info) =>
            ApplyDamage(info, info.Bonuses.ForceCritical ? info.CriticalMultiplier : 1f, DefaultHitPoint, info.Bonuses.ForceCritical);

        public void TakeHit(DamageInfo info, HitboxRegion region, Vector3 point)
        {
            float multiplier = HitboxProfile.Resolve(_hitboxProfile, region, info.CriticalMultiplier, out bool isCritical);
            if (info.Bonuses.ForceCritical && !isCritical)
            {
                multiplier *= info.CriticalMultiplier;
                isCritical  = true;
            }
            ApplyDamage(info, multiplier, point, isCritical);
        }

        public void Heal(float amount)
        {
            float before = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, MaxHealth);
            OnChanged?.Invoke();
            if (_currentHealth <= before) return;

            OnHealed?.Invoke(_currentHealth - before);
            OnRestored(_currentHealth - before, 0f);
        }

        // Gives up up to this much shield on purpose (a discharge) and returns how much it was.
        // The shield then waits out its regen delay, as after a hit.
        public float DrainShield(float amount)
        {
            float drained = Mathf.Min(Mathf.Max(0f, amount), _shield.Current);
            if (IsDead || drained <= 0f) return 0f;

            _shield.Drain(drained);
            _shield.SuppressRegen();
            OnChanged?.Invoke();
            return drained;
        }

        // Puts shield back (a siphon). Doesn't touch the regen delay: what was taken from someone
        // else shouldn't keep this shield from recharging as well.
        public void RestoreShield(float amount)
        {
            if (IsDead || amount <= 0f) return;

            float before = _shield.Current;
            _shield.Restore(amount);
            OnChanged?.Invoke();
            if (_shield.Current > before) OnRestored(0f, _shield.Current - before);
        }

        // Sets health and shield straight to these shares of their maxima, without a hit (test
        // scenarios: "start wounded", "start with the shield broken"). Health stays above 0;
        // a lowered shield waits out its regen delay, as after a hit.
        public void SetShares(float health, float shield)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Clamp(MaxHealth * Mathf.Clamp01(health), Mathf.Min(1f, MaxHealth), MaxHealth);
            float target = MaxShield * Mathf.Clamp01(shield);
            if (target < _shield.Current)
            {
                _shield.Drain(_shield.Current - target);
                _shield.SuppressRegen();
            }
            else _shield.Restore(target - _shield.Current);
            OnChanged?.Invoke();
        }

        public void Revive()
        {
            ResetHealth();
            OnRevived?.Invoke();
        }

        // Teammates (and the source itself) are immune; teamless sources hit everyone.
        public bool CanBeDamagedBy(DamageSource source) => source.Team == Team.None || source.Team != Team;

        // Re-reads the maxima (e.g. after an enemy's stats changed) and fills health and shield.
        protected void RefillToMax()
        {
            _shield.SetMax(MaxShield, keepRatio: false);
            ResetHealth();
        }

        private void ResetHealth()
        {
            _currentHealth = StartingHealth;
            _shield.Fill();
            OnChanged?.Invoke();
        }

        // Every hit that lands, shield and health together; called after health and shield are updated.
        protected virtual void OnDamageTaken(float amount, Vector3 point, bool isCritical) { }

        // Health and shield actually gained, from any source: heals, regeneration, a siphon.
        // Not raised by Revive, refills or test scenarios setting them directly.
        protected virtual void OnRestored(float health, float shield) { }

        // A base value with this character's stat modifiers (buffs, difficulty) applied,
        // for subclasses' MaxHealth/Armor. Unchanged when there's no CharacterStats.
        protected float WithModifiers(ItemStat stat, float baseValue) =>
            _stats != null ? _stats.Apply(stat, baseValue) : baseValue;

        private void ApplyDamage(DamageInfo info, float multiplier, Vector3 point, bool isCritical)
        {
            if (IsDead || IsInvulnerable || _invulnerabilityHolds > 0 || !CanBeDamagedBy(info.Source)) return;

            LastHitSource = info.Source;
            LastHitPoint  = point;
            OnHit?.Invoke(info);

            float resolved = info.ResolveDamage(Armor * (1f - _armorReductionPercent)) * multiplier;
            float amount   = resolved;
            foreach (IDamageInterceptor interceptor in _interceptors)
                amount = Mathf.Max(0f, interceptor.Intercept(info, amount, point));
            // A hit an interceptor stopped outright (a parry) carries no status effects either.
            bool deflected = resolved > 0f && amount <= 0f;
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
            }

            OnChanged?.Invoke();
            // Damage the shield soaked up counts too, so hits on a shielded enemy still show a number.
            if (dealt > 0f) OnDamageTaken(dealt, point, isCritical);
            CombatEvents.Report(new DamageReport(this, info.Source, dealt, point, isCritical, IsDead));
            if (IsDead)
            {
                OnDeath?.Invoke();
                return;
            }

            if (!deflected) ApplyOnHitEffects(info);
        }

        private void ApplyOnHitEffects(in DamageInfo info)
        {
            if (_statusEffects == null || info.OnHitEffects == null) return;

            // The attacker's status chance makes effects likelier; this character's status
            // resistance makes them less likely. Status damage strengthens what lands.
            float resistance = Mathf.Clamp01(WithModifiers(ItemStat.StatusResistance, 0f));
            float chanceScale = (1f + info.Bonuses.StatusChance) * (1f - resistance);
            DamageInfo carried = info.Bonuses.StatusDamage != 0f
                ? info.WithDamageScale(Mathf.Max(0f, 1f + info.Bonuses.StatusDamage))
                : info;

            foreach (StatusEffectApplication application in info.OnHitEffects)
            {
                if (application.Effect != null && UnityEngine.Random.value < application.Chance * chanceScale)
                    _statusEffects.Apply(application.Effect, carried);
            }
        }
    }
}
