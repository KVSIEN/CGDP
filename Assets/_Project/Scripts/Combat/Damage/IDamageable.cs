namespace CGD.Combat
{
    // Common damage surface (implemented by HealthManager), so status effects and other
    // cross-cutting systems can resolve damage without caring what they hit.
    public interface IDamageable
    {
        float Armor { get; }
        // Temporary fractional reduction (0..1) applied on top of Armor when resolving
        // damage — e.g. Ice's armor-reduction stacks. 0 = no reduction.
        float ArmorReductionPercent { get; set; }
        float MaxHealth { get; }
        void TakeDamage(DamageInfo info);
    }
}
