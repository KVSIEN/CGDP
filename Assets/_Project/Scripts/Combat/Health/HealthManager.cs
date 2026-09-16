using System;
using UnityEngine;

// Parent of a character's Hitboxes and the single place damage is resolved:
// region multiplier → armor → shield → health. Subclasses only supply their stats.
public abstract class HealthManager : MonoBehaviour, IDamageable
{
    [Tooltip("Per-region damage multipliers. Empty = every region ×1, Head still counts as critical.")]
    [SerializeField] private HitboxProfile _hitboxProfile;

    private float _currentHealth;
    private float _shield;
    private float _shieldRegenTimer;
    private float _armorReductionPercent;

    public abstract float MaxHealth { get; }
    public abstract float Armor     { get; }
    public abstract float MaxShield { get; }
    protected abstract float ShieldRegenDelay { get; }
    protected abstract float ShieldRegenRate  { get; }

    protected virtual float   StartingHealth  => MaxHealth;
    // Feedback position for damage without a hit point, e.g. status effect ticks.
    protected virtual Vector3 DefaultHitPoint => transform.position;

    public float Health => _currentHealth;
    public float Shield => _shield;
    public bool  IsDead => _currentHealth <= 0f;
    public float ArmorReductionPercent
    {
        get => _armorReductionPercent;
        set => _armorReductionPercent = Mathf.Clamp01(value);
    }

    public event Action        OnChanged;
    public event Action        OnDeath;
    public event Action<float> OnDamaged;

    protected virtual void Awake() => ResetHealth();

    private void Update() => TickShieldRegen(Time.deltaTime);

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

    protected void ResetHealth()
    {
        _currentHealth = StartingHealth;
        _shield        = MaxShield;
        OnChanged?.Invoke();
    }

    protected virtual void OnDamageTaken(float amount, Vector3 point, bool isCritical) { }

    private void ApplyDamage(DamageInfo info, float multiplier, Vector3 point, bool isCritical)
    {
        if (IsDead) return;

        float amount = info.ResolveDamage(Armor * (1f - _armorReductionPercent)) * multiplier;
        _shieldRegenTimer = ShieldRegenDelay;

        if (_shield > 0f)
        {
            // Lightning hits the shield harder; the extra bite is undone before
            // computing what carries over so only the shield portion is boosted.
            float shieldMult = info.Type == DamageType.Lightning ? DamageInfo.LightningShieldBonus : 1f;
            float absorbed   = Mathf.Min(_shield, amount * shieldMult);
            _shield -= absorbed;
            amount  -= absorbed / shieldMult;
        }

        if (amount > 0f)
        {
            _currentHealth = Mathf.Max(_currentHealth - amount, 0f);
            OnDamaged?.Invoke(amount);
            OnDamageTaken(amount, point, isCritical);
        }

        OnChanged?.Invoke();
        if (IsDead) OnDeath?.Invoke();
    }

    private void TickShieldRegen(float deltaTime)
    {
        if (_shield >= MaxShield) return;

        if (_shieldRegenTimer > 0f)
        {
            _shieldRegenTimer -= deltaTime;
            return;
        }

        _shield = Mathf.Min(_shield + ShieldRegenRate * deltaTime, MaxShield);
        OnChanged?.Invoke();
    }
}
