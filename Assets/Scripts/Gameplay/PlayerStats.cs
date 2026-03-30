using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _health = 100f;

    [Header("Ammo")]
    [SerializeField] private int _magazineSize = 30;
    [SerializeField] private int _ammo = 30;
    [SerializeField] private int _ammoReserve = 90;

    public float Health => _health;
    public float MaxHealth => _maxHealth;
    public int Ammo => _ammo;
    public int AmmoReserve => _ammoReserve;
    public int MagazineSize => _magazineSize;

    public event Action OnChanged;
    public event Action OnDeath;

    private float _initialHealth;
    private int   _initialAmmo;
    private int   _initialReserve;

    private void Awake()
    {
        _initialHealth  = _health;
        _initialAmmo    = _ammo;
        _initialReserve = _ammoReserve;
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

    public void UseAmmo(int count = 1)
    {
        _ammo = Mathf.Max(_ammo - count, 0);
        OnChanged?.Invoke();
    }

    public void Reload()
    {
        int needed = _magazineSize - _ammo;
        int taken = Mathf.Min(needed, _ammoReserve);
        _ammo += taken;
        _ammoReserve -= taken;
        OnChanged?.Invoke();
    }

    public void Respawn()
    {
        _health     = _initialHealth;
        _ammo       = _initialAmmo;
        _ammoReserve = _initialReserve;
        OnChanged?.Invoke();
    }
}
