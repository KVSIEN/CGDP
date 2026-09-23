using CGD.Items;

namespace CGD.Weapons
{
    // Per-type stat ranges for the "Apply Type Defaults" context menu.
    // Each band spans the full subtype spectrum (e.g. CQB carbine to battle rifle).
    // Tradeoff axes in StatRollProfile decide where in each band a roll lands;
    // without a profile every stat rolls independently and can stack the best ends.
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

        // 4–5 STK, ~0.35s TTK, 30–60m. Band: CQB carbine (900 RPM) → battle rifle (450 RPM).
        private static void ApplyAR(WeaponCategoryData c)
        {
            c.Names     = new[] { "M4A1", "AK-47", "SCAR-L", "HK416", "AR-15", "M16A4",
                                  "G3A3", "FN FAL", "AUG A3", "Galil ACE" };
            c.FireModes = new[] { FireMode.Auto, FireMode.Auto, FireMode.Auto, FireMode.Semi, FireMode.Burst };

            c.RPM           = new(450,   900,  0f);
            c.BurstCount    = new(3,     3);
            c.BurstInterval = new(0.06f, 0.10f);

            c.Damage             = new(18,    34,    0f);
            c.HeadshotMultiplier = new(2f,    2.3f,  0f);
            c.RangeOptimal       = new(30f,   90f,   0f);   // carbine → marksman barrel
            c.RangeFalloffEnd    = new(110f,  260f,  0f);
            c.DamageFalloffMin   = new(0.30f, 0.55f, 0f);

            c.AmmoType          = AmmoType.StandardRounds;
            c.MagazineSize      = new(20,    40,   0f);   // 20-rd battle rifle → 40-rd carbine

            c.ReloadTime          = new(2.0f, 3.2f, 0f);
            c.TacticalReloadTime  = new(1.6f, 2.6f, 0f);
            c.TacticalReloadBonus = new(0.3f, 0.6f, 0f);

            c.DrawTime       = new(0.50f, 0.80f, 0f);
            c.HipSpreadDeg   = new(1.8f, 4.5f, 0f);
            c.AdsSpreadDeg   = new(0.01f, 0.01f, 0f);
            c.AdsSpreadMultiplier = new(0f, 0f, 0f);      // no ADS bloom
            c.SpreadPerShot  = new(0.5f, 1.2f, 0f);
            c.MaxSpread      = new(4.5f, 9f,   0f);
            c.SpreadRecovery = new(10f,  19f,  0f);

            c.RecoilScaleVertical   = new(0.7f,   2.2f,  0f);  // carbine soft → battle rifle hard
            c.RecoilJitterVertical  = new(0.083f, 0.25f, 0f);
            c.RecoilScaleHorizontal = new(0.35f,  0.80f, 0f);
            c.RecoilHorizontalBias  = new(-0.2f,  0.2f,  0f);
            c.MaxAccumulatedRecoil  = new(10f,    20f,   0f);
            c.MaxAccumulatedHorizontalRecoil = new(5f, 10f, 0f);

            c.RecoilHeatPerShot       = new(0.04f, 0.10f, 0f);  // full heat after ~10–25 rounds
            c.RecoilHeatCooldown      = new(1.2f,  1.8f,  0f);
            c.MaxHeatRecoilMultiplier = new(1.3f,  1.6f,  0f);
            c.RecoilHeatJitterMultiplier = new(1.5f,  2.0f,  0f);
            c.HotAdsSpreadMultiplier  = new(0f,    0f,    0f);

            c.RecoilRecoverySpeed         = new(5f, 7.5f, 0f);
            c.RecoilRecoveryFraction      = new(0.55f, 0.80f, 0f);
            c.RecoilRecoveryDelay         = new(0.10f, 0.16f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.85f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.35f, 0.55f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.10f, 0.25f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.10f, 0.20f, 0f);

            c.LookSwayAmount    = new(0.35f, 0.70f, 0f);
            c.LookSwayRecovery  = new(8f,    13f,   0f);
            c.IdleSwayAmount    = new(0.25f, 0.55f, 0f);
            c.IdleSwaySpeed     = new(0.6f,  0.9f,  0f);
            c.MoveSwayAmount    = new(0.7f,  1.4f,  0f);

            c.AdsFovDeg = new(34f, 50f, 0f);   // marksman variants zoom deeper
            c.AdsSpeed  = new(7f,  13f, 0f);
        }

