using UnityEngine;
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
    public static class WeaponGenerator
    {
        public static WeaponInstance Generate(WeaponCategoryData cat) => Generate(cat, cat.Roll());

        public static WeaponInstance Generate(WeaponCategoryData cat, ItemRoll roll)
        {
            var d = ScriptableObject.CreateInstance<WeaponData>();

            // ── Identity ──────────────────────────────────────────────────────────
            d.WeaponName = cat.Names[Random.Range(0, cat.Names.Length)];
            d.FireMode    = cat.FireModes[Random.Range(0, cat.FireModes.Length)];
            d.FireBehavior = cat.FireBehavior;
            d.OnHitEffects = cat.OnHitEffects;

            // ── Firing ────────────────────────────────────────────────────────────
            d.RoundsPerMinute = roll.Sample(ItemStat.FireRate, cat.RPM);
            d.BurstCount      = cat.BurstCount.Evaluate();
            d.BurstInterval   = cat.BurstInterval.EvaluateClamped();
            d.PelletCount     = Mathf.Max(1, cat.PelletCount.Evaluate());

            // ── Handling ──────────────────────────────────────────────────────────
            d.DrawTime = roll.Sample(ItemStat.DrawTime, cat.DrawTime);
            d.HitMask  = cat.HitMask;
            d.NoiseRadius = cat.NoiseRadius;

            // ── Damage ────────────────────────────────────────────────────────────
            d.Damage             = roll.Sample(ItemStat.Damage, cat.Damage);
            d.DamageType         = cat.DamageType;
            d.ArmorPenetration   = Mathf.Clamp01(roll.Sample(ItemStat.ArmorPenetration, cat.ArmorPenetration));
            d.HeadshotMultiplier = roll.Sample(ItemStat.CritDamage, cat.HeadshotMultiplier);
            d.RangeOptimal       = roll.Sample(ItemStat.Range, cat.RangeOptimal);
            d.RangeFalloffEnd    = Mathf.Max(d.RangeOptimal + 10f, cat.RangeFalloffEnd.EvaluateClamped());
            d.DamageFalloffMin   = cat.DamageFalloffMin.EvaluateClamped();

            // ── Ammo ──────────────────────────────────────────────────────────────
            d.MagazineSize = roll.Sample(ItemStat.MagazineSize, cat.MagazineSize);
            d.ReserveAmmo  = WeaponData.NormalizeReserveAmmo(d.MagazineSize, roll.Sample(ItemStat.AmmoReserve, cat.ReserveAmmo));

            // ── Reload ────────────────────────────────────────────────────────────
            d.ReloadTime         = roll.Sample(ItemStat.ReloadTime, cat.ReloadTime);
            d.TacticalReloadTime = Mathf.Max(0.5f, roll.Sample(ItemStat.ReloadTime, cat.TacticalReloadTime));

            // ── Spread ────────────────────────────────────────────────────────────
            d.HipSpreadDeg        = roll.Sample(ItemStat.Spread, cat.HipSpreadDeg);
            d.AdsSpreadDeg        = roll.Sample(ItemStat.Spread, cat.AdsSpreadDeg);
            d.AdsSpreadMultiplier = cat.AdsSpreadMultiplier.EvaluateClamped();
            d.SpreadPerShot       = cat.SpreadPerShot.EvaluateClamped();
            d.MaxSpread           = roll.Sample(ItemStat.Spread, cat.MaxSpread);
            d.SpreadRecovery      = cat.SpreadRecovery.EvaluateClamped();

            // ── Control — Kick ────────────────────────────────────────────────────
            d.RecoilScale         = new Vector2(roll.Sample(ItemStat.Recoil, cat.RecoilScaleHorizontal),
                                                roll.Sample(ItemStat.Recoil, cat.RecoilScaleVertical));
            d.RecoilJitter.y      = cat.RecoilJitterVertical.EvaluateClamped(); // horizontal jitter keeps WeaponData's default
            d.RecoilHorizontalBias = cat.RecoilHorizontalBias.EvaluateClamped();
            d.MaxAccumulatedRecoil = roll.Sample(ItemStat.Recoil, cat.MaxAccumulatedRecoil);
            d.MaxAccumulatedHorizontalRecoil = roll.Sample(ItemStat.Recoil, cat.MaxAccumulatedHorizontalRecoil);

            // ── Control — Buildup ─────────────────────────────────────────────────
            d.RecoilHeatPerShot          = Mathf.Clamp01(cat.RecoilHeatPerShot.EvaluateClamped());
            d.RecoilHeatCooldown         = Mathf.Max(0f, cat.RecoilHeatCooldown.EvaluateClamped());
            d.MaxHeatRecoilMultiplier    = Mathf.Max(1f, cat.MaxHeatRecoilMultiplier.EvaluateClamped());
            d.RecoilHeatJitterMultiplier = Mathf.Max(1f, cat.RecoilHeatJitterMultiplier.EvaluateClamped());
            d.HotAdsSpreadMultiplier     = Mathf.Clamp01(cat.HotAdsSpreadMultiplier.EvaluateClamped());

            // ── Control — Recovery ────────────────────────────────────────────────
            d.RecoilRecoverySpeed           = cat.RecoilRecoverySpeed.EvaluateClamped();
            d.RecoilRecoveryFraction        = cat.RecoilRecoveryFraction.EvaluateClamped();
            d.AdsRecoilRecoveryFraction     = cat.AdsRecoilRecoveryFraction.EvaluateClamped();
            d.RecoilRecoveryDelay           = cat.RecoilRecoveryDelay.EvaluateClamped();
            d.AdsRecoilMultiplier           = cat.AdsRecoilMultiplier.EvaluateClamped();
            d.HipRecoilVerticalMultiplier   = cat.HipRecoilVerticalMultiplier.EvaluateClamped();
            d.HipRecoilHorizontalMultiplier = cat.HipRecoilHorizontalMultiplier.EvaluateClamped();

            // ── Handling — Sway ───────────────────────────────────────────────────
            d.LookSwayAmount    = Mathf.Clamp01(roll.Sample(ItemStat.Sway, cat.LookSwayAmount));
            d.LookSwayRecovery  = cat.LookSwayRecovery.EvaluateClamped();
            d.IdleSwayAmount    = roll.Sample(ItemStat.Sway, cat.IdleSwayAmount);
            d.IdleSwaySpeed     = cat.IdleSwaySpeed.EvaluateClamped();
            d.MoveSwayAmount    = roll.Sample(ItemStat.Sway, cat.MoveSwayAmount);

            // ── ADS ───────────────────────────────────────────────────────────────
            d.AdsFovDeg = Mathf.Max(1f,   cat.AdsFovDeg.EvaluateClamped());
            d.AdsSpeed  = Mathf.Max(0.1f, cat.AdsSpeed.EvaluateClamped());

            return new WeaponInstance(cat, roll, d);
        }
    }
}
