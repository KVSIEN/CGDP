using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Meters;
using CGD.Perks;

namespace CGD.Weapons
{
    // Rolls a shield type into a concrete shield. How much it stops and its bash damage follow
    // quality (its power); stamina per blocked damage stays random (its character). Same
    // definition, tier and seed → the same shield. Its seed layers reroll on their own, as a
    // weapon's do (see WeaponGenerator).
    public static class ShieldGenerator
    {
        public const string CharacterLayer = "shield";

        public static readonly IReadOnlyList<string> Layers =
            new[] { ItemRoll.QualityLayer, ItemRoll.StatsLayer, CharacterLayer, PerkPool.PerksLayer };

        public static ShieldInstance Generate(ShieldDefinition definition, ItemRoll roll)
        {
            var d = ScriptableObject.CreateInstance<MeleeWeaponData>();
            RandomStream random = roll.StreamFor(CharacterLayer);

            d.WeaponName = random.Pick(definition.Names);

            MeleeAttackStep bash = definition.Bash.Clone();
            bash.Damage = definition.Bash.Damage * roll.Sample(ItemStat.Damage, definition.BashDamage);
            d.Bash      = bash;
            d.StaminaCost = new MeterCost { Meter = definition.StaminaMeter, Amount = Mathf.Max(0f, definition.BashStaminaCost) };
            d.HitMask     = definition.HitMask;
            d.NoiseRadius = definition.NoiseRadius;

            // Block share is authored worst → best, so the roll's Armor desirability walks it towards Max.
            GuardSettings guard = definition.Guard;
            guard.BlockDamageMultiplier = Mathf.Clamp01(roll.Sample(ItemStat.Armor, definition.BlockDamageMultiplier));
            guard.StaminaPerDamage      = Mathf.Max(0f, definition.StaminaPerDamage.EvaluateClamped(random));
            d.CanGuard     = true;
            d.Guard        = guard;
            d.ParryReflect = definition.ParryReflect;

            return new ShieldInstance(definition, roll, d);
        }
    }
}
