using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Perks;

namespace CGD.Weapons
{
    // Rolls a category into a concrete weapon.
    //
    // Stats that carry the weapon's *power* are sampled through the ItemRoll, so the
    // quality curve and its tradeoff axes decide where each lands inside the
    // category's authored range. Stats that only give the weapon *character* —
    // recoil recovery, heat behaviour, burst timing — stay uniformly random, because
    // tying them to quality would make high-tier weapons feel same-y rather than
    // strong.
    //
    // First the build is picked (WeaponBuild): the model (the AR's M16, AK-47…), its firing
    // variant and the category's stat style, which between them settle the name, fire mode,
    // ammo, reload, recoil pattern and the scales that stretch the category's ranges before
    // the roll samples them. The roll steps below then fill in the stats from that build.
    //
    // Everything random here comes from the roll's seed, so a weapon can be rebuilt
    // exactly from its category, tier and seed. Like the map generator, the seed splits
    // into layers (Layers), each rerollable on its own through its variant
    // (GearDefinition.RerollLayer): quality, stats (where the power stats lean), character
    // (model, name, styles, fire mode and the stats that aren't quality-driven) and perks.
    // Variant 0 of every layer is the plain seed, so existing seeds keep their weapons.
    //
    // ItemRoll.Sample draws nothing; only the character stream (`random`) does, so the steps
    // must keep drawing from it in this order or existing seeds give different weapons.
    public static class WeaponGenerator
    {
        public const string CharacterLayer = "weapon";

        public static readonly IReadOnlyList<string> Layers =
            new[] { ItemRoll.QualityLayer, ItemRoll.StatsLayer, CharacterLayer, PerkPool.PerksLayer };

        public static WeaponInstance Generate(WeaponCategoryData cat) => Generate(cat, cat.Roll());

        // Same category, tier and seed → the same weapon, name and all.
        public static WeaponInstance Generate(WeaponCategoryData cat, Seed seed) => Generate(cat, cat.Roll(cat.Tier, seed));

        public static WeaponInstance Generate(WeaponCategoryData cat, ItemRoll roll)
        {
            var d = ScriptableObject.CreateInstance<WeaponData>();
            RandomStream random = roll.StreamFor(CharacterLayer);
            WeaponBuild build   = WeaponBuild.Pick(cat, random);

            ApplyBuild(d, cat, build);
            RollFiring(d, cat, roll, random, build);
            RollProjectile(d, cat, roll, random, build);
            RollDamage(d, cat, roll, random, build);
            RollAmmo(d, cat, roll, build);
            RollSpread(d, cat, roll, random, build);
            RollRecoil(d, cat, roll, random, build);
            RollHandling(d, cat, roll, random);

            return new WeaponInstance(cat, roll, d);
        }

        // What the build settles outright, and what the category hands down unrolled.
        private static void ApplyBuild(WeaponData d, WeaponCategoryData cat, WeaponBuild build)
        {
            d.WeaponName    = build.Name;
            d.FireMode      = build.FireMode;
            d.FireBehavior  = build.FireBehavior;
            d.AmmoType      = build.AmmoType;
            d.RoundsPerLoad = build.RoundsPerLoad;

            d.OnHitEffects = cat.OnHitEffects;
            d.QuickMelee   = cat.QuickMelee;
            d.DamageType   = cat.DamageType;
            d.HitMask      = cat.HitMask;
            d.NoiseRadius  = cat.NoiseRadius;

            d.HasDrawStances = cat.HasDrawStances;
            d.VerticalDraw   = cat.VerticalDraw;
            d.HorizontalDraw = cat.HorizontalDraw;
        }

        private static void RollFiring(WeaponData d, WeaponCategoryData cat, ItemRoll roll, RandomStream random, WeaponBuild build)
        {
            d.RoundsPerMinute = roll.Sample(ItemStat.FireRate, StyleScales.Scale(cat.RPM, build.Scales.FireRate, build.Leans.FireRate));
            d.BurstCount      = cat.BurstCount.Evaluate(random);
            d.BurstInterval   = cat.BurstInterval.EvaluateClamped(random);
            d.PelletCount     = Mathf.Max(1, build.PelletCount.Evaluate(random));
        }

