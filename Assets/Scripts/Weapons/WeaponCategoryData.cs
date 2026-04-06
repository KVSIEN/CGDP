using UnityEngine;

// Create one asset per weapon type via Assets > Create > CGD > Weapon Category.
// Right-click the asset and choose "Apply Type Defaults" to auto-fill realistic thresholds,
// then tweak individual ranges as needed.
[CreateAssetMenu(fileName = "WeaponCategory", menuName = "CGD/Weapon Category")]
public class WeaponCategoryData : ScriptableObject
{
    [Header("Identity")]
    public WeaponType Type;
    public string[]   Names = { "Weapon" };

    // FireMode entries can be repeated to weight the random pick.
    // e.g. { Auto, Auto, Auto, Semi } = 75% Auto, 25% Semi.
    public FireMode[] FireModes = { FireMode.Auto };

    [Header("Firing")]
    public FloatRange RPM          = new(600, 800);
    public IntRange   BurstCount   = new(3, 3);    // only used if Burst is in FireModes
    public FloatRange BurstInterval = new(0.07f, 0.10f);

    [Header("Damage")]
    public FloatRange Damage              = new(25, 35);
    public FloatRange HeadshotMultiplier  = new(2f, 2f);
    public FloatRange RangeOptimal        = new(40, 60);
    public FloatRange RangeFalloffEnd     = new(120, 200);
    public FloatRange DamageFalloffMin    = new(0.3f, 0.5f);

    [Header("Ammo")]
    public IntRange   MagazineSize       = new(25, 35);
    public IntRange   ReserveAmmo        = new(75, 110);
    // Reserve = RoundToInt(magazine * multiplier)
    public FloatRange ReserveMultiplier  = new(2.5f, 3.5f);

    [Header("Reload")]
    public FloatRange ReloadTime          = new(2.2f, 3.0f);
    public FloatRange TacticalReloadTime  = new(1.7f, 2.2f);
    // TacticalReloadTime is stored directly for WeaponData and can still be tuned via bonus.
    public FloatRange TacticalReloadBonus = new(0.3f, 0.6f);

    [Header("Handling")]
    public FloatRange DrawTime           = new(0.5f, 0.7f);
    public LayerMask HitMask             = ~0;

    [Header("Spread")]
    public FloatRange HipSpreadDeg          = new(2.0f, 3.5f);
    public FloatRange AdsSpreadDeg          = new(0.2f, 0.5f);
    public FloatRange AdsSpreadMultiplier   = new(0f, 0.15f);
    public FloatRange SpreadPerShot         = new(0.6f, 1.0f);
    public FloatRange MaxSpread      = new(5f,   8f);
    public FloatRange SpreadRecovery = new(12f,  18f);

    [Header("Recoil")]
    public FloatRange RecoilVerticalMax  = new(0.9f, 1.5f);
    public FloatRange RecoilVerticalBias = new(0.1f, 0.3f);
    public FloatRange RecoilHorizontalMax     = new(0.4f, 0.7f);
    public FloatRange RecoilHorizontalBias    = new(-0.2f, 0.2f);  // drift direction
    public FloatRange MaxAccumulatedRecoil    = new(12f,  18f);

    [Header("Recoil — Recovery")]
    public FloatRange RecoilRecoverySpeed         = new(4f, 9f);
    public FloatRange RecoilRecoveryFraction      = new(0.55f, 0.80f);
    public FloatRange RecoilRecoveryDelay         = new(0.08f, 0.18f);
    public FloatRange AdsRecoilRecoveryFraction   = new(0.85f, 1f);
    public FloatRange AdsRecoilMultiplier         = new(0.35f, 0.55f);
    public FloatRange HipRecoilVerticalMultiplier = new(0.10f, 0.25f);
    public FloatRange HipRecoilHorizontalMultiplier = new(0.10f, 0.25f);

    // ── Context Menu ──────────────────────────────────────────────────────────

    [ContextMenu("Apply Type Defaults")]
    public void ApplyTypeDefaults()
    {
        switch (Type)
        {
            case WeaponType.AR:     ApplyAR();     break;
            case WeaponType.SMG:    ApplySMG();    break;
            case WeaponType.Pistol: ApplyPistol(); break;
            case WeaponType.Sniper: ApplySniper(); break;
            case WeaponType.LMG:    ApplyLMG();    break;
        }
    }

    // ── Per-Type Defaults ─────────────────────────────────────────────────────

