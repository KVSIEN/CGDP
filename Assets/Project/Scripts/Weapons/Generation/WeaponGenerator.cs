using UnityEngine;

namespace CGD.Weapons
{
    public static class WeaponGenerator
    {
        public static WeaponData Generate(WeaponCategoryData cat)
        {
            var d = ScriptableObject.CreateInstance<WeaponData>();

            // ── Identity ──────────────────────────────────────────────────────────
            d.WeaponName = cat.Names[Random.Range(0, cat.Names.Length)];
            d.FireMode    = cat.FireModes[Random.Range(0, cat.FireModes.Length)];
            d.FireBehavior = cat.FireBehavior;
            d.OnHitEffects = cat.OnHitEffects;

            // ── Firing ────────────────────────────────────────────────────────────
            d.RoundsPerMinute = cat.RPM.EvaluateClamped();
            d.BurstCount      = cat.BurstCount.Evaluate();
            d.BurstInterval   = cat.BurstInterval.EvaluateClamped();
            d.PelletCount     = Mathf.Max(1, cat.PelletCount.Evaluate());

            // ── Handling ──────────────────────────────────────────────────────────
            d.DrawTime = cat.DrawTime.EvaluateClamped();
            d.HitMask  = cat.HitMask;
            d.NoiseRadius = cat.NoiseRadius;

            // ── Damage ────────────────────────────────────────────────────────────
            d.Damage             = cat.Damage.EvaluateClamped();
            d.DamageType         = cat.DamageType;
            d.ArmorPenetration   = Mathf.Clamp01(cat.ArmorPenetration.EvaluateClamped());
            d.HeadshotMultiplier = cat.HeadshotMultiplier.EvaluateClamped();
            d.RangeOptimal       = cat.RangeOptimal.EvaluateClamped();
            d.RangeFalloffEnd    = Mathf.Max(d.RangeOptimal + 10f, cat.RangeFalloffEnd.EvaluateClamped());
            d.DamageFalloffMin   = cat.DamageFalloffMin.EvaluateClamped();

            // ── Ammo ──────────────────────────────────────────────────────────────
            d.MagazineSize = cat.MagazineSize.Evaluate();
            d.ReserveAmmo  = WeaponData.NormalizeReserveAmmo(d.MagazineSize, cat.ReserveAmmo.Evaluate());

            // ── Reload ────────────────────────────────────────────────────────────
            d.ReloadTime         = cat.ReloadTime.EvaluateClamped();
            d.TacticalReloadTime = Mathf.Max(0.5f, cat.TacticalReloadTime.EvaluateClamped());

            // ── Spread ────────────────────────────────────────────────────────────
            d.HipSpreadDeg        = cat.HipSpreadDeg.EvaluateClamped();
            d.AdsSpreadDeg        = cat.AdsSpreadDeg.EvaluateClamped();
            d.AdsSpreadMultiplier = cat.AdsSpreadMultiplier.EvaluateClamped();
            d.SpreadPerShot       = cat.SpreadPerShot.EvaluateClamped();
            d.MaxSpread           = cat.MaxSpread.EvaluateClamped();
            d.SpreadRecovery      = cat.SpreadRecovery.EvaluateClamped();

            // ── Control — Kick ────────────────────────────────────────────────────
            d.RecoilScale         = new Vector2(cat.RecoilScaleHorizontal.EvaluateClamped(), cat.RecoilScaleVertical.EvaluateClamped());
            d.RecoilJitter.y      = cat.RecoilJitterVertical.EvaluateClamped(); // horizontal jitter keeps WeaponData's default
            d.RecoilHorizontalBias = cat.RecoilHorizontalBias.EvaluateClamped();
            d.MaxAccumulatedRecoil = cat.MaxAccumulatedRecoil.EvaluateClamped();
            d.MaxAccumulatedHorizontalRecoil = cat.MaxAccumulatedHorizontalRecoil.EvaluateClamped();

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
            d.LookSwayAmount    = Mathf.Clamp01(cat.LookSwayAmount.EvaluateClamped());
            d.LookSwayRecovery  = cat.LookSwayRecovery.EvaluateClamped();
            d.IdleSwayAmount    = cat.IdleSwayAmount.EvaluateClamped();
            d.IdleSwaySpeed     = cat.IdleSwaySpeed.EvaluateClamped();
            d.MoveSwayAmount    = cat.MoveSwayAmount.EvaluateClamped();

            // ── ADS ───────────────────────────────────────────────────────────────
            d.AdsFovDeg = Mathf.Max(1f,   cat.AdsFovDeg.EvaluateClamped());
            d.AdsSpeed  = Mathf.Max(0.1f, cat.AdsSpeed.EvaluateClamped());

            return d;
        }
    }
}