        // Charged shots, projectile flight and bow volleys.
        private static void RollProjectile(WeaponData d, WeaponCategoryData cat, ItemRoll roll, RandomStream random, WeaponBuild build)
        {
            StyleScales scales = build.Scales;
            StyleLeans  leans  = build.Leans;
            // A faster draw is a better bow, so charge time follows quality on the draw-speed axis.
            d.ChargeTime                 = Mathf.Max(0f, roll.Sample(ItemStat.DrawTime, StyleScales.Scale(cat.ChargeTime, scales.ChargeTime, leans.ChargeTime)));
            d.MinChargeToFire            = Mathf.Clamp01(cat.MinChargeToFire.EvaluateClamped(random));
            d.ProjectileSpeed            = Mathf.Max(0.1f, StyleScales.Scale(cat.ProjectileSpeed, scales.ProjectileSpeed, leans.ProjectileSpeed).EvaluateClamped(random));
            d.ProjectileGravity          = Mathf.Max(0f, cat.ProjectileGravity.EvaluateClamped(random));
            d.ProjectileLifetime         = Mathf.Max(0.1f, cat.ProjectileLifetime.EvaluateClamped(random));
            d.ProjectileInstantHitTime   = Mathf.Max(0f, cat.ProjectileInstantHitTime.EvaluateClamped(random));
            d.LowChargeSpeedMultiplier   = Mathf.Clamp(cat.LowChargeSpeedMultiplier.EvaluateClamped(random), 0.05f, 1f);
            d.LowChargeGravityMultiplier = Mathf.Max(1f, cat.LowChargeGravityMultiplier.EvaluateClamped(random));
            d.LowChargeDamageMultiplier  = Mathf.Clamp(cat.LowChargeDamageMultiplier.EvaluateClamped(random), 0.05f, 1f);
            d.VolleySpacingDeg           = Mathf.Max(0f, cat.VolleySpacingDeg.EvaluateClamped(random));
        }

        private static void RollDamage(WeaponData d, WeaponCategoryData cat, ItemRoll roll, RandomStream random, WeaponBuild build)
        {
            StyleScales scales = build.Scales;
            StyleLeans  leans  = build.Leans;
            d.Damage             = roll.Sample(ItemStat.Damage, StyleScales.Scale(cat.Damage, scales.Damage, leans.Damage));
            d.ArmorPenetration   = Mathf.Clamp01(roll.Sample(ItemStat.ArmorPenetration, cat.ArmorPenetration));
            d.HeadshotMultiplier = roll.Sample(ItemStat.CritDamage, cat.HeadshotMultiplier);
            d.RangeOptimal       = roll.Sample(ItemStat.Range, StyleScales.Scale(cat.RangeOptimal, scales.Range, leans.Range));
            d.RangeFalloffEnd    = Mathf.Max(d.RangeOptimal + 10f, StyleScales.Scale(cat.RangeFalloffEnd, scales.Range, leans.Range).EvaluateClamped(random));
            d.DamageFalloffMin   = cat.DamageFalloffMin.EvaluateClamped(random);
        }

        private static void RollAmmo(WeaponData d, WeaponCategoryData cat, ItemRoll roll, WeaponBuild build)
        {
            IntRange magazine = build.IsAutomatic && cat.AutomaticMagazineSize.Max > 0 ? cat.AutomaticMagazineSize : cat.MagazineSize;
            d.MagazineSize       = build.FixedMagazineSize > 0 ? build.FixedMagazineSize
                                 : roll.Sample(ItemStat.MagazineSize, StyleScales.Scale(magazine, build.Scales.MagazineSize, build.Leans.MagazineSize));
            d.ReloadTime         = roll.Sample(ItemStat.ReloadTime, cat.ReloadTime);
            d.TacticalReloadTime = Mathf.Max(0.5f, roll.Sample(ItemStat.ReloadTime, cat.TacticalReloadTime));
        }

        private static void RollSpread(WeaponData d, WeaponCategoryData cat, ItemRoll roll, RandomStream random, WeaponBuild build)
        {
            float scale = build.Scales.Spread;
            float lean  = build.Leans.Spread;
            d.HipSpreadDeg        = roll.Sample(ItemStat.Spread, StyleScales.Scale(cat.HipSpreadDeg, scale, lean));
            d.AdsSpreadDeg        = roll.Sample(ItemStat.Spread, build.AdsSpreadDeg);
            d.AdsSpreadMultiplier = cat.AdsSpreadMultiplier.EvaluateClamped(random);
            d.SpreadPerShot       = StyleScales.Scale(cat.SpreadPerShot, scale, lean).EvaluateClamped(random);
            d.MaxSpread           = roll.Sample(ItemStat.Spread, StyleScales.Scale(cat.MaxSpread, scale, lean));
            d.SpreadRecovery      = cat.SpreadRecovery.EvaluateClamped(random);
        }

