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

            d.WeaponName = random.Pick(cat.Names);
            d.FireMode    = random.Pick(cat.FireModes);
            d.FireBehavior = cat.FireBehavior;
            d.OnHitEffects = cat.OnHitEffects;

            d.RoundsPerMinute = roll.Sample(ItemStat.FireRate, cat.RPM);
            d.BurstCount      = cat.BurstCount.Evaluate(random);
            d.BurstInterval   = cat.BurstInterval.EvaluateClamped(random);
            d.PelletCount     = Mathf.Max(1, cat.PelletCount.Evaluate(random));

            d.DrawTime = roll.Sample(ItemStat.DrawTime, cat.DrawTime);
            d.HitMask  = cat.HitMask;
            d.NoiseRadius = cat.NoiseRadius;

            d.Damage             = roll.Sample(ItemStat.Damage, cat.Damage);
            d.DamageType         = cat.DamageType;
            d.ArmorPenetration   = Mathf.Clamp01(roll.Sample(ItemStat.ArmorPenetration, cat.ArmorPenetration));
            d.HeadshotMultiplier = roll.Sample(ItemStat.CritDamage, cat.HeadshotMultiplier);
            d.RangeOptimal       = roll.Sample(ItemStat.Range, cat.RangeOptimal);
            d.RangeFalloffEnd    = Mathf.Max(d.RangeOptimal + 10f, cat.RangeFalloffEnd.EvaluateClamped(random));
            d.DamageFalloffMin   = cat.DamageFalloffMin.EvaluateClamped(random);

            d.AmmoType     = cat.AmmoType;
            d.MagazineSize = roll.Sample(ItemStat.MagazineSize, cat.MagazineSize);

            d.ReloadTime         = roll.Sample(ItemStat.ReloadTime, cat.ReloadTime);
            d.TacticalReloadTime = Mathf.Max(0.5f, roll.Sample(ItemStat.ReloadTime, cat.TacticalReloadTime));

            d.HipSpreadDeg        = roll.Sample(ItemStat.Spread, cat.HipSpreadDeg);
            d.AdsSpreadDeg        = roll.Sample(ItemStat.Spread, cat.AdsSpreadDeg);
            d.AdsSpreadMultiplier = cat.AdsSpreadMultiplier.EvaluateClamped(random);
            d.SpreadPerShot       = cat.SpreadPerShot.EvaluateClamped(random);
            d.MaxSpread           = roll.Sample(ItemStat.Spread, cat.MaxSpread);
            d.SpreadRecovery      = cat.SpreadRecovery.EvaluateClamped(random);

            d.RecoilScale         = new Vector2(roll.Sample(ItemStat.Recoil, cat.RecoilScaleHorizontal),
                                                roll.Sample(ItemStat.Recoil, cat.RecoilScaleVertical));
            d.RecoilJitter.y      = cat.RecoilJitterVertical.EvaluateClamped(random); // horizontal jitter keeps WeaponData's default
            d.RecoilHorizontalBias = cat.RecoilHorizontalBias.EvaluateClamped(random);
            d.MaxAccumulatedRecoil = roll.Sample(ItemStat.Recoil, cat.MaxAccumulatedRecoil);
            d.MaxAccumulatedHorizontalRecoil = roll.Sample(ItemStat.Recoil, cat.MaxAccumulatedHorizontalRecoil);

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
    }
}
