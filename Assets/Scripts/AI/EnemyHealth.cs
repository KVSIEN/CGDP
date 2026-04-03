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
        if (_health <= 0f) return;
        _health = Mathf.Max(_health - amount, 0f);
        _healthBar?.ShowDamage(_health, _data.MaxHealth);
        OnDamaged?.Invoke(_health, _data.MaxHealth);
        if (_health <= 0f) OnDeath?.Invoke();
    }
}
