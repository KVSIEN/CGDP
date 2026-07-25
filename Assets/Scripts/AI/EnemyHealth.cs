using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData      _data;
    [SerializeField] private EnemyHealthBar _healthBar;
    [SerializeField] private Vector3        _popupOffset = new Vector3(0f, 0.3f, 0f);

    private float _health;
    private float _shield;
    private float _shieldRegenTimer;

    public float Health    => _health;
    public float MaxHealth => _data.MaxHealth;
    public float Armor     => _data.Armor;
    public float Shield    => _shield;
    public float MaxShield => _data.MaxShield;

    public event Action           OnDeath;
    public event Action<float, float> OnDamaged;

    private void Awake()
    {
        _health = _data.MaxHealth;
        _shield = _data.MaxShield;
    }

    private void Update()
    {
        TickShieldRegen(Time.deltaTime);
    }

    public void TakeDamage(DamageInfo info)
    {
        TakeDamageAt(info, transform.position + Vector3.up * 1.5f, false);
    }

    public void TakeDamageAt(DamageInfo info, Vector3 worldPos, bool headshot)
    {
        if (_health <= 0f) return;

        float amount = info.ResolveDamage(_data.Armor);
        _shieldRegenTimer = _data.ShieldRegenDelay;

        if (_shield > 0f)
        {
            float absorbed = Mathf.Min(_shield, amount);
            _shield -= absorbed;
            amount  -= absorbed;
        }

        if (amount > 0f)
        {
            _health = Mathf.Max(_health - amount, 0f);
            DamagePopup.Spawn(amount, worldPos + _popupOffset, headshot);
            _healthBar?.ShowDamage(_health, _data.MaxHealth);
            OnDamaged?.Invoke(_health, _data.MaxHealth);
        }

        if (_health <= 0f) OnDeath?.Invoke();
    }

    private void TickShieldRegen(float deltaTime)
    {
        if (_shield >= _data.MaxShield) return;

        if (_shieldRegenTimer > 0f)
        {
            _shieldRegenTimer -= deltaTime;
            return;
        }

        _shield = Mathf.Min(_shield + _data.ShieldRegenRate * deltaTime, _data.MaxShield);
    }
}