        // 5–6 STK, ~0.35s TTK, 5–20m. Band: PDW (1200 RPM) → heavy SMG (600 RPM).
        private static void ApplySMG(WeaponCategoryData c)
        {
            c.Names     = new[] { "MP5", "UMP-45", "P90", "Vector", "MP7", "PP-19 Bizon",
                                  "Thompson", "MAC-10", "Scorpion EVO" };
            c.FireModes = new[] { FireMode.Auto };

            c.RPM           = new(600,   1200,  0f);
            c.BurstCount    = new(3,     3);
            c.BurstInterval = new(0.04f, 0.07f);

            c.Damage             = new(13,    28,    0f);
            c.HeadshotMultiplier = new(1.75f, 2.25f, 0f);
            c.RangeOptimal       = new(12f,   40f,   0f);
            c.RangeFalloffEnd    = new(55f,   130f,  0f);
            c.DamageFalloffMin   = new(0.25f, 0.45f, 0f);

            c.AmmoType          = AmmoType.LightRounds;
            // P90 is an outlier at 50 rounds; most SMGs are 20–32 → bias low
            c.MagazineSize      = new(20,   50,   -0.3f);

            c.ReloadTime          = new(1.6f, 2.7f, 0f);
            c.TacticalReloadTime  = new(1.2f, 2.2f, 0f);
            c.TacticalReloadBonus = new(0.2f, 0.5f, 0f);

            c.DrawTime       = new(0.35f, 0.60f, 0f);
            c.HipSpreadDeg   = new(1.3f, 3.2f, 0f);
            c.AdsSpreadDeg   = new(0.01f, 0.01f, 0f);
            c.AdsSpreadMultiplier = new(0f, 0f, 0f);      // no ADS bloom
            c.SpreadPerShot  = new(0.4f, 1.0f, 0f);
            c.MaxSpread      = new(3.5f, 8f,   0f);
            c.SpreadRecovery = new(12f,  24f,  0f);   // recovers fast, high fire rate

            c.RecoilScaleVertical   = new(0.45f, 1.4f,  0f);
            c.RecoilJitterVertical  = new(0.118f, 0.294f, 0f);
            c.RecoilScaleHorizontal = new(0.4f,  1.0f,  0f);  // more erratic
            c.RecoilHorizontalBias  = new(-0.3f, 0.3f,  0f);
            c.MaxAccumulatedRecoil  = new(7f,    16f,   0f);
            c.MaxAccumulatedHorizontalRecoil = new(3.5f, 8f, 0f);

            c.RecoilHeatPerShot       = new(0.03f, 0.07f, 0f);  // high RPM, so less per round
            c.RecoilHeatCooldown      = new(1.5f,  2.2f,  0f);
            c.MaxHeatRecoilMultiplier = new(1.3f,  1.6f,  0f);
            c.RecoilHeatJitterMultiplier = new(1.8f,  2.5f,  0f);
            c.HotAdsSpreadMultiplier  = new(0f,    0f,    0f);

            c.RecoilRecoverySpeed         = new(5f, 7f, 0f);
            c.RecoilRecoveryFraction      = new(0.45f, 0.70f, 0f);
            c.RecoilRecoveryDelay         = new(0.08f, 0.14f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.80f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.30f, 0.50f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.08f, 0.18f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.08f, 0.18f, 0f);

            // Light, snappy in the hands; heavy SMGs a little less so.
            c.LookSwayAmount    = new(0.25f, 0.50f, 0f);
            c.LookSwayRecovery  = new(11f,   17f,   0f);
            c.IdleSwayAmount    = new(0.18f, 0.40f, 0f);
            c.IdleSwaySpeed     = new(0.7f,  1.0f,  0f);
            c.MoveSwayAmount    = new(0.6f,  1.1f,  0f);

            // Light zoom, snappy aim — SMG needs peripheral vision for CQB.
            c.AdsFovDeg = new(45f, 58f, 0f);
            c.AdsSpeed  = new(12f, 20f, 0f);
        }

