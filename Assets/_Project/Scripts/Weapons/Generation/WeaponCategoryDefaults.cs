namespace CGD.Weapons
{
    // Realistic per-type stat thresholds, applied from WeaponCategoryData's
    // "Apply Type Defaults" context menu.
    public static class WeaponCategoryDefaults
    {
        public static void Apply(WeaponCategoryData c)
        {
            switch (c.Type)
            {
                case WeaponType.AR:      ApplyAR(c);      break;
                case WeaponType.SMG:     ApplySMG(c);     break;
                case WeaponType.Pistol:  ApplyPistol(c);  break;
                case WeaponType.Sniper:  ApplySniper(c);  break;
                case WeaponType.LMG:     ApplyLMG(c);     break;
                case WeaponType.Shotgun: ApplyShotgun(c); break;
            }
        }

        private static void ApplyAR(WeaponCategoryData c)
        {
            c.Names     = new[] { "M4A1", "AK-47", "SCAR-L", "HK416", "AR-15", "M16A4" };
            c.FireModes = new[] { FireMode.Auto, FireMode.Auto, FireMode.Auto, FireMode.Semi, FireMode.Burst };

            c.RPM           = new(600,   850,  0f);
            c.BurstCount    = new(3,     3);
            c.BurstInterval = new(0.07f, 0.09f);

            c.Damage             = new(22,    32,    0f);
            c.HeadshotMultiplier = new(2f,    2f);
            c.RangeOptimal       = new(40f,   60f,   0f);
            c.RangeFalloffEnd    = new(120f,  180f,  0f);
            c.DamageFalloffMin   = new(0.30f, 0.50f, 0f);

            c.MagazineSize      = new(25,    40,   0f);   // slight high bias — 30 to 40 common
            c.ReserveAmmo       = new(80,    120,  0f);
            c.ReserveMultiplier = new(2.5f,  3.5f, 0f);

            c.ReloadTime          = new(2.2f, 3.0f, 0f);
            c.TacticalReloadTime  = new(1.8f, 2.4f, 0f);
            c.TacticalReloadBonus = new(0.3f, 0.6f, 0f);

            c.HipSpreadDeg   = new(2.0f, 3.5f, 0f);
            c.AdsSpreadDeg   = new(0.2f, 0.5f, 0f);
            c.AdsSpreadMultiplier = new(0.00f, 0.15f, 0f);
            c.SpreadPerShot  = new(0.6f, 1.0f, 0f);
            c.MaxSpread      = new(5f,   8f,   0f);
            c.SpreadRecovery = new(12f,  18f,  0f);

            c.RecoilScaleVertical   = new(0.9f,   1.5f,  0f);
            c.RecoilJitterVertical  = new(0.083f, 0.25f, 0f);
            c.RecoilScaleHorizontal = new(0.4f,   0.7f,  0f);
            c.RecoilHorizontalBias  = new(-0.2f,  0.2f,  0f);
            c.MaxAccumulatedRecoil  = new(12f,    18f,   0f);
            c.MaxAccumulatedHorizontalRecoil = new(6f, 9f, 0f);

            c.RecoilRecoverySpeed         = new(5f, 7.5f, 0f);
            c.RecoilRecoveryFraction      = new(0.55f, 0.80f, 0f);
            c.RecoilRecoveryDelay         = new(0.10f, 0.16f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.85f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.35f, 0.55f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.10f, 0.25f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.10f, 0.20f, 0f);

            // ~15 rounds of controlled fire before drift becomes obvious.
            c.RecoilHeatPerShot          = new(0.05f, 0.07f, 0f);
            c.RecoilHeatDecay            = new(0.5f,  0.8f,  0f);
            c.RecoilHeatKickMultiplier   = new(1.4f,  1.7f,  0f);
            c.RecoilHeatJitterMultiplier = new(2.0f,  3.0f,  0f);

            c.LookSwayAmount    = new(0.4f,  0.6f,  0f);
            c.LookSwayRecovery  = new(9f,    12f,   0f);
            c.IdleSwayAmount    = new(0.3f,  0.5f,  0f);
            c.IdleSwaySpeed     = new(0.6f,  0.9f,  0f);
            c.MoveSwayAmount    = new(0.8f,  1.2f,  0f);
            c.AdsSwayMultiplier = new(0.20f, 0.30f, 0f);
        }

        private static void ApplySMG(WeaponCategoryData c)
        {
            c.Names     = new[] { "MP5", "UMP-45", "P90", "Vector", "MP7", "PP-19 Bizon" };
            c.FireModes = new[] { FireMode.Auto };

            c.RPM           = new(750,   1100,  0f);
            c.BurstCount    = new(3,     3);
            c.BurstInterval = new(0.05f, 0.07f);

            c.Damage             = new(15,    24,    0f);
            c.HeadshotMultiplier = new(1.75f, 2.25f, 0f);
            c.RangeOptimal       = new(20f,   35f,   0f);
            c.RangeFalloffEnd    = new(80f,   120f,  0f);
            c.DamageFalloffMin   = new(0.25f, 0.45f, 0f);

            // P90 is an outlier at 50 rounds; most SMGs are 20–32 → bias low
            c.MagazineSize      = new(20,   50,   -0.5f);
            c.ReserveAmmo       = new(60,   100,  0f);
            c.ReserveMultiplier = new(3f,   4f,    0f);

            c.ReloadTime          = new(1.8f, 2.5f, 0f);
            c.TacticalReloadTime  = new(1.4f, 2.1f, 0f);
            c.TacticalReloadBonus = new(0.2f, 0.5f, 0f);

            c.HipSpreadDeg   = new(1.5f, 2.8f, 0f);
            c.AdsSpreadDeg   = new(0.3f, 0.6f, 0f);
            c.AdsSpreadMultiplier = new(0.05f, 0.20f, 0f);
            c.SpreadPerShot  = new(0.5f, 0.9f, 0f);
            c.MaxSpread      = new(4f,   7f,   0f);
            c.SpreadRecovery = new(14f,  22f,  0f);   // recovers fast, high fire rate

            c.RecoilScaleVertical   = new(0.6f,  1.1f,  0f);
            c.RecoilJitterVertical  = new(0.118f, 0.294f, 0f);
            c.RecoilScaleHorizontal = new(0.5f,  0.9f,  0f);  // more erratic
            c.RecoilHorizontalBias  = new(-0.3f, 0.3f,  0f);
            c.MaxAccumulatedRecoil  = new(8f,    14f,   0f);
            c.MaxAccumulatedHorizontalRecoil = new(4f, 7f, 0f);

            c.RecoilRecoverySpeed         = new(5f, 7f, 0f);
            c.RecoilRecoveryFraction      = new(0.45f, 0.70f, 0f);
            c.RecoilRecoveryDelay         = new(0.08f, 0.14f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.80f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.30f, 0.50f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.08f, 0.18f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.08f, 0.18f, 0f);

            // Long mags reward sustained fire — heat builds slowly but climbs high.
            c.RecoilHeatPerShot          = new(0.035f, 0.05f, 0f);
            c.RecoilHeatDecay            = new(0.8f,   1.1f,  0f);
            c.RecoilHeatKickMultiplier   = new(1.5f,   1.9f,  0f);
            c.RecoilHeatJitterMultiplier = new(2.5f,   3.5f,  0f);

            // Light, snappy in the hands.
            c.LookSwayAmount    = new(0.3f,  0.45f, 0f);
            c.LookSwayRecovery  = new(12f,   16f,   0f);
            c.IdleSwayAmount    = new(0.2f,  0.35f, 0f);
            c.IdleSwaySpeed     = new(0.7f,  1.0f,  0f);
            c.MoveSwayAmount    = new(0.7f,  1.0f,  0f);
            c.AdsSwayMultiplier = new(0.25f, 0.35f, 0f);
        }

        private static void ApplyPistol(WeaponCategoryData c)
        {
            c.Names     = new[] { "M9", "Glock 17", "Desert Eagle", "USP-S", "P250", "Five-seveN" };
            c.FireModes = new[] { FireMode.Semi, FireMode.Semi, FireMode.Semi, FireMode.Auto };

            c.RPM           = new(300,   600,  0f);
            c.BurstCount    = new(2,     2);
            c.BurstInterval = new(0.08f, 0.12f);

            // Wide range: Glock ≈ 20 dmg, Deagle ≈ 55 dmg
            c.Damage             = new(20,    55,    0f);
            c.HeadshotMultiplier = new(2.0f,  2.5f,  0f);
            c.RangeOptimal       = new(15f,   25f,   0f);
            c.RangeFalloffEnd    = new(50f,   80f,   0f);
            c.DamageFalloffMin   = new(0.30f, 0.60f, 0f);

            // Deagle 7, Five-seveN 20, Glock 17 17 → slight low bias
            c.MagazineSize      = new(7,    20,   -0.2f);
            c.ReserveAmmo       = new(35,   80,   0f);
            c.ReserveMultiplier = new(3f,   5f,    0f);

            c.ReloadTime          = new(1.5f, 2.2f, 0f);
            c.TacticalReloadTime  = new(1.1f, 1.8f, 0f);
            c.TacticalReloadBonus = new(0.1f, 0.4f, 0f);

            c.HipSpreadDeg   = new(1.5f, 3.5f, 0f);
            c.AdsSpreadDeg   = new(0.4f, 0.9f, 0f);
            c.AdsSpreadMultiplier = new(0.10f, 0.25f, 0f);
            c.SpreadPerShot  = new(0.8f, 1.5f, 0f);  // bloom quickly
            c.MaxSpread      = new(5f,   9f,   0f);
            c.SpreadRecovery = new(10f,  16f,  0f);

            c.RecoilScaleVertical   = new(1.2f,  2.8f,  0f);  // heavy per shot
            c.RecoilJitterVertical  = new(0.1f,  0.25f, 0f);
            c.RecoilScaleHorizontal = new(0.3f,  0.9f,  0f);
            c.RecoilHorizontalBias  = new(-0.15f, 0.15f, 0f);
            c.MaxAccumulatedRecoil  = new(6f,    12f,   0f);   // small mag = small cap
            c.MaxAccumulatedHorizontalRecoil = new(3f, 6f, 0f);

            c.RecoilRecoverySpeed         = new(5f, 8f, 0f);
            c.RecoilRecoveryFraction      = new(0.60f, 0.90f, 0f);
            c.RecoilRecoveryDelay         = new(0.10f, 0.18f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.85f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.40f, 0.65f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.25f, 0.50f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.20f, 0.40f, 0f);

            // Semi-auto by nature — heat rises fast per shot, decays fast too.
            c.RecoilHeatPerShot          = new(0.08f, 0.12f, 0f);
            c.RecoilHeatDecay            = new(0.4f,  0.7f,  0f);
            c.RecoilHeatKickMultiplier   = new(1.2f,  1.5f,  0f);
            c.RecoilHeatJitterMultiplier = new(1.4f,  1.8f,  0f);

            // Very nimble — best-handling weapon class.
            c.LookSwayAmount    = new(0.25f, 0.35f, 0f);
            c.LookSwayRecovery  = new(14f,   18f,   0f);
            c.IdleSwayAmount    = new(0.2f,  0.3f,  0f);
            c.IdleSwaySpeed     = new(0.7f,  1.0f,  0f);
            c.MoveSwayAmount    = new(0.6f,  0.9f,  0f);
            c.AdsSwayMultiplier = new(0.30f, 0.40f, 0f);
        }

        private static void ApplySniper(WeaponCategoryData c)
        {
            c.Names     = new[] { "AWP", "Barrett M82A1", "L96A1", "M24", "Kar98k", "SV-98" };
            c.FireModes = new[] { FireMode.Semi };   // bolt-action feel

            c.RPM           = new(30,    80,    0f);
            c.BurstCount    = new(1,     1);
            c.BurstInterval = new(0.0f,  0.0f);

            c.Damage             = new(70,    160,   0f);  // one-shot potential
            c.HeadshotMultiplier = new(2.0f,  3.0f,  0f);  // devastating headshots
            c.RangeOptimal       = new(80f,   200f,  0f);
            c.RangeFalloffEnd    = new(400f,  1000f, 0f);
            c.DamageFalloffMin   = new(0.60f, 0.90f, 0f);  // retain damage at range

            c.MagazineSize      = new(5,    10,   0f);
            c.ReserveAmmo       = new(20,   35,   0f);
            c.ReserveMultiplier = new(2f,   3f,   0f);

            c.ReloadTime          = new(2.8f, 5.0f, 0f);
            c.TacticalReloadTime  = new(2.0f, 4.5f, 0f);
            c.TacticalReloadBonus = new(0.3f, 0.8f, 0f);

            c.HipSpreadDeg   = new(8f,    15f,   0f);     // terrible hipfire
            c.AdsSpreadDeg   = new(0.01f, 0.15f, -0.5f); // weighted toward very accurate
            c.AdsSpreadMultiplier = new(0f, 0.05f, 0f);
            c.SpreadPerShot  = new(2.0f,  4.0f,  0f);
            c.MaxSpread      = new(15f,   25f,   0f);
            c.SpreadRecovery = new(4f,    8f,    0f);     // slow recovery

            c.RecoilScaleVertical   = new(3.0f, 6.0f,  0f);  // massive kick
            c.RecoilJitterVertical  = new(0.067f, 0.178f, 0f);
            c.RecoilScaleHorizontal = new(0.8f, 1.8f,  0f);
            c.RecoilHorizontalBias  = new(-0.1f, 0.1f, 0f);
            c.MaxAccumulatedRecoil  = new(3f,   6f,    0f);  // one shot at a time
            c.MaxAccumulatedHorizontalRecoil = new(1.5f, 3f, 0f);

            c.RecoilRecoverySpeed         = new(3.5f, 5f, 0f);
            c.RecoilRecoveryFraction      = new(0.85f, 1f, 0f);
            c.RecoilRecoveryDelay         = new(0.18f, 0.35f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.95f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.05f, 0.05f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.05f, 0.05f, 0f);

            // Buildup barely matters — one shot at a time — but keep it low & fast-decaying.
            c.RecoilHeatPerShot          = new(0.25f, 0.4f, 0f);
            c.RecoilHeatDecay            = new(0.6f,  0.9f, 0f);
            c.RecoilHeatKickMultiplier   = new(1.1f,  1.3f, 0f);
            c.RecoilHeatJitterMultiplier = new(1.2f,  1.5f, 0f);

            // Heavy, wobbly weapon that stabilizes dramatically when scoped.
            c.LookSwayAmount    = new(0.75f, 1.0f,  0f);
            c.LookSwayRecovery  = new(6f,    9f,    0f);
            c.IdleSwayAmount    = new(0.7f,  1.0f,  0f);
            c.IdleSwaySpeed     = new(0.4f,  0.6f,  0f);
            c.MoveSwayAmount    = new(1.4f,  1.9f,  0f);
            c.AdsSwayMultiplier = new(0.05f, 0.15f, 0f); // scoped = rock steady
        }

        private static void ApplyLMG(WeaponCategoryData c)
        {
            c.Names     = new[] { "M249 SAW", "MG42", "RPK", "Negev", "M60", "PKM" };
            c.FireModes = new[] { FireMode.Auto };

            c.RPM           = new(600,   950,  0f);
            c.BurstCount    = new(3,     3);
            c.BurstInterval = new(0.07f, 0.10f);

            c.Damage             = new(28,    42,    0f);
            c.HeadshotMultiplier = new(1.75f, 2.0f,  0f);
            c.RangeOptimal       = new(40f,   70f,   0f);
            c.RangeFalloffEnd    = new(150f,  300f,  0f);
            c.DamageFalloffMin   = new(0.35f, 0.55f, 0f);

            // Belt-fed: 75 baseline, up to 200; weighted toward lower end of the belt range
            c.MagazineSize      = new(75,   200,  -0.3f);
            c.ReserveAmmo       = new(125,  225,  0f);
            c.ReserveMultiplier = new(1.5f, 2.5f,  0f);  // fewer spare belts

            c.ReloadTime          = new(4.5f, 8.0f, 0f);  // slow belt/drum swap
            c.TacticalReloadTime  = new(3.8f, 6.5f, 0f);
            c.TacticalReloadBonus = new(0.5f, 1.5f, 0f);

            c.HipSpreadDeg   = new(3.5f, 6.0f, 0f);   // wide hipfire
            c.AdsSpreadDeg   = new(0.6f, 1.2f, 0f);   // not precise even ADS (vibration)
            c.AdsSpreadMultiplier = new(0.20f, 0.40f, 0f);
            c.SpreadPerShot  = new(0.8f, 1.4f, 0f);
            c.MaxSpread      = new(10f,  16f,  0f);
            c.SpreadRecovery = new(6f,   10f,  0f);    // slow recovery

            c.RecoilScaleVertical   = new(1.2f,  2.2f,  0f);
            c.RecoilJitterVertical  = new(0.176f, 0.353f, 0f);
            c.RecoilScaleHorizontal = new(0.8f,  1.4f,  0f);
            c.RecoilHorizontalBias  = new(-0.25f, 0.25f, 0f);
            c.MaxAccumulatedRecoil  = new(20f,   35f,   0f);  // huge cap for sustained fire
            c.MaxAccumulatedHorizontalRecoil = new(10f, 17f, 0f);

            c.RecoilRecoverySpeed         = new(3f, 5f, 0f);
            c.RecoilRecoveryFraction      = new(0.30f, 0.55f, 0f);
            c.RecoilRecoveryDelay         = new(0.10f, 0.18f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.75f, 0.90f, 0f);
            c.AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.20f, 0.35f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.18f, 0.30f, 0f);

            // Long belts + slow decay = feared "hot LMG" territory during held triggers.
            c.RecoilHeatPerShot          = new(0.025f, 0.035f, 0f);
            c.RecoilHeatDecay            = new(0.3f,   0.5f,   0f);
            c.RecoilHeatKickMultiplier   = new(1.7f,   2.2f,   0f);
            c.RecoilHeatJitterMultiplier = new(3.0f,   4.0f,   0f);

            // Heaviest handling in the roster.
            c.LookSwayAmount    = new(0.85f, 1.0f,  0f);
            c.LookSwayRecovery  = new(5f,    7f,    0f);
            c.IdleSwayAmount    = new(0.5f,  0.8f,  0f);
            c.IdleSwaySpeed     = new(0.4f,  0.6f,  0f);
            c.MoveSwayAmount    = new(1.6f,  2.2f,  0f);
            c.AdsSwayMultiplier = new(0.20f, 0.35f, 0f);
        }

        private static void ApplyShotgun(WeaponCategoryData c)
        {
            c.Names     = new[] { "M870", "SPAS-12", "Benelli M3", "Mossberg 500", "KSG", "AA-12" };
            c.FireModes = new[] { FireMode.Semi, FireMode.Semi, FireMode.Semi, FireMode.Auto };

            c.RPM           = new(60,    120,   0f);   // pump ≈ 60 RPM, semi-auto ≈ 120 RPM
            c.BurstCount    = new(1,     1);
            c.BurstInterval = new(0.0f,  0.0f);
            c.PelletCount   = new(8,     12,    0f);

            // Damage is per pellet — 9 pellets × 12 dmg = 108 total at close range
            c.Damage             = new(10f,   15f,   0f);
            c.HeadshotMultiplier = new(1.5f,  2.0f,  0f);  // headshots less decisive — pellet spread
            c.RangeOptimal       = new(8f,    15f,   0f);   // very short effective range
            c.RangeFalloffEnd    = new(25f,   45f,   0f);
            c.DamageFalloffMin   = new(0.10f, 0.25f, 0f);  // steep dropoff

            c.MagazineSize      = new(5,     8,     0f);
            c.ReserveAmmo       = new(20,    36,    0f);
            c.ReserveMultiplier = new(3f,    5f,    0f);

            c.ReloadTime          = new(2.5f, 4.5f, 0f);   // shell-by-shell reload takes longer
            c.TacticalReloadTime  = new(2.0f, 3.5f, 0f);
            c.TacticalReloadBonus = new(0.3f, 1.0f, 0f);

            c.HipSpreadDeg   = new(8f,    15f,   0f);   // wide pellet cone hip
            c.AdsSpreadDeg   = new(4f,    10f,   0f);   // tighter but still wide ADS
            c.AdsSpreadMultiplier = new(0.30f, 0.60f, 0f);
            c.SpreadPerShot  = new(0.5f,  1.5f,  0f);   // bloom per shot (between shots)
            c.MaxSpread      = new(6f,    12f,   0f);
            c.SpreadRecovery = new(8f,    14f,   0f);

            c.RecoilScaleVertical   = new(2.5f,  5.0f,  0f);  // heavy kick per shot
            c.RecoilJitterVertical  = new(0.08f, 0.213f, 0f);
            c.RecoilScaleHorizontal = new(0.5f,  1.2f,  0f);
            c.RecoilHorizontalBias  = new(-0.15f, 0.15f, 0f);
            c.MaxAccumulatedRecoil  = new(4f,    8f,    0f);  // low cap — one shot at a time
            c.MaxAccumulatedHorizontalRecoil = new(2f, 4f, 0f);

            c.RecoilRecoverySpeed         = new(4f,   6f,    0f);
            c.RecoilRecoveryFraction      = new(0.70f, 1.0f,  0f);
            c.RecoilRecoveryDelay         = new(0.15f, 0.30f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.85f, 1.0f,  0f);
            c.AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.30f, 0.55f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.20f, 0.40f, 0f);

            // One shot at a time — heat mostly irrelevant, but adds character to auto shotguns.
            c.RecoilHeatPerShot          = new(0.20f, 0.30f, 0f);
            c.RecoilHeatDecay            = new(0.5f,  0.8f,  0f);
            c.RecoilHeatKickMultiplier   = new(1.15f, 1.35f, 0f);
            c.RecoilHeatJitterMultiplier = new(1.3f,  1.6f,  0f);

            // Chunky, hard to swing quickly.
            c.LookSwayAmount    = new(0.55f, 0.75f, 0f);
            c.LookSwayRecovery  = new(7f,    10f,   0f);
            c.IdleSwayAmount    = new(0.35f, 0.55f, 0f);
            c.IdleSwaySpeed     = new(0.5f,  0.7f,  0f);
            c.MoveSwayAmount    = new(1.1f,  1.5f,  0f);
            c.AdsSwayMultiplier = new(0.25f, 0.35f, 0f);
        }
    }
}