    private void ApplyAR()
    {
        Names     = new[] { "M4A1", "AK-47", "SCAR-L", "HK416", "AR-15", "M16A4" };
        FireModes = new[] { FireMode.Auto, FireMode.Auto, FireMode.Auto, FireMode.Semi, FireMode.Burst };

        RPM           = new(600,   850,  0f);
        BurstCount    = new(3,     3);
        BurstInterval = new(0.07f, 0.09f);

        Damage             = new(22,    32,    0f);
        HeadshotMultiplier = new(2f,    2f);
        RangeOptimal       = new(40f,   60f,   0f);
        RangeFalloffEnd    = new(120f,  180f,  0f);
        DamageFalloffMin   = new(0.30f, 0.50f, 0f);

        MagazineSize      = new(25,    40,   0f);   // slight high bias — 30 to 40 common
        ReserveAmmo       = new(80,    120,  0f);
        ReserveMultiplier = new(2.5f,  3.5f, 0f);

        ReloadTime          = new(2.2f, 3.0f, 0f);
        TacticalReloadTime  = new(1.8f, 2.4f, 0f);
        TacticalReloadBonus = new(0.3f, 0.6f, 0f);

        HipSpreadDeg   = new(2.0f, 3.5f, 0f);
        AdsSpreadDeg   = new(0.2f, 0.5f, 0f);
        AdsSpreadMultiplier = new(0.00f, 0.15f, 0f);
        SpreadPerShot  = new(0.6f, 1.0f, 0f);
        MaxSpread      = new(5f,   8f,   0f);
        SpreadRecovery = new(12f,  18f,  0f);

        RecoilVerticalMax  = new(0.9f,  1.5f,  0f);
        RecoilVerticalBias = new(0.1f,  0.3f,  0f);
        RecoilHorizontalMax     = new(0.4f,  0.7f,  0f);
        RecoilHorizontalBias    = new(-0.2f, 0.2f,  0f);
        MaxAccumulatedRecoil    = new(12f,   18f,   0f);

        RecoilRecoverySpeed         = new(5f, 7.5f, 0f);
        RecoilRecoveryFraction      = new(0.55f, 0.80f, 0f);
        RecoilRecoveryDelay         = new(0.10f, 0.16f, 0f);
        AdsRecoilRecoveryFraction   = new(0.85f, 1f, 0f);
        AdsRecoilMultiplier         = new(0.35f, 0.55f, 0f);
        HipRecoilVerticalMultiplier = new(0.10f, 0.25f, 0f);
        HipRecoilHorizontalMultiplier = new(0.10f, 0.20f, 0f);
    }

    private void ApplySMG()
    {
        Names     = new[] { "MP5", "UMP-45", "P90", "Vector", "MP7", "PP-19 Bizon" };
        FireModes = new[] { FireMode.Auto };

        RPM           = new(750,   1100,  0f);
        BurstCount    = new(3,     3);
        BurstInterval = new(0.05f, 0.07f);

        Damage             = new(15,    24,    0f);
        HeadshotMultiplier = new(1.75f, 2.25f, 0f);
        RangeOptimal       = new(20f,   35f,   0f);
        RangeFalloffEnd    = new(80f,   120f,  0f);
        DamageFalloffMin   = new(0.25f, 0.45f, 0f);

        // P90 is an outlier at 50 rounds; most SMGs are 20–32 → bias low
        MagazineSize      = new(20,   50,   -0.5f);
        ReserveAmmo       = new(60,   100,  0f);
        ReserveMultiplier = new(3f,   4f,    0f);

        ReloadTime          = new(1.8f, 2.5f, 0f);
        TacticalReloadTime  = new(1.4f, 2.1f, 0f);
        TacticalReloadBonus = new(0.2f, 0.5f, 0f);

        HipSpreadDeg   = new(1.5f, 2.8f, 0f);
        AdsSpreadDeg   = new(0.3f, 0.6f, 0f);
        AdsSpreadMultiplier = new(0.05f, 0.20f, 0f);
        SpreadPerShot  = new(0.5f, 0.9f, 0f);
        MaxSpread      = new(4f,   7f,   0f);
        SpreadRecovery = new(14f,  22f,  0f);   // recovers fast, high fire rate

        RecoilVerticalMax  = new(0.6f,  1.1f,  0f);
        RecoilVerticalBias = new(0.1f,  0.25f, 0f);
        RecoilHorizontalMax     = new(0.5f,  0.9f,  0f);  // more erratic
        RecoilHorizontalBias    = new(-0.3f, 0.3f,  0f);
        MaxAccumulatedRecoil    = new(8f,    14f,   0f);

        RecoilRecoverySpeed         = new(5f, 7f, 0f);
        RecoilRecoveryFraction      = new(0.45f, 0.70f, 0f);
        RecoilRecoveryDelay         = new(0.08f, 0.14f, 0f);
        AdsRecoilRecoveryFraction   = new(0.80f, 1f, 0f);
        AdsRecoilMultiplier         = new(0.30f, 0.50f, 0f);
        HipRecoilVerticalMultiplier = new(0.08f, 0.18f, 0f);
        HipRecoilHorizontalMultiplier = new(0.08f, 0.18f, 0f);
    }