        // 3–4 STK, ~0.5s TTK, 15–25m. Band: Glock (600 RPM) → hand cannon (200 RPM).
        private static void ApplyPistol(WeaponCategoryData c)
        {
            c.Names     = new[] { "M9", "Glock 17", "Desert Eagle", "USP-S", "P250", "Five-seveN",
                                  "M1911", ".44 Magnum", "Glock 18", "CZ-75" };
            c.FireModes = new[] { FireMode.Semi, FireMode.Semi, FireMode.Semi, FireMode.Auto };

            c.RPM           = new(200,   600,  0f);
            c.BurstCount    = new(2,     2);
            c.BurstInterval = new(0.08f, 0.12f);

            // Wide band: Glock ≈ 20 dmg, Deagle ≈ 60 dmg
            c.Damage             = new(20,    60,    0f);
            c.HeadshotMultiplier = new(2.0f,  2.5f,  0f);
            c.RangeOptimal       = new(12f,   35f,   0f);
            c.RangeFalloffEnd    = new(45f,   90f,   0f);
            c.DamageFalloffMin   = new(0.30f, 0.60f, 0f);

            // Standard pistols share the SMG light pool; hand-cannon variants (Deagle,
            // .44 Magnum revolvers) override to HeavyRounds on their individual WeaponData.
            c.AmmoType          = AmmoType.LightRounds;
            // Revolver 6, Deagle 7, Glock 17 17, Five-seveN 20 → slight low bias
            c.MagazineSize      = new(6,    20,   -0.2f);

            c.ReloadTime          = new(1.3f, 2.4f, 0f);
            c.TacticalReloadTime  = new(1.0f, 1.9f, 0f);
            c.TacticalReloadBonus = new(0.1f, 0.4f, 0f);

            c.DrawTime       = new(0.30f, 0.60f, 0f);
            c.HipSpreadDeg   = new(1.3f, 4.0f, 0f);
            c.AdsSpreadDeg   = new(0.01f, 0.01f, 0f);
            c.AdsSpreadMultiplier = new(0f, 0f, 0f);      // no ADS bloom
            c.SpreadPerShot  = new(0.8f, 1.6f, 0f);  // bloom quickly
            c.MaxSpread      = new(4.5f, 10f,  0f);
            c.SpreadRecovery = new(9f,   17f,  0f);

            c.RecoilScaleVertical   = new(0.9f,  3.6f,  0f);  // Glock light → Deagle brutal
            c.RecoilJitterVertical  = new(0.1f,  0.25f, 0f);
            c.RecoilScaleHorizontal = new(0.25f, 1.0f,  0f);
            c.RecoilHorizontalBias  = new(-0.15f, 0.15f, 0f);
            c.MaxAccumulatedRecoil  = new(5f,    13f,   0f);   // small mag = small cap
            c.MaxAccumulatedHorizontalRecoil = new(2.5f, 6.5f, 0f);

            c.RecoilHeatPerShot       = new(0.08f, 0.15f, 0f);  // punishes spamming the trigger
            c.RecoilHeatCooldown      = new(1.5f,  2.5f,  0f);
            c.MaxHeatRecoilMultiplier = new(1.2f,  1.4f,  0f);
            c.RecoilHeatJitterMultiplier = new(1.3f,  1.6f,  0f);
            c.HotAdsSpreadMultiplier  = new(0f,    0f,    0f);

            c.RecoilRecoverySpeed         = new(5f, 8f, 0f);
            c.RecoilRecoveryFraction      = new(0.60f, 0.90f, 0f);
            c.RecoilRecoveryDelay         = new(0.10f, 0.18f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.85f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.40f, 0.65f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.25f, 0.50f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.20f, 0.40f, 0f);

            // Very nimble — best-handling weapon class.
            c.LookSwayAmount    = new(0.20f, 0.45f, 0f);
            c.LookSwayRecovery  = new(13f,   19f,   0f);
            c.IdleSwayAmount    = new(0.15f, 0.40f, 0f);
            c.IdleSwaySpeed     = new(0.7f,  1.0f,  0f);
            c.MoveSwayAmount    = new(0.5f,  1.0f,  0f);

            // Minimal zoom, snappiest aim in the roster — iron sights on a small gun.
            c.AdsFovDeg = new(48f, 60f, 0f);
            c.AdsSpeed  = new(12f, 20f, 0f);
        }

