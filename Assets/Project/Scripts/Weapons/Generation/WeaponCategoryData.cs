using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Items;

namespace CGD.Weapons
{
    // Create one asset per weapon type via Assets > Create > CGD > Weapons > Weapon Category.
    // Right-click the asset and choose "Apply Type Defaults" to auto-fill realistic thresholds,
    // then tweak individual ranges as needed.
    [CreateAssetMenu(fileName = "WeaponCategory", menuName = "CGD/Weapons/Weapon Category")]
    public class WeaponCategoryData : WeaponCategory
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

        [Header("Charge & Projectile")]
        [Tooltip("Seconds to reach full draw (Charge fire mode only)")]
        public FloatRange ChargeTime                 = new(0.8f, 0.8f);
        [Tooltip("Releasing before this fraction of a full draw cancels the shot (Charge fire mode only)")]
        public FloatRange MinChargeToFire            = new(0.15f, 0.15f);
        [Tooltip("Muzzle velocity in m/s. Bullets ~400–1000, arrows ~40–80, bolts ~70–120")]
        public FloatRange ProjectileSpeed            = new(900f, 900f);
        [Tooltip("Downward acceleration in m/s². 0 = straight flight; arrows arc")]
        public FloatRange ProjectileGravity          = new(0f, 0f);
        public FloatRange ProjectileLifetime         = new(5f, 5f);
        [Tooltip("Flight time resolved instantly as a raycast. 0 = always a visible projectile")]
        public FloatRange ProjectileInstantHitTime   = new(0.02f, 0.02f);
        [Tooltip("Speed, gravity and damage multipliers for a minimum-charge release (Charge fire mode only)")]
        public FloatRange LowChargeSpeedMultiplier   = new(0.4f, 0.4f);
        public FloatRange LowChargeGravityMultiplier = new(3f, 3f);
        public FloatRange LowChargeDamageMultiplier  = new(1f, 1f);

        [Header("Bows")]
        [Tooltip("Bows: the Weapon Mode key switches between a vertical and a horizontal draw")]
        public bool       HasDrawStances;
        public DrawStance VerticalDraw   = new(1.25f, 1.15f, 0.6f);
        public DrawStance HorizontalDraw = new(0.65f, 0.8f,  1.8f);
        [Tooltip("Angle between neighbouring arrows when a bow rolls more than one arrow (Pellet Count)")]
        public FloatRange VolleySpacingDeg = new(3f, 3f);

        [Header("On Hit")]
        [Tooltip("Status effects each hit may apply")]
        public StatusEffectApplication[] OnHitEffects;

        [Header("Damage")]
        public FloatRange Damage              = new(25, 35);
        public DamageType DamageType          = DamageType.Physical;
        public FloatRange ArmorPenetration    = new(0f, 0f);
        public FloatRange HeadshotMultiplier  = new(2f, 2f);
        public FloatRange RangeOptimal        = new(40, 60);
        public FloatRange RangeFalloffEnd     = new(120, 200);
        public FloatRange DamageFalloffMin    = new(0.3f, 0.5f);

        [Header("Ammo")]
        [Tooltip("Which shared ammo pool weapons in this category draw from.")]
        public AmmoType   AmmoType           = AmmoType.StandardRounds;
        public IntRange   MagazineSize       = new(25, 35);
        [Tooltip("Magazine for weapons that roll Auto or Burst (e.g. an autoloading crossbow). 0–0 = use Magazine Size")]
        public IntRange   AutomaticMagazineSize = new(0, 0);

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
        public FloatRange AdsSpreadDeg          = new(0.01f, 0.01f);
        public FloatRange AdsSpreadMultiplier   = new(0f, 0f);
        public FloatRange SpreadPerShot         = new(0.6f, 1.0f);
        public FloatRange MaxSpread      = new(5f,   8f);
        public FloatRange SpreadRecovery = new(12f,  18f);

        [Header("Control — Kick")]
        public FloatRange RecoilScaleVertical   = new(0.9f, 1.5f);
        public FloatRange RecoilJitterVertical  = new(0.083f, 0.25f);
        public FloatRange RecoilScaleHorizontal = new(0.4f, 0.7f);
        public FloatRange RecoilHorizontalBias  = new(-0.2f, 0.2f);  // drift direction
        public FloatRange MaxAccumulatedRecoil  = new(12f,  18f);
        public FloatRange MaxAccumulatedHorizontalRecoil = new(6f, 9f);

        [Header("Control — Buildup")]
        public FloatRange RecoilHeatPerShot          = new(0.06f, 0.10f);
        public FloatRange RecoilHeatCooldown         = new(1.2f, 2.0f);
        public FloatRange MaxHeatRecoilMultiplier    = new(1.4f, 1.8f);
        public FloatRange RecoilHeatJitterMultiplier = new(1.5f, 2.0f);
        public FloatRange HotAdsSpreadMultiplier     = new(0f, 0f);

        [Header("Control — Recovery")]
        public FloatRange RecoilRecoverySpeed         = new(4f, 9f);
        public FloatRange RecoilRecoveryFraction      = new(0.55f, 0.80f);
        public FloatRange RecoilRecoveryDelay         = new(0.08f, 0.18f);
        public FloatRange AdsRecoilRecoveryFraction   = new(0.85f, 1f);
        public FloatRange AdsRecoilMultiplier         = new(0.35f, 0.55f);
        public FloatRange HipRecoilVerticalMultiplier = new(0.10f, 0.25f);
        public FloatRange HipRecoilHorizontalMultiplier = new(0.10f, 0.25f);

        [Header("Handling — Sway")]
        public FloatRange LookSwayAmount    = new(0.4f, 0.6f);
        public FloatRange LookSwayRecovery  = new(9f,   12f);
        public FloatRange IdleSwayAmount    = new(0.3f, 0.5f);
        public FloatRange IdleSwaySpeed     = new(0.6f, 0.9f);
        public FloatRange MoveSwayAmount    = new(0.8f, 1.2f);

        [Header("ADS")]
        public FloatRange AdsFovDeg = new(40f, 48f);
        public FloatRange AdsSpeed  = new(8f,  12f);

        // A weapon's stats live as named fields on generated WeaponData rather than in
        // a StatBlock, because the firing code reads them directly. The roll still
        // comes from the shared quality curve, so weapons and armor scale together.
        public override WeaponItem Generate(ItemRoll roll) => WeaponGenerator.Generate(this, roll);

        [ContextMenu("Apply Type Defaults")]
        public void ApplyTypeDefaults() => WeaponCategoryDefaults.Apply(this);
    }
}
