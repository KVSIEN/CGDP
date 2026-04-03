using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyData      _data;
    [SerializeField] private EnemyHealthBar _healthBar;

    private float _health;

    public float Health    => _health;
    public float MaxHealth => _data.MaxHealth;

    public event Action           OnDeath;
    public event Action<float, float> OnDamaged;

    private void Awake()
    {
        _health = _data.MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        TakeDamageAt(amount, transform.position + Vector3.up * 1.5f, false);
    }

    public void TakeDamageAt(float amount, Vector3 worldPos, bool headshot)
    {
        if (_health <= 0f) return;
        _health = Mathf.Max(_health - amount, 0f);
        DamagePopup.Spawn(amount, worldPos, headshot);
        _healthBar?.ShowDamage(_health, _data.MaxHealth);
        OnDamaged?.Invoke(_health, _data.MaxHealth);
        if (_health <= 0f) OnDeath?.Invoke();
    }
}
