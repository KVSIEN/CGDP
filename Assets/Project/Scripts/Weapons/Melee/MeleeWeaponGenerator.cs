using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Meters;
using CGD.Perks;

namespace CGD.Weapons
{
    // Rolls a melee category into a concrete weapon. Damage, attack speed and armour
    // penetration follow the quality roll (the weapon's power); reach, stamina cost and
    // guard stay uniformly random (its character). Same category, tier and seed → the
    // same weapon. Its seed layers reroll on their own, as a firearm's do (see WeaponGenerator).
    //
    // A category with Models first picks one (from its own fork of the character stream, so
    // categories without models roll as they always have): its name, its grip, and scales
    // that stretch each range before it's rolled, with the stats leaning toward the model's
    // side of each range the way a firearm model's do.
    public static class MeleeWeaponGenerator
    {
        public const string CharacterLayer = "melee";

        public static readonly IReadOnlyList<string> Layers =
            new[] { ItemRoll.QualityLayer, ItemRoll.StatsLayer, CharacterLayer, PerkPool.PerksLayer };

        public static MeleeWeaponInstance Generate(MeleeCategoryData cat) => Generate(cat, cat.Roll());

        public static MeleeWeaponInstance Generate(MeleeCategoryData cat, Seed seed) => Generate(cat, cat.Roll(cat.Tier, seed));

        public static MeleeWeaponInstance Generate(MeleeCategoryData cat, ItemRoll roll)
        {
            var d = ScriptableObject.CreateInstance<MeleeWeaponData>();
            RandomStream random = roll.StreamFor(CharacterLayer);
            MeleeModel   model  = cat.Models.Length > 0 ? random.Fork("model").PickWeighted(cat.Models, m => m.Weight) : null;
            MeleeScales  scales = model != null ? model.Scales : MeleeScales.Identity;

            float damage = roll.Sample(ItemStat.Damage, Scaled(cat.Damage, scales.Damage, model));
            float pen    = Mathf.Clamp01(roll.Sample(ItemStat.ArmorPenetration, Scaled(cat.ArmorPenetration, scales.ArmorPenetration, model)));
            float reach  = Mathf.Max(0.1f, Scaled(cat.Reach, scales.Reach, model).EvaluateClamped(random));

            d.WeaponName = model != null ? model.Name : random.Pick(cat.Names);
            d.OneHanded  = model == null || model.Grip == MeleeGrip.AsType ? cat.OneHanded : model.Grip == MeleeGrip.OneHanded;
            d.LightCombo = new MeleeAttackStep[cat.LightCombo.Length];
            for (int i = 0; i < cat.LightCombo.Length; i++)
                d.LightCombo[i] = Scale(cat.LightCombo[i], damage, reach, pen);
            d.HeavyAttack        = Scale(cat.HeavyAttack, damage, reach, pen);
            d.Bash               = Scale(cat.Bash, damage, reach, pen);
            d.HeavyHoldThreshold = cat.HeavyHoldThreshold;
            d.ComboResetTime     = cat.ComboResetTime;
            d.WeaveWindow        = cat.WeaveWindow;
            d.HitMask            = cat.HitMask;
            d.NoiseRadius        = cat.NoiseRadius;

            d.AttackSpeed            = Mathf.Max(0.1f, roll.Sample(ItemStat.FireRate, Scaled(cat.AttackSpeed, scales.AttackSpeed, model)));
            d.StaminaCost            = new MeterCost { Meter = cat.StaminaMeter, Amount = Mathf.Max(0f, Scaled(cat.StaminaCost, scales.StaminaCost, model).EvaluateClamped(random)) };
            d.HeavyStaminaMultiplier = cat.HeavyStaminaMultiplier;

            GuardSettings guard = cat.Guard;
            guard.BlockDamageMultiplier = Mathf.Clamp01(Scaled(cat.BlockDamageMultiplier, scales.BlockDamage, model).EvaluateClamped(random));
            guard.ParryWindow           = Mathf.Max(0f, Scaled(cat.ParryWindow, scales.ParryWindow, model).EvaluateClamped(random));
            d.CanGuard     = true;
            d.Guard        = guard;
            d.ParryReflect = cat.ParryReflect;

            return new MeleeWeaponInstance(cat, roll, d);
        }

        // The range stretched by a model's scale and leaning its way; untouched without a model.
        private static FloatRange Scaled(FloatRange range, float scale, MeleeModel model) =>
            model != null ? StyleScales.Scale(range, scale, StyleLeans.LeanOf(scale)) : range;

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
