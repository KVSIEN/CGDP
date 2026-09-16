using UnityEngine;

public class EnemyHealth : HealthManager
{
    [SerializeField] private EnemyData      _data;
    [SerializeField] private EnemyHealthBar _healthBar;
    [SerializeField] private Vector3        _popupOffset = new Vector3(0f, 0.3f, 0f);

    public override float MaxHealth => _data.MaxHealth;
    public override float Armor     => _data.Armor;
    public override float MaxShield => _data.MaxShield;
    protected override float ShieldRegenDelay => _data.ShieldRegenDelay;
    protected override float ShieldRegenRate  => _data.ShieldRegenRate;

    protected override Vector3 DefaultHitPoint => transform.position + Vector3.up * 1.5f;

    protected override void OnDamageTaken(float amount, Vector3 point, bool isCritical)
    {
        DamagePopup.Spawn(amount, point + _popupOffset, isCritical);
        _healthBar?.ShowDamage(Health, MaxHealth);
    }
}
