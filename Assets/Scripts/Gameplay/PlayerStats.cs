using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _health = 100f;

    [Header("Armor")]
    [SerializeField] private float _armor = 0f;

    [Header("Shield")]
    [SerializeField] private float _maxShield = 0f;
    [Tooltip("Seconds without taking damage before shield starts regenerating")]
    [SerializeField] private float _shieldRegenDelay = 5f;
    [Tooltip("Shield points restored per second once regen starts")]
    [SerializeField] private float _shieldRegenRate = 10f;

    public float Health => _health;
    public float MaxHealth => _maxHealth;
    public float Armor => _armor;
    public float ArmorReductionPercent
    {
        get => _armorReductionPercent;
        set => _armorReductionPercent = Mathf.Clamp01(value);
    }
    public float Shield => _shield;
    public float MaxShield => _maxShield;

    public event Action OnChanged;
    public event Action OnDeath;
    public event Action<float> OnDamaged;

    private float _initialHealth;
    private float _shield;
    private float _shieldRegenTimer;
    private float _armorReductionPercent;

    private void Awake()
    {
        _initialHealth = _health;
        _shield = _maxShield;
    }

    private void Update()
    {
        TickShieldRegen(Time.deltaTime);
    }

    public void TakeDamage(DamageInfo info)
    {
        if (_health <= 0f) return;

        float amount = info.ResolveDamage(_armor * (1f - _armorReductionPercent));
        _shieldRegenTimer = _shieldRegenDelay;

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
            _health = Mathf.Clamp(_health - amount, 0f, _maxHealth);
            OnDamaged?.Invoke(amount);
        }

        OnChanged?.Invoke();
        if (_health <= 0f) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        _health = Mathf.Clamp(_health + amount, 0f, _maxHealth);
        OnChanged?.Invoke();
    }

    public void Respawn()
    {
        _health = _initialHealth;
        _shield = _maxShield;
        OnChanged?.Invoke();
    }

    private void TickShieldRegen(float deltaTime)
    {
        if (_shield >= _maxShield) return;

        if (_shieldRegenTimer > 0f)
        {
            _shieldRegenTimer -= deltaTime;
            return;
        }

        _shield = Mathf.Min(_shield + _shieldRegenRate * deltaTime, _maxShield);
        OnChanged?.Invoke();
    }
}
