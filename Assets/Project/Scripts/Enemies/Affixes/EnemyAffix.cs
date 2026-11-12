using UnityEngine;

namespace CGD.Enemies
{
    // A random trait an enemy can roll on spawn, named in front of it ("Armored Husk").
    // Placeholder affixes are stat changes; behaviour-changing ones come later.
    [CreateAssetMenu(fileName = "EnemyAffix", menuName = "CGD/Enemies/Enemy Affix")]
    public class EnemyAffix : ScriptableObject
    {
        [Tooltip("Shown in front of the enemy's name")]
        public string Prefix = "Affixed";

        [Header("Stats")]
        [Min(0f)] public float HealthMultiplier = 1f;
        [Tooltip("Flat armour added")]
        public float ArmorBonus;
        [Tooltip("Flat shield added (gives shieldless enemies a shield)")]
        public float ShieldBonus;
        [Min(0f)] public float DamageMultiplier = 1f;
        [Tooltip("× patrol and chase speed")]
        [Min(0f)] public float SpeedMultiplier = 1f;

        public void ApplyTo(EnemyData data)
        {
            data.MaxHealth    *= HealthMultiplier;
            data.Armor        += ArmorBonus;
            data.MaxShield    += ShieldBonus;
            data.AttackDamage *= DamageMultiplier;
            data.PatrolSpeed  *= SpeedMultiplier;
            data.ChaseSpeed   *= SpeedMultiplier;
            data.DisplayName   = $"{Prefix} {data.DisplayName}";
        }
    }
}
