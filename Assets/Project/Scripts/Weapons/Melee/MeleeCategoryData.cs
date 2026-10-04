using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Meters;

namespace CGD.Weapons
{
    // One melee weapon type (dagger, sword…), rolled into individual weapons like a
    // WeaponCategoryData. The moveset is authored once as template steps; each roll scales
    // them: step Damage values are relative (1 = the rolled Damage), step Range is
    // multiplied by the rolled Reach. Right-click → Apply Type Defaults fills a type's band.
    [CreateAssetMenu(fileName = "MeleeCategory", menuName = "CGD/Weapons/Melee Category")]
    public class MeleeCategoryData : GearDefinition
    {
        [Header("Identity")]
        public MeleeWeaponType Type;
        public string[] Names = { "Blade" };

        [Header("Moveset (template)")]
        [Tooltip("Light combo; Damage is relative to the rolled Damage")]
        public MeleeAttackStep[] LightCombo = { new() { Damage = 1f } };
        [Tooltip("Hold-to-charge heavy attack; Damage is relative to the rolled Damage")]
        public MeleeAttackStep HeavyAttack = new() { Damage = 2.5f };
        public float HeavyHoldThreshold = 0.35f;
        public float ComboResetTime     = 1.2f;
        [Tooltip("Seconds the next combo step stays open after a dodge, parry, ability or weapon switch")]
        public float WeaveWindow        = 2.5f;

        [Header("Rolls")]
        [Tooltip("Damage of a 1× step")]
        public FloatRange Damage           = new(20f, 30f);
        [Tooltip("Swing speed multiplier: above 1 = faster wind-up, strike and recovery")]
        public FloatRange AttackSpeed      = new(1f, 1f);
        [Tooltip("× every step's range")]
        public FloatRange Reach            = new(1f, 1f);
        public FloatRange ArmorPenetration = new(0f, 0f);

        [Header("Stamina")]
        [Tooltip("Meter the swings cost. Characters without it swing for free")]
        public MeterDefinition StaminaMeter;
        [Tooltip("Per light attack")]
        public FloatRange StaminaCost = new(8f, 12f);
        public float      HeavyStaminaMultiplier = 2f;

        [Header("Guard")]
        [Tooltip("Share of a blocked hit that still gets through (lower = sturdier guard)")]
        public FloatRange    BlockDamageMultiplier = new(0.3f, 0.3f);
        [Tooltip("Seconds after raising the guard in which a hit is parried")]
        public FloatRange    ParryWindow           = new(0.2f, 0.2f);
        [Tooltip("Arc, stamina per blocked damage and parry stun (Block Damage and Parry Window are rolled above)")]
        public GuardSettings Guard                 = GuardSettings.Default;

        public LayerMask HitMask = ~0;
        [Tooltip("How far away enemies hear a swing (0 = silent)")]
        public float NoiseRadius = 8f;

        public override ItemInstance CreateInstance(ItemRoll roll) => MeleeWeaponGenerator.Generate(this, roll);

        [ContextMenu("Apply Type Defaults")]
        public void ApplyTypeDefaults() => MeleeCategoryDefaults.Apply(this);
    }
}
