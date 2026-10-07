using UnityEngine;
using CGD.Core;
using CGD.Items;

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
    // A styled category (e.g. the Modular Pistol) also picks a FiringStyle and a StatStyle;
    // their scales stretch the category's ranges before the roll samples them.
    //
    // Everything random here comes from the roll's seed, so a weapon can be rebuilt
    // exactly from its category, tier and seed.
    public static class WeaponGenerator
    {
        private const string CharacterLayer = "weapon";

        public static WeaponInstance Generate(WeaponCategoryData cat) => Generate(cat, cat.Roll());

        // Same category, tier and seed → the same weapon, name and all.
        public static WeaponInstance Generate(WeaponCategoryData cat, Seed seed) => Generate(cat, cat.Roll(cat.Tier, seed));

        public static WeaponInstance Generate(WeaponCategoryData cat, ItemRoll roll)
        {
            var d = ScriptableObject.CreateInstance<WeaponData>();
            RandomStream random = roll.Seed.Derive(CharacterLayer).Stream();

            // Unstyled categories draw exactly as before, so their seeds still give the same weapons.
            string name        = random.Pick(cat.Names);
            FiringStyle firing = PickStyle(cat.FiringStyles, s => s.Weight, random);
            d.FireMode         = firing != null ? firing.FireMode : random.Pick(cat.FireModes);
            StatStyle lean     = PickStyle(cat.StatStyles, s => s.Weight, random);
            StyleScales scales = (firing != null ? firing.Scales : StyleScales.Identity)
                               * (lean   != null ? lean.Scales   : StyleScales.Identity);

            d.WeaponName   = StyledName(name, firing, lean);
            d.FireBehavior = firing != null && firing.FireBehavior != null ? firing.FireBehavior : cat.FireBehavior;
            d.OnHitEffects = cat.OnHitEffects;
            d.QuickMelee   = cat.QuickMelee;

            d.RoundsPerMinute = roll.Sample(ItemStat.FireRate, StyleScales.Scale(cat.RPM, scales.FireRate));
            d.BurstCount      = cat.BurstCount.Evaluate(random);
            d.BurstInterval   = cat.BurstInterval.EvaluateClamped(random);
            IntRange pellets  = firing != null && firing.PelletCount.Max > 0 ? firing.PelletCount : cat.PelletCount;
            d.PelletCount     = Mathf.Max(1, pellets.Evaluate(random));

            // A faster draw is a better bow, so charge time follows quality on the draw-speed axis.
            d.ChargeTime                 = Mathf.Max(0f, roll.Sample(ItemStat.DrawTime, cat.ChargeTime));
            d.MinChargeToFire            = Mathf.Clamp01(cat.MinChargeToFire.EvaluateClamped(random));
            d.ProjectileSpeed            = Mathf.Max(0.1f, cat.ProjectileSpeed.EvaluateClamped(random));
            d.ProjectileGravity          = Mathf.Max(0f, cat.ProjectileGravity.EvaluateClamped(random));
            d.ProjectileLifetime         = Mathf.Max(0.1f, cat.ProjectileLifetime.EvaluateClamped(random));
            d.ProjectileInstantHitTime   = Mathf.Max(0f, cat.ProjectileInstantHitTime.EvaluateClamped(random));
            d.LowChargeSpeedMultiplier   = Mathf.Clamp(cat.LowChargeSpeedMultiplier.EvaluateClamped(random), 0.05f, 1f);
            d.LowChargeGravityMultiplier = Mathf.Max(1f, cat.LowChargeGravityMultiplier.EvaluateClamped(random));
            d.LowChargeDamageMultiplier  = Mathf.Clamp(cat.LowChargeDamageMultiplier.EvaluateClamped(random), 0.05f, 1f);

            d.HasDrawStances   = cat.HasDrawStances;
            d.VerticalDraw     = cat.VerticalDraw;
            d.HorizontalDraw   = cat.HorizontalDraw;
            d.VolleySpacingDeg = Mathf.Max(0f, cat.VolleySpacingDeg.EvaluateClamped(random));

            d.DrawTime = roll.Sample(ItemStat.DrawTime, cat.DrawTime);
            d.HitMask  = cat.HitMask;
            d.NoiseRadius = cat.NoiseRadius;

            d.Damage             = roll.Sample(ItemStat.Damage, StyleScales.Scale(cat.Damage, scales.Damage));
            d.DamageType         = cat.DamageType;
            d.ArmorPenetration   = Mathf.Clamp01(roll.Sample(ItemStat.ArmorPenetration, cat.ArmorPenetration));
            d.HeadshotMultiplier = roll.Sample(ItemStat.CritDamage, cat.HeadshotMultiplier);
            d.RangeOptimal       = roll.Sample(ItemStat.Range, StyleScales.Scale(cat.RangeOptimal, scales.Range));
            d.RangeFalloffEnd    = Mathf.Max(d.RangeOptimal + 10f, StyleScales.Scale(cat.RangeFalloffEnd, scales.Range).EvaluateClamped(random));
            d.DamageFalloffMin   = cat.DamageFalloffMin.EvaluateClamped(random);

            d.AmmoType     = cat.AmmoType;
            bool automatic = d.FireMode == FireMode.Auto || d.FireMode == FireMode.Burst;
            IntRange magazine = automatic && cat.AutomaticMagazineSize.Max > 0 ? cat.AutomaticMagazineSize : cat.MagazineSize;
            d.MagazineSize = roll.Sample(ItemStat.MagazineSize, StyleScales.Scale(magazine, scales.MagazineSize));

            d.ReloadTime         = roll.Sample(ItemStat.ReloadTime, cat.ReloadTime);
            d.TacticalReloadTime = Mathf.Max(0.5f, roll.Sample(ItemStat.ReloadTime, cat.TacticalReloadTime));

            FloatRange adsSpread  = firing != null && firing.AdsSpreadDeg.Max > 0f ? firing.AdsSpreadDeg : cat.AdsSpreadDeg;
            d.HipSpreadDeg        = roll.Sample(ItemStat.Spread, StyleScales.Scale(cat.HipSpreadDeg, scales.Spread));
            d.AdsSpreadDeg        = roll.Sample(ItemStat.Spread, adsSpread);
            d.AdsSpreadMultiplier = cat.AdsSpreadMultiplier.EvaluateClamped(random);
            d.SpreadPerShot       = StyleScales.Scale(cat.SpreadPerShot, scales.Spread).EvaluateClamped(random);
            d.MaxSpread           = roll.Sample(ItemStat.Spread, StyleScales.Scale(cat.MaxSpread, scales.Spread));
            d.SpreadRecovery      = cat.SpreadRecovery.EvaluateClamped(random);

            d.RecoilScale         = new Vector2(roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.RecoilScaleHorizontal, scales.Recoil)),
                                                roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.RecoilScaleVertical, scales.Recoil)));
            d.RecoilJitter.y      = cat.RecoilJitterVertical.EvaluateClamped(random); // horizontal jitter keeps WeaponData's default
            d.RecoilHorizontalBias = cat.RecoilHorizontalBias.EvaluateClamped(random);
            d.MaxAccumulatedRecoil = roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.MaxAccumulatedRecoil, scales.Recoil));
            d.MaxAccumulatedHorizontalRecoil = roll.Sample(ItemStat.Recoil, StyleScales.Scale(cat.MaxAccumulatedHorizontalRecoil, scales.Recoil));

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

            d.LookSwayAmount    = Mathf.Clamp01(roll.Sample(ItemStat.Sway, cat.LookSwayAmount));
            d.LookSwayRecovery  = cat.LookSwayRecovery.EvaluateClamped(random);
            d.IdleSwayAmount    = roll.Sample(ItemStat.Sway, cat.IdleSwayAmount);
            d.IdleSwaySpeed     = cat.IdleSwaySpeed.EvaluateClamped(random);
            d.MoveSwayAmount    = roll.Sample(ItemStat.Sway, cat.MoveSwayAmount);

            d.AdsFovDeg = Mathf.Max(1f,   cat.AdsFovDeg.EvaluateClamped(random));
            d.AdsSpeed  = Mathf.Max(0.1f, cat.AdsSpeed.EvaluateClamped(random));

            return new WeaponInstance(cat, roll, d);
        }

        // Null when the category has no styles of this kind, without drawing from the stream.
        private static T PickStyle<T>(T[] styles, System.Func<T, float> weight, RandomStream random) where T : class =>
            styles != null && styles.Length > 0 ? random.PickWeighted(styles, weight) : null;

        // "Marksman Burst Modular Pistol"; a style without a name adds nothing.
        private static string StyledName(string name, FiringStyle firing, StatStyle lean)
        {
            if (!string.IsNullOrEmpty(firing?.Name)) name = $"{firing.Name} {name}";
            if (!string.IsNullOrEmpty(lean?.Name))   name = $"{lean.Name} {name}";
            return name;
        }
    }
}
