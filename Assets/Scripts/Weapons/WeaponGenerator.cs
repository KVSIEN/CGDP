using UnityEngine;

public static class WeaponGenerator
{
    public static WeaponData Generate(WeaponCategoryData cat)
    {
        var d = ScriptableObject.CreateInstance<WeaponData>();

        // ── Identity ──────────────────────────────────────────────────────────
        d.WeaponName = cat.Names[Random.Range(0, cat.Names.Length)];
        d.FireMode   = cat.FireModes[Random.Range(0, cat.FireModes.Length)];

        // ── Firing ────────────────────────────────────────────────────────────
        d.RoundsPerMinute = cat.RPM.EvaluateClamped();
        d.BurstCount      = cat.BurstCount.Evaluate();
        d.BurstInterval   = cat.BurstInterval.EvaluateClamped();

        // ── Handling ──────────────────────────────────────────────────────────
        d.DrawTime = cat.DrawTime.EvaluateClamped();
        d.HitMask  = cat.HitMask;

        // ── Damage ────────────────────────────────────────────────────────────
        d.Damage             = cat.Damage.EvaluateClamped();
        d.HeadshotMultiplier = cat.HeadshotMultiplier.EvaluateClamped();
        d.RangeOptimal       = cat.RangeOptimal.EvaluateClamped();
        d.RangeFalloffEnd    = Mathf.Max(d.RangeOptimal + 10f, cat.RangeFalloffEnd.EvaluateClamped());
        d.DamageFalloffMin   = cat.DamageFalloffMin.EvaluateClamped();

        // ── Ammo ──────────────────────────────────────────────────────────────
        d.MagazineSize = cat.MagazineSize.Evaluate();
        d.ReserveAmmo  = cat.ReserveAmmo.Evaluate();

        // ── Reload ────────────────────────────────────────────────────────────
        d.ReloadTime         = cat.ReloadTime.EvaluateClamped();
        d.TacticalReloadTime = Mathf.Max(0.5f, cat.TacticalReloadTime.EvaluateClamped());

        // ── Spread ────────────────────────────────────────────────────────────
        d.HipSpreadDeg        = cat.HipSpreadDeg.EvaluateClamped();
        d.HipSpreadScale      = 1f;
        d.AdsSpreadDeg        = cat.AdsSpreadDeg.EvaluateClamped();
        d.AdsSpreadMultiplier = cat.AdsSpreadMultiplier.EvaluateClamped();
        d.SpreadPerShot       = cat.SpreadPerShot.EvaluateClamped();
        d.MaxSpread           = cat.MaxSpread.EvaluateClamped();
        d.SpreadRecovery      = cat.SpreadRecovery.EvaluateClamped();

        // ── Recoil — Kick ─────────────────────────────────────────────────────
        d.RecoilVertical          = cat.RecoilVertical.EvaluateClamped();
        d.RecoilVerticalVariation = cat.RecoilVerticalVariation.EvaluateClamped();
        d.RecoilHorizontalMax     = cat.RecoilHorizontalMax.EvaluateClamped();
        d.RecoilHorizontalBias    = cat.RecoilHorizontalBias.EvaluateClamped();
        d.MaxAccumulatedRecoil    = cat.MaxAccumulatedRecoil.EvaluateClamped();

        // ── Recoil — Recovery ─────────────────────────────────────────────────
        d.RecoilRecoverySpeed           = cat.RecoilRecoverySpeed.EvaluateClamped();
        d.RecoilRecoveryFraction        = cat.RecoilRecoveryFraction.EvaluateClamped();
        d.AdsRecoilRecoveryFraction     = cat.AdsRecoilRecoveryFraction.EvaluateClamped();
        d.RecoilRecoveryDelay           = cat.RecoilRecoveryDelay.EvaluateClamped();
        d.AdsRecoilMultiplier           = cat.AdsRecoilMultiplier.EvaluateClamped();
        d.HipRecoilVerticalMultiplier   = cat.HipRecoilVerticalMultiplier.EvaluateClamped();
        d.HipRecoilHorizontalMultiplier = cat.HipRecoilHorizontalMultiplier.EvaluateClamped();

        return d;
    }
}