    private void ApplyPistol()
    {
        Names     = new[] { "M9", "Glock 17", "Desert Eagle", "USP-S", "P250", "Five-seveN" };
        FireModes = new[] { FireMode.Semi, FireMode.Semi, FireMode.Semi, FireMode.Auto };

        RPM           = new(300,   600,  0f);
        BurstCount    = new(2,     2);
        BurstInterval = new(0.08f, 0.12f);

        // Wide range: Glock ≈ 20 dmg, Deagle ≈ 55 dmg
        Damage             = new(20,    55,    0f);
        HeadshotMultiplier = new(2.0f,  2.5f,  0f);
        RangeOptimal       = new(15f,   25f,   0f);
        RangeFalloffEnd    = new(50f,   80f,   0f);
        DamageFalloffMin   = new(0.30f, 0.60f, 0f);

        // Deagle 7, Five-seveN 20, Glock 17 17 → slight low bias
        MagazineSize      = new(7,    20,   -0.2f);
        ReserveAmmo       = new(35,   80,   0f);
        ReserveMultiplier = new(3f,   5f,    0f);

        ReloadTime          = new(1.5f, 2.2f, 0f);
        TacticalReloadTime  = new(1.1f, 1.8f, 0f);
        TacticalReloadBonus = new(0.1f, 0.4f, 0f);

        HipSpreadDeg   = new(1.5f, 3.5f, 0f);
        AdsSpreadDeg   = new(0.4f, 0.9f, 0f);
        AdsSpreadMultiplier = new(0.10f, 0.25f, 0f);
        SpreadPerShot  = new(0.8f, 1.5f, 0f);  // bloom quickly
        MaxSpread      = new(5f,   9f,   0f);
        SpreadRecovery = new(10f,  16f,  0f);

        RecoilVerticalMax  = new(1.2f,  2.8f,  0f);  // heavy per shot
        RecoilVerticalBias = new(0.2f,  0.5f,  0f);
        RecoilHorizontalMax     = new(0.3f,  0.9f,  0f);
        RecoilHorizontalBias    = new(-0.15f, 0.15f, 0f);
        MaxAccumulatedRecoil    = new(6f,    12f,   0f);   // small mag = small cap

        RecoilRecoverySpeed         = new(5f, 8f, 0f);
        RecoilRecoveryFraction      = new(0.60f, 0.90f, 0f);
        RecoilRecoveryDelay         = new(0.10f, 0.18f, 0f);
        AdsRecoilRecoveryFraction   = new(0.85f, 1f, 0f);
        AdsRecoilMultiplier         = new(0.40f, 0.65f, 0f);
        HipRecoilVerticalMultiplier = new(0.25f, 0.50f, 0f);
        HipRecoilHorizontalMultiplier = new(0.20f, 0.40f, 0f);
    }

