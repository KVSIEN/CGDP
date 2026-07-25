// Common surface PlayerStats and EnemyHealth both expose, so status effects and
// other cross-cutting systems can resolve damage without caring which one they hit.
public interface IDamageable
{
    float Armor { get; }
    float MaxHealth { get; }
    void TakeDamage(DamageInfo info);
}
