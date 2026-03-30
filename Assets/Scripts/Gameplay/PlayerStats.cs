using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _health = 100f;

    public float Health => _health;
    public float MaxHealth => _maxHealth;

    public event Action OnChanged;
    public event Action OnDeath;

    private float _initialHealth;

    private void Awake()
    {
        _initialHealth = _health;
    }

    public void TakeDamage(float amount)
    {
        if (_health <= 0f) return;
        _health = Mathf.Clamp(_health - amount, 0f, _maxHealth);
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
        OnChanged?.Invoke();
    }
}