        // Kick, its buildup and its recovery. The build's recoil pattern shapes the kick —
        // its sideways share and reach, lean, drift, jumpiness and muzzle flip; without one,
        // the category's random lean and WeaponData's defaults apply.
        private static void RollRecoil(WeaponData d, WeaponCategoryData cat, ItemRoll roll, RandomStream random, WeaponBuild build)
        {
            RecoilPattern pattern = build.Recoil;
            float vertical = build.Scales.Recoil;
            float sideways = vertical * (pattern?.Horizontal ?? 1f);
            float reach    = sideways * (pattern?.Reach ?? 1f);
            float lean     = build.Leans.Recoil;

            d.RecoilScale = new Vector2(roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.RecoilScaleHorizontal, sideways, lean)),
                                        roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.RecoilScaleVertical, vertical, lean)));
            d.RecoilJitter.y = cat.RecoilJitterVertical.EvaluateClamped(random);
            float categoryBias = cat.RecoilHorizontalBias.EvaluateClamped(random);   // drawn either way
            d.MaxAccumulatedRecoil           = roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.MaxAccumulatedRecoil, vertical, lean));
            d.MaxAccumulatedHorizontalRecoil = roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.MaxAccumulatedHorizontalRecoil, reach, lean));
            d.RecoilHorizontalBias = pattern?.Bias ?? categoryBias;
            if (pattern != null)
            {
                d.HorizontalDriftMode = pattern.Drift;
                d.RecoilJitter.x      = pattern.Jitter;
                d.MuzzleFlip          = pattern.MuzzleFlip;
            }

            d.RecoilHeatPerShot          = Mathf.Clamp01(cat.RecoilHeatPerShot.EvaluateClamped(random));
            d.RecoilHeatCooldown         = Mathf.Max(0f, cat.RecoilHeatCooldown.EvaluateClamped(random));
            d.MaxHeatRecoilMultiplier    = Mathf.Max(1f, cat.MaxHeatRecoilMultiplier.EvaluateClamped(random));
            d.RecoilHeatJitterMultiplier = Mathf.Max(1f, cat.RecoilHeatJitterMultiplier.EvaluateClamped(random));
            d.HotAdsSpreadMultiplier     = Mathf.Clamp01(cat.HotAdsSpreadMultiplier.EvaluateClamped(random));

            d.RecoilRecoverySpeed           = cat.RecoilRecoverySpeed.EvaluateClamped(random);
            d.RecoilRecoveryFraction        = cat.RecoilRecoveryFraction.EvaluateClamped(random);
            d.AdsRecoilRecoveryFraction     = cat.AdsRecoilRecoveryFraction.EvaluateClamped(random);
            d.RecoilRecoveryDelay           = cat.RecoilRecoveryDelay.EvaluateClamped(random);
            d.AdsRecoilMultiplier           = cat.AdsRecoilMultiplier.EvaluateClamped(random);
            d.HipRecoilVerticalMultiplier   = cat.HipRecoilVerticalMultiplier.EvaluateClamped(random);
            d.HipRecoilHorizontalMultiplier = cat.HipRecoilHorizontalMultiplier.EvaluateClamped(random);
        }

        // Draw, sway and aiming.
        private static void RollHandling(WeaponData d, WeaponCategoryData cat, ItemRoll roll, RandomStream random)
        {
            d.DrawTime = roll.Sample(ItemStat.DrawTime, cat.DrawTime);

            d.LookSwayAmount   = Mathf.Clamp01(roll.Sample(ItemStat.Sway, cat.LookSwayAmount));
            d.LookSwayRecovery = cat.LookSwayRecovery.EvaluateClamped(random);
            d.IdleSwayAmount   = roll.Sample(ItemStat.Sway, cat.IdleSwayAmount);
            d.IdleSwaySpeed    = cat.IdleSwaySpeed.EvaluateClamped(random);
            d.MoveSwayAmount   = roll.Sample(ItemStat.Sway, cat.MoveSwayAmount);

            d.AdsFovDeg = Mathf.Max(1f,   cat.AdsFovDeg.EvaluateClamped(random));
            d.AdsSpeed  = Mathf.Max(0.1f, cat.AdsSpeed.EvaluateClamped(random));
        }
    }
}