    private void ApplySniper()
    {
        Names     = new[] { "AWP", "Barrett M82A1", "L96A1", "M24", "Kar98k", "SV-98" };
        FireModes = new[] { FireMode.Semi };   // bolt-action feel

        RPM           = new(30,    80,    0f);
        BurstCount    = new(1,     1);
        BurstInterval = new(0.0f,  0.0f);

        Damage             = new(70,    160,   0f);  // one-shot potential
        HeadshotMultiplier = new(2.0f,  3.0f,  0f);  // devastating headshots
        RangeOptimal       = new(80f,   200f,  0f);
        RangeFalloffEnd    = new(400f,  1000f, 0f);
        DamageFalloffMin   = new(0.60f, 0.90f, 0f);  // retain damage at range

        MagazineSize      = new(5,    10,   0f);
        ReserveAmmo       = new(20,   35,   0f);
        ReserveMultiplier = new(2f,   3f,   0f);

        ReloadTime          = new(2.8f, 5.0f, 0f);
        TacticalReloadTime  = new(2.0f, 4.5f, 0f);
        TacticalReloadBonus = new(0.3f, 0.8f, 0f);

        HipSpreadDeg   = new(8f,    15f,   0f);     // terrible hipfire
        AdsSpreadDeg   = new(0.01f, 0.15f, -0.5f); // weighted toward very accurate
        AdsSpreadMultiplier = new(0f, 0.05f, 0f);
        SpreadPerShot  = new(2.0f,  4.0f,  0f);
        MaxSpread      = new(15f,   25f,   0f);
        SpreadRecovery = new(4f,    8f,    0f);     // slow recovery

        RecoilVerticalMax  = new(3.0f, 6.0f,  0f);  // massive kick
        RecoilVerticalBias = new(0.3f, 0.8f,  0f);
        RecoilHorizontalMax     = new(0.8f, 1.8f,  0f);
        RecoilHorizontalBias    = new(-0.1f, 0.1f, 0f);
        MaxAccumulatedRecoil    = new(3f,   6f,    0f);  // one shot at a time

        RecoilRecoverySpeed         = new(3.5f, 5f, 0f);
        RecoilRecoveryFraction      = new(0.85f, 1f, 0f);
        RecoilRecoveryDelay         = new(0.18f, 0.35f, 0f);
        AdsRecoilRecoveryFraction   = new(0.95f, 1f, 0f);
        AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
        HipRecoilVerticalMultiplier = new(0.05f, 0.05f, 0f);
        HipRecoilHorizontalMultiplier = new(0.05f, 0.05f, 0f);
    }

    private void ApplyLMG()
    {
        Names     = new[] { "M249 SAW", "MG42", "RPK", "Negev", "M60", "PKM" };
        FireModes = new[] { FireMode.Auto };

        RPM           = new(600,   950,  0f);
        BurstCount    = new(3,     3);
        BurstInterval = new(0.07f, 0.10f);

        Damage             = new(28,    42,    0f);
        HeadshotMultiplier = new(1.75f, 2.0f,  0f);
        RangeOptimal       = new(40f,   70f,   0f);
        RangeFalloffEnd    = new(150f,  300f,  0f);
        DamageFalloffMin   = new(0.35f, 0.55f, 0f);

        // Belt-fed: 75 baseline, up to 200; weighted toward lower end of the belt range
        MagazineSize      = new(75,   200,  -0.3f);
        ReserveAmmo       = new(125,  225,  0f);
        ReserveMultiplier = new(1.5f, 2.5f,  0f);  // fewer spare belts

        ReloadTime          = new(4.5f, 8.0f, 0f);  // slow belt/drum swap
        TacticalReloadTime  = new(3.8f, 6.5f, 0f);
        TacticalReloadBonus = new(0.5f, 1.5f, 0f);

        HipSpreadDeg   = new(3.5f, 6.0f, 0f);   // wide hipfire
        AdsSpreadDeg   = new(0.6f, 1.2f, 0f);   // not precise even ADS (vibration)
        AdsSpreadMultiplier = new(0.20f, 0.40f, 0f);
        SpreadPerShot  = new(0.8f, 1.4f, 0f);
        MaxSpread      = new(10f,  16f,  0f);
        SpreadRecovery = new(6f,   10f,  0f);    // slow recovery

        RecoilVerticalMax  = new(1.2f,  2.2f,  0f);
        RecoilVerticalBias = new(0.3f,  0.6f,  0f);
        RecoilHorizontalMax     = new(0.8f,  1.4f,  0f);
        RecoilHorizontalBias    = new(-0.25f, 0.25f, 0f);
        MaxAccumulatedRecoil    = new(20f,   35f,   0f);  // huge cap for sustained fire

        RecoilRecoverySpeed         = new(3f, 5f, 0f);
        RecoilRecoveryFraction      = new(0.30f, 0.55f, 0f);
        RecoilRecoveryDelay         = new(0.10f, 0.18f, 0f);
        AdsRecoilRecoveryFraction   = new(0.75f, 0.90f, 0f);
        AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
        HipRecoilVerticalMultiplier = new(0.20f, 0.35f, 0f);
        HipRecoilHorizontalMultiplier = new(0.18f, 0.30f, 0f);
    }
}
