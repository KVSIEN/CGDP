using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Meters;

namespace CGD.Weapons
{
    // Rolls a melee category into a concrete weapon. Damage, attack speed and armour
    // penetration follow the quality roll (the weapon's power); reach, stamina cost and
    // guard stay uniformly random (its character). Same category, tier and seed → the
    // same weapon.
    public static class MeleeWeaponGenerator
    {
        private const string CharacterLayer = "melee";

        public static MeleeWeaponInstance Generate(MeleeCategoryData cat) => Generate(cat, cat.Roll());

        public static MeleeWeaponInstance Generate(MeleeCategoryData cat, Seed seed) => Generate(cat, cat.Roll(cat.Tier, seed));

        public static MeleeWeaponInstance Generate(MeleeCategoryData cat, ItemRoll roll)
        {
            var d = ScriptableObject.CreateInstance<MeleeWeaponData>();
            RandomStream random = roll.Seed.Derive(CharacterLayer).Stream();

            float damage = roll.Sample(ItemStat.Damage, cat.Damage);
            float pen    = Mathf.Clamp01(roll.Sample(ItemStat.ArmorPenetration, cat.ArmorPenetration));
            float reach  = Mathf.Max(0.1f, cat.Reach.EvaluateClamped(random));

            d.WeaponName = random.Pick(cat.Names);
            d.LightCombo = new MeleeAttackStep[cat.LightCombo.Length];
            for (int i = 0; i < cat.LightCombo.Length; i++)
                d.LightCombo[i] = Scale(cat.LightCombo[i], damage, reach, pen);
            d.HeavyAttack        = Scale(cat.HeavyAttack, damage, reach, pen);
            d.HeavyHoldThreshold = cat.HeavyHoldThreshold;
            d.ComboResetTime     = cat.ComboResetTime;
            d.WeaveWindow        = cat.WeaveWindow;
            d.SwapStrikeWindow   = cat.SwapStrikeWindow;
            d.HitMask            = cat.HitMask;
            d.NoiseRadius        = cat.NoiseRadius;

            d.AttackSpeed            = Mathf.Max(0.1f, roll.Sample(ItemStat.FireRate, cat.AttackSpeed));
            d.StaminaCost            = new MeterCost { Meter = cat.StaminaMeter, Amount = Mathf.Max(0f, cat.StaminaCost.EvaluateClamped(random)) };
            d.HeavyStaminaMultiplier = cat.HeavyStaminaMultiplier;

            GuardSettings guard = cat.Guard;
            guard.BlockDamageMultiplier = Mathf.Clamp01(cat.BlockDamageMultiplier.EvaluateClamped(random));
            guard.ParryWindow           = Mathf.Max(0f, cat.ParryWindow.EvaluateClamped(random));
            d.CanGuard     = true;
            d.Guard        = guard;
            d.ParryReflect = cat.ParryReflect;

            return new MeleeWeaponInstance(cat, roll, d);
        }

        private static MeleeAttackStep Scale(MeleeAttackStep template, float damage, float reach, float pen)
        {
            MeleeAttackStep step = template.Clone();
            step.Damage           = template.Damage * damage;
            step.Range            = template.Range * reach;
            step.ArmorPenetration = Mathf.Clamp01(template.ArmorPenetration + pen);
            return step;
        }
    }
}