        // 1 STK, 0.6–1.2s TTK, 80–200m. Band: semi-auto DMR (260 RPM) → bolt-action (35 RPM).
        private static void ApplySniper(WeaponCategoryData c)
        {
            c.Names     = new[] { "AWP", "Barrett M82A1", "L96A1", "M24", "Kar98k", "SV-98",
                                  "SVD Dragunov", "SR-25", "Mk 14 EBR" };
            c.FireModes = new[] { FireMode.Semi };   // bolt and semi-auto both fire one per click

            c.RPM           = new(35,    260,   0f);
            c.BurstCount    = new(1,     1);
            c.BurstInterval = new(0.0f,  0.0f);

            c.Damage             = new(45,    170,   0f);  // DMR two-tap → AMR one-shot
            c.HeadshotMultiplier = new(2.0f,  3.0f,  0f);  // devastating headshots
            c.RangeOptimal       = new(50f,   220f,  0f);
            c.RangeFalloffEnd    = new(250f,  1000f, 0f);
            c.DamageFalloffMin   = new(0.60f, 0.90f, 0f);  // retain damage at range

            c.AmmoType          = AmmoType.HeavyRounds;
            c.MagazineSize      = new(5,    20,   -0.3f);  // 5-rd bolt → 20-rd SR-25

            c.ReloadTime          = new(2.4f, 5.0f, 0f);
            c.TacticalReloadTime  = new(1.9f, 4.5f, 0f);
            c.TacticalReloadBonus = new(0.3f, 0.8f, 0f);

            c.DrawTime       = new(0.60f, 1.00f, 0f);
            c.HipSpreadDeg   = new(6f,    15f,   0f);     // terrible hipfire
            c.AdsSpreadDeg   = new(0.01f, 0.01f, 0f);
            c.AdsSpreadMultiplier = new(0f, 0f, 0f);      // no ADS bloom
            c.SpreadPerShot  = new(1.5f,  4.0f,  0f);
            c.MaxSpread      = new(12f,   25f,   0f);
            c.SpreadRecovery = new(4f,    10f,   0f);     // slow recovery

            c.RecoilScaleVertical   = new(1.8f, 6.0f,  0f);  // DMR manageable → AMR massive
            c.RecoilJitterVertical  = new(0.067f, 0.178f, 0f);
            c.RecoilScaleHorizontal = new(0.6f, 1.8f,  0f);
            c.RecoilHorizontalBias  = new(-0.1f, 0.1f, 0f);
            c.MaxAccumulatedRecoil  = new(3f,   8f,    0f);
            c.MaxAccumulatedHorizontalRecoil = new(1.5f, 4f, 0f);

            c.RecoilHeatPerShot       = new(0f,    0.04f, 0f);  // only the fast DMR end builds heat
            c.RecoilHeatCooldown      = new(1.5f,  2.5f,  0f);
            c.MaxHeatRecoilMultiplier = new(1f,    1.3f,  0f);
            c.RecoilHeatJitterMultiplier = new(1f,    1.4f,  0f);
            c.HotAdsSpreadMultiplier  = new(0f,    0f,    0f);

            c.RecoilRecoverySpeed         = new(3.5f, 6f, 0f);
            c.RecoilRecoveryFraction      = new(0.85f, 1f, 0f);
            c.RecoilRecoveryDelay         = new(0.15f, 0.35f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.95f, 1f, 0f);
            c.AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.05f, 0.05f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.05f, 0.05f, 0f);

            // Heavy, wobbly weapon from the hip; DMRs a little lighter.
            c.LookSwayAmount    = new(0.55f, 1.0f,  0f);
            c.LookSwayRecovery  = new(6f,    11f,   0f);
            c.IdleSwayAmount    = new(0.45f, 1.0f,  0f);
            c.IdleSwaySpeed     = new(0.4f,  0.6f,  0f);
            c.MoveSwayAmount    = new(1.0f,  1.9f,  0f);

