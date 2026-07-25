using UnityEngine;

// Carries a hit through the single mitigation step shared by weapons, abilities,
// and status ticks, so every damage source resolves against armor the same way.
public readonly struct DamageInfo
{
    // Shared by PlayerStats/EnemyHealth's shield-absorption step.
    public const float LightningShieldBonus = 1.5f;

    public readonly float RawDamage;
    public readonly float ArmorPenetration; // 0..1
    public readonly DamageType Type;

    public DamageInfo(float rawDamage, float armorPenetration = 0f, DamageType type = DamageType.Physical)
    {
        RawDamage        = rawDamage;
        ArmorPenetration = Mathf.Clamp01(armorPenetration);
        Type             = type;
    }

    // Effective Armor = Armor × (1 − Armor Penetration%)
    // Effective Damage = Raw Damage × (100 / (100 + Effective Armor))
    public float ResolveDamage(float armor)
    {
        if (Type == DamageType.True) return RawDamage;

        float effectiveArmor = Mathf.Max(0f, armor) * (1f - ArmorPenetration);
        return RawDamage * (100f / (100f + effectiveArmor));
    }
}
