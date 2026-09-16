using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    // Create one asset per weapon type via Assets > Create > CGD > Weapons > Weapon Category.
    // Right-click the asset and choose "Apply Type Defaults" to auto-fill realistic thresholds,
    // then tweak individual ranges as needed.
    [CreateAssetMenu(fileName = "WeaponCategory", menuName = "CGD/Weapons/Weapon Category")]
    public class WeaponCategoryData : ScriptableObject
    {
        [Header("Identity")]
        public WeaponType Type;
        public string[]   Names = { "Weapon" };

        // FireMode entries can be repeated to weight the random pick.
        // e.g. { Auto, Auto, Auto, Semi } = 75% Auto, 25% Semi.
        public FireMode[] FireModes = { FireMode.Auto };

        [Header("Firing")]
        public WeaponFireBehavior FireBehavior;
        public FloatRange RPM          = new(600, 800);
        public IntRange   BurstCount   = new(3, 3);    // only used if Burst is in FireModes
        public FloatRange BurstInterval = new(0.07f, 0.10f);
        [Tooltip("Pellets per shot. 1 for all non-shotgun types.")]
        public IntRange   PelletCount  = new(1, 1);

        [Header("On Hit")]
        [Tooltip("Status effects each hit may apply")]
        public StatusEffectApplication[] OnHitEffects;

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
        [Tooltip("How far away enemies hear it (0 = silent)")]
        public float NoiseRadius = 40f;

        [Header("Spread")]
        public FloatRange HipSpreadDeg          = new(2.0f, 3.5f);
        public FloatRange AdsSpreadDeg          = new(0.2f, 0.5f);
        public FloatRange AdsSpreadMultiplier   = new(0f, 0.15f);
        public FloatRange SpreadPerShot         = new(0.6f, 1.0f);
        public FloatRange MaxSpread      = new(5f,   8f);
        public FloatRange SpreadRecovery = new(12f,  18f);

        [Header("Recoil")]
        public FloatRange RecoilScaleVertical   = new(0.9f, 1.5f);
        public FloatRange RecoilJitterVertical  = new(0.083f, 0.25f);
        public FloatRange RecoilScaleHorizontal = new(0.4f, 0.7f);
        public FloatRange RecoilHorizontalBias  = new(-0.2f, 0.2f);  // drift direction
        public FloatRange MaxAccumulatedRecoil  = new(12f,  18f);
        public FloatRange MaxAccumulatedHorizontalRecoil = new(6f, 9f);

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
        public void ApplyTypeDefaults() => WeaponCategoryDefaults.Apply(this);
    }
}