            // DMR glass → deep AMR scope, and a correspondingly slower scope-in.
            c.AdsFovDeg = new(14f, 34f, 0f);
            c.AdsSpeed  = new(3f,  8f,  0f);
        }

        // 3–4 STK, ~0.25s TTK, 30–70m. Band: high-cyclic MG (1200 RPM) → GPMG (500 RPM).
        private static void ApplyLMG(WeaponCategoryData c)
        {
            c.Names     = new[] { "M249 SAW", "MG42", "RPK", "Negev", "M60", "PKM",
                                  "MG3", "Stoner 63" };
            c.FireModes = new[] { FireMode.Auto };

            c.RPM           = new(500,   1200, 0f);
            c.BurstCount    = new(3,     3);
            c.BurstInterval = new(0.05f, 0.10f);

            c.Damage             = new(24,    45,    0f);
            c.HeadshotMultiplier = new(1.75f, 2.0f,  0f);
            c.RangeOptimal       = new(35f,   80f,   0f);
            c.RangeFalloffEnd    = new(140f,  320f,  0f);
            c.DamageFalloffMin   = new(0.35f, 0.55f, 0f);

            c.AmmoType          = AmmoType.StandardRounds;
            // Box-fed RPK at 50 up to a 200-round belt; weighted toward the lower end
            c.MagazineSize      = new(50,   200,  -0.3f);

            c.ReloadTime          = new(4.0f, 8.5f, 0f);  // slow belt/drum swap
            c.TacticalReloadTime  = new(3.4f, 7.0f, 0f);
            c.TacticalReloadBonus = new(0.5f, 1.5f, 0f);

            c.DrawTime       = new(0.75f, 1.15f, 0f);
            c.HipSpreadDeg   = new(3.0f, 6.5f, 0f);   // wide hipfire
            c.AdsSpreadDeg   = new(0.01f, 0.01f, 0f);
            c.AdsSpreadMultiplier = new(0f, 0f, 0f);      // no ADS bloom
            c.SpreadPerShot  = new(0.7f, 1.5f, 0f);
            c.MaxSpread      = new(9f,   17f,  0f);
            c.SpreadRecovery = new(6f,   11f,  0f);    // slow recovery

            c.RecoilScaleVertical   = new(1.0f,  2.4f,  0f);
            c.RecoilJitterVertical  = new(0.176f, 0.353f, 0f);
            c.RecoilScaleHorizontal = new(0.7f,  1.5f,  0f);
            c.RecoilHorizontalBias  = new(-0.25f, 0.25f, 0f);
            c.MaxAccumulatedRecoil  = new(18f,   38f,   0f);  // huge cap for sustained fire
            c.MaxAccumulatedHorizontalRecoil = new(9f, 18f, 0f);

            // Big belt: heat builds over ~25–50 rounds and cools slowly, so the first
            // stretch of a burst is accurate and long sustained fire becomes a hose.
            c.RecoilHeatPerShot       = new(0.02f, 0.04f, 0f);
            c.RecoilHeatCooldown      = new(0.8f,  1.2f,  0f);
            c.MaxHeatRecoilMultiplier = new(1.5f,  1.9f,  0f);
            c.RecoilHeatJitterMultiplier = new(2.0f,  2.8f,  0f);
            c.HotAdsSpreadMultiplier  = new(0f,    0f,    0f);

            c.RecoilRecoverySpeed         = new(3f, 5f, 0f);
            c.RecoilRecoveryFraction      = new(0.30f, 0.55f, 0f);
            c.RecoilRecoveryDelay         = new(0.10f, 0.18f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.75f, 0.90f, 0f);
            c.AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.20f, 0.35f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.18f, 0.30f, 0f);

            // Heaviest handling in the roster.
            c.LookSwayAmount    = new(0.70f, 1.0f,  0f);
            c.LookSwayRecovery  = new(5f,    8f,    0f);
            c.IdleSwayAmount    = new(0.45f, 0.85f, 0f);
            c.IdleSwaySpeed     = new(0.4f,  0.6f,  0f);
            c.MoveSwayAmount    = new(1.4f,  2.2f,  0f);

            // Moderate zoom, slow shoulder mount — the LMG is big and heavy.
            c.AdsFovDeg = new(40f, 52f, 0f);
            c.AdsSpeed  = new(4f,  8f,  0f);
        }

        // 1 STK at 3–8m, useless past 20m. Band: pump (60 RPM) → auto (260 RPM).
        // PelletCount stays ≥ 6 — slugs need a separate category or PelletCount↔Damage axis.
        private static void ApplyShotgun(WeaponCategoryData c)
        {
            c.Names     = new[] { "M870", "SPAS-12", "Benelli M3", "Mossberg 500", "KSG", "AA-12",
                                  "Saiga-12", "Winchester 1897" };
            c.FireModes = new[] { FireMode.Semi, FireMode.Semi, FireMode.Semi, FireMode.Auto };

            c.RPM           = new(50,    260,   0f);   // pump ≈ 60 RPM, auto ≈ 260 RPM
            c.BurstCount    = new(1,     1);
            c.BurstInterval = new(0.0f,  0.0f);
            c.PelletCount   = new(6,     12,    0f);

            // Damage is per pellet — 8 pellets × 12 dmg = 96 total at close range
            c.Damage             = new(9f,    18f,   0f);
            c.HeadshotMultiplier = new(1.5f,  2.0f,  0f);  // headshots less decisive — pellet spread
            c.RangeOptimal       = new(6f,    20f,   0f);   // very short effective range
            c.RangeFalloffEnd    = new(22f,   55f,   0f);
            c.DamageFalloffMin   = new(0.10f, 0.25f, 0f);  // steep dropoff

            c.AmmoType          = AmmoType.ShotgunShells;
            c.MagazineSize      = new(4,     10,    0f);

            c.ReloadTime          = new(2.2f, 4.5f, 0f);   // shell-by-shell reload takes longer
            c.TacticalReloadTime  = new(1.8f, 3.5f, 0f);
            c.TacticalReloadBonus = new(0.3f, 1.0f, 0f);

            c.DrawTime       = new(0.50f, 0.85f, 0f);
            c.HipSpreadDeg   = new(6f,    16f,   0f);   // wide pellet cone hip
            c.AdsSpreadDeg   = new(1.5f,  4f,    0f);   // roughly a quarter of the hip cone
            c.AdsSpreadMultiplier = new(0.30f, 0.60f, 0f);
            c.SpreadPerShot  = new(0.5f,  1.5f,  0f);   // bloom per shot (between shots)
            c.MaxSpread      = new(6f,    12f,   0f);
            c.SpreadRecovery = new(8f,    16f,   0f);

            c.RecoilScaleVertical   = new(1.6f,  5.0f,  0f);  // auto lighter, pump brutal
            c.RecoilJitterVertical  = new(0.08f, 0.213f, 0f);
            c.RecoilScaleHorizontal = new(0.4f,  1.2f,  0f);
            c.RecoilHorizontalBias  = new(-0.15f, 0.15f, 0f);
            c.MaxAccumulatedRecoil  = new(4f,    10f,   0f);
            c.MaxAccumulatedHorizontalRecoil = new(2f, 5f, 0f);

            c.RecoilHeatPerShot       = new(0f,    0.05f, 0f);  // only matters at auto-shotgun RPM
            c.RecoilHeatCooldown      = new(1.5f,  2.5f,  0f);
            c.MaxHeatRecoilMultiplier = new(1f,    1.2f,  0f);
            c.RecoilHeatJitterMultiplier = new(1.2f,  1.4f,  0f);
            c.HotAdsSpreadMultiplier  = new(0.35f, 0.65f, 0f);

            c.RecoilRecoverySpeed         = new(4f,   7f,    0f);
            c.RecoilRecoveryFraction      = new(0.70f, 1.0f,  0f);
            c.RecoilRecoveryDelay         = new(0.12f, 0.30f, 0f);
            c.AdsRecoilRecoveryFraction   = new(0.85f, 1.0f,  0f);
            c.AdsRecoilMultiplier         = new(0.55f, 0.75f, 0f);
            c.HipRecoilVerticalMultiplier = new(0.30f, 0.55f, 0f);
            c.HipRecoilHorizontalMultiplier = new(0.20f, 0.40f, 0f);

            // Chunky, hard to swing quickly.
            c.LookSwayAmount    = new(0.45f, 0.80f, 0f);
            c.LookSwayRecovery  = new(7f,    11f,   0f);
            c.IdleSwayAmount    = new(0.30f, 0.60f, 0f);
            c.IdleSwaySpeed     = new(0.5f,  0.7f,  0f);
            c.MoveSwayAmount    = new(0.9f,  1.6f,  0f);

            // Minimal zoom — close-range weapon — moderate aim speed.
            c.AdsFovDeg = new(45f, 58f, 0f);
            c.AdsSpeed  = new(8f,  13f, 0f);
        }
    }
}
