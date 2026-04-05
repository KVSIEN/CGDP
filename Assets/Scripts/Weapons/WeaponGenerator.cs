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

        // ── Damage ────────────────────────────────────────────────────────────
        d.Damage             = cat.Damage.EvaluateClamped();
        d.HeadshotMultiplier = cat.HeadshotMultiplier.EvaluateClamped();
        d.RangeOptimal       = cat.RangeOptimal.EvaluateClamped();
        d.RangeFalloffEnd    = Mathf.Max(d.RangeOptimal + 10f, cat.RangeFalloffEnd.EvaluateClamped());
        d.DamageFalloffMin   = cat.DamageFalloffMin.EvaluateClamped();

        // ── Ammo ──────────────────────────────────────────────────────────────
        d.MagazineSize = cat.MagazineSize.Evaluate();
        d.ReserveAmmo  = Mathf.RoundToInt(d.MagazineSize * cat.ReserveMultiplier.EvaluateClamped());

        // ── Reload ────────────────────────────────────────────────────────────
        float reloadTime     = cat.ReloadTime.EvaluateClamped();
        float tacBonus       = cat.TacticalReloadBonus.EvaluateClamped();
        d.ReloadTime         = reloadTime;
        d.TacticalReloadTime = Mathf.Max(0.5f, reloadTime - tacBonus);

        // ── Spread ────────────────────────────────────────────────────────────
        d.HipSpreadDeg        = cat.HipSpreadDeg.EvaluateClamped();
        d.HipSpreadScale      = 1f;
        d.AdsSpreadDeg        = cat.AdsSpreadDeg.EvaluateClamped();
        d.AdsSpreadMultiplier = DeriveAdsSpreadMult(cat.Type);
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
        // Derived from fire rate and category feel rather than separately configurable
        d.RecoilRecoverySpeed          = DeriveRecoverySpeed(cat.Type, d.RoundsPerMinute);
        d.RecoilRecoveryFraction       = DeriveRecoveryFraction(cat.Type);
        d.AdsRecoilRecoveryFraction    = Mathf.Min(1f, d.RecoilRecoveryFraction + 0.2f);
        d.RecoilRecoveryDelay          = DeriveRecoveryDelay(d.RoundsPerMinute);
        d.AdsRecoilMultiplier          = DeriveAdsRecoilMult(cat.Type);
        d.HipRecoilVerticalMultiplier  = DeriveHipRecoilVert(cat.Type);
        d.HipRecoilHorizontalMultiplier = DeriveHipRecoilHoriz(cat.Type);

        return d;
    }

    // ── Derived stat helpers ──────────────────────────────────────────────────

    // How much bloom applies while ADS (0 = zero bloom, 1 = same as hip)
    private static float DeriveAdsSpreadMult(WeaponType t) => t switch
    {
        WeaponType.Sniper => 0f,
        WeaponType.Pistol => Random.Range(0.10f, 0.25f),
        WeaponType.AR     => Random.Range(0.00f, 0.15f),
        WeaponType.SMG    => Random.Range(0.05f, 0.20f),
        WeaponType.LMG    => Random.Range(0.20f, 0.40f),
        _                 => 0f,
    };

    // Higher RPM weapons need faster recoil recovery so it doesn't stack endlessly
    private static float DeriveRecoverySpeed(WeaponType t, float rpm) => t switch
    {
        WeaponType.Sniper => Random.Range(3f, 5f),
        WeaponType.LMG    => Random.Range(3f, 5f),
        WeaponType.Pistol => Random.Range(5f, 9f),
        _                 => Mathf.Lerp(4f, 9f, Mathf.InverseLerp(400f, 1100f, rpm)),
    };

    // 0 = BF-style (aim stays where recoil took it), 1 = CoD-style (full return)
    private static float DeriveRecoveryFraction(WeaponType t) => t switch
    {
        WeaponType.Sniper => Random.Range(0.85f, 1.0f),  // fully returns (bolt-action)
        WeaponType.Pistol => Random.Range(0.60f, 0.90f),
        WeaponType.AR     => Random.Range(0.55f, 0.80f),
        WeaponType.SMG    => Random.Range(0.45f, 0.70f),
        WeaponType.LMG    => Random.Range(0.30f, 0.55f), // spray and pray
        _                 => 0.65f,
    };

    // Delay before recoil recovery starts — slightly longer than one fire interval
    private static float DeriveRecoveryDelay(float rpm)
    {
        float fireInterval = 60f / rpm;
        return Mathf.Clamp(fireInterval * 1.2f, 0.08f, 0.5f);
    }

    // Recoil multiplier while aiming down sights
    private static float DeriveAdsRecoilMult(WeaponType t) => t switch
    {
        WeaponType.Sniper => Random.Range(0.55f, 0.75f),
        WeaponType.Pistol => Random.Range(0.40f, 0.65f),
        WeaponType.AR     => Random.Range(0.35f, 0.55f),
        WeaponType.SMG    => Random.Range(0.30f, 0.50f),
        WeaponType.LMG    => Random.Range(0.55f, 0.75f), // hard to control even ADS
        _                 => 0.45f,
    };

    // How much vertical camera kick applies while hip-firing (0 = spread only)
    private static float DeriveHipRecoilVert(WeaponType t) => t switch
    {
        WeaponType.Sniper => 0.05f,                       // barely moves camera, spread handles it
        WeaponType.Pistol => Random.Range(0.25f, 0.50f),
        WeaponType.AR     => Random.Range(0.10f, 0.25f),
        WeaponType.SMG    => Random.Range(0.08f, 0.18f),
        WeaponType.LMG    => Random.Range(0.20f, 0.35f),
        _                 => 0.15f,
    };

    private static float DeriveHipRecoilHoriz(WeaponType t) => t switch
    {
        WeaponType.Sniper => 0.05f,
        WeaponType.Pistol => Random.Range(0.20f, 0.40f),
        WeaponType.AR     => Random.Range(0.10f, 0.20f),
        WeaponType.SMG    => Random.Range(0.08f, 0.18f),
        WeaponType.LMG    => Random.Range(0.18f, 0.30f),
        _                 => 0.15f,
    };
}
