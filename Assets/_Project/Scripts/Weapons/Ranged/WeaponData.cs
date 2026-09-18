using UnityEngine;
using CGD.Audio;
using CGD.Combat;

namespace CGD.Weapons
{
    public enum FireMode { Semi, Auto, Burst }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "CGD/Weapons/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string WeaponName = "Rifle";

        // ── Fire Behavior ─────────────────────────────────────────────────────
        [Header("Fire Behavior")]
        public WeaponFireBehavior FireBehavior;

        // ── Firing ────────────────────────────────────────────────────────────
        [Header("On Hit")]
        [Tooltip("Status effects each hit may apply")]
        public StatusEffectApplication[] OnHitEffects;

        [Header("Firing")]
        public FireMode FireMode = FireMode.Auto;
        [Tooltip("Rounds per minute")]
        public float RoundsPerMinute = 750f;
        [Tooltip("Shots per burst (Burst mode only)")]
        public int BurstCount = 3;
        [Tooltip("Delay between shots within a burst")]
        public float BurstInterval = 0.075f;
        [Tooltip("Pellets fired per shot. Values above 1 produce shotgun-style spread. Damage is applied per pellet.")]
        public int PelletCount = 1;

        // ── Damage ────────────────────────────────────────────────────────────
        [Header("Damage")]
        public float Damage = 25f;
        public DamageType DamageType = DamageType.Physical;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        [Tooltip("Multiplier applied when hitting a critical hitbox region (the head by default)")]
        public float HeadshotMultiplier = 2f;
        [Tooltip("Full damage up to this distance (metres)")]
        public float RangeOptimal = 50f;
        [Tooltip("Damage reaches minimum at this distance")]
        public float RangeFalloffEnd = 150f;
        [Tooltip("Damage multiplier from RangeFalloffEnd out to MaxRange (0–1)")]
        [Range(0.1f, 1f)]
        public float DamageFalloffMin = 0.4f;
        [Tooltip("How far shots travel (metres). Past RangeFalloffEnd they still hit, at DamageFalloffMin damage. Never shorter than RangeFalloffEnd.")]
        [Min(0f)]
        public float MaxRange = 1000f;
        public float EffectiveMaxRange => Mathf.Max(MaxRange, RangeFalloffEnd);
        public LayerMask HitMask = ~0;
        [Tooltip("How far away enemies hear it (0 = silent)")]
        public float NoiseRadius = 40f;

        // ── Ammo & Reload ────────────────────────────────────────────────────
        [Header("Ammo & Reload")]
        public int MagazineSize = 30;
        public int ReserveAmmo  = 90;
        [Tooltip("Reload from empty")]
        public float ReloadTime = 2.6f;
        [Tooltip("Reload with a round still chambered (faster)")]
        public float TacticalReloadTime = 2.1f;

        public int GetNormalizedReserveAmmo()
        {
            return NormalizeReserveAmmo(MagazineSize, ReserveAmmo);
        }

        public static int NormalizeReserveAmmo(int magazineSize, int reserveAmmo)
        {
            if (magazineSize <= 0 || reserveAmmo <= 0) return 0;

            int clips = Mathf.Max(1, Mathf.RoundToInt((float)reserveAmmo / magazineSize));
            return clips * magazineSize;
        }

        // ── Accuracy (where bullets land) ─────────────────────────────────────
        [Header("Accuracy")]
        [Tooltip("Cone half-angle while hip-firing (degrees)")]
        public float HipSpreadDeg  = 2.5f;
        [Tooltip("Cone half-angle while ADS (degrees)")]
        public float AdsSpreadDeg  = 0.01f;
        [Tooltip("How much bloom (SpreadPerShot) applies while fully ADS (0 = no bloom, 1 = same as hip). Ignored when AdsSpreadDeg is 0.")]
        [Range(0f, 1f)]
        public float AdsSpreadMultiplier = 0f;
        [Tooltip("ADS bloom multiplier at full recoil heat, so sustained ADS fire scatters more. Never lower than AdsSpreadMultiplier; ignored when AdsSpreadDeg or AdsSpreadMultiplier is 0.")]
        [Range(0f, 1f)]
        public float HotAdsSpreadMultiplier = 0.4f;
        [Tooltip("Spread added per shot (bloom)")]
        public float SpreadPerShot = 0.8f;
        [Tooltip("Spread degrees recovered per second when not shooting")]
        public float SpreadRecovery = 14f;
        [Tooltip("Maximum spread cap (degrees). At the cap the crosshair still pulses by SpreadPerShot with each shot and settles back before the next one.")]
        public float MaxSpread = 6f;

        // Heat (0–1) raises ADS bloom from AdsSpreadMultiplier toward HotAdsSpreadMultiplier.
        // AdsSpreadMultiplier = 0 is an explicit "no ADS bloom" switch that heat can't override.
        public float GetAdsSpreadMultiplier(float heat)
        {
            if (AdsSpreadDeg <= 0f || AdsSpreadMultiplier <= 0f) return 0f;
            return Mathf.Lerp(AdsSpreadMultiplier, Mathf.Max(AdsSpreadMultiplier, HotAdsSpreadMultiplier), heat);
        }

        // ── Control — Kick ───────────────────────────────────────────────
        [Header("Control — Kick")]
        [Tooltip("Per-shot recoil magnitude (degrees): x = horizontal, y = vertical")]
        public Vector2 RecoilScale = new Vector2(0.55f, 1.2f);
        [Tooltip("Randomness applied per shot, as a fraction of RecoilScale: x = horizontal, y = vertical")]
        public Vector2 RecoilJitter = new Vector2(0.35f, 0.1667f);
        [Tooltip("Authored horizontal lean applied to every shot (-1 full left, 0 none, 1 full right)")]
        [Range(-1f, 1f)]
        public float RecoilHorizontalBias = 0.15f;
        [Tooltip("Cap on total accumulated upward recoil (degrees). At the cap each shot still kicks, then the aim settles back to the cap before the next shot.")]
        public float MaxAccumulatedRecoil = 14f;
        [Tooltip("Cap on total accumulated sideways recoil (degrees), either direction. Drift that reaches it swings back the other way.")]
        public float MaxAccumulatedHorizontalRecoil = 7f;

        // ── Control — Buildup (heat over sustained fire) ─────────────────
        [Header("Control — Buildup")]
        [Tooltip("Heat added per shot, as a fraction of full heat (0.1 = full after 10 shots, 0 = no build-up)")]
        [Range(0f, 1f)]
        public float RecoilHeatPerShot = 0.08f;
        [Tooltip("Heat lost per second once the weapon stops firing (1 = full heat clears in 1 second)")]
        [Min(0f)]
        public float RecoilHeatCooldown = 1.5f;
        [Tooltip("Kick multiplier at full heat; kick scales linearly from 1× at no heat (1 = no build-up)")]
        [Min(1f)]
        public float MaxHeatRecoilMultiplier = 1.6f;
        [Tooltip("Jitter multiplier at full heat (higher = late shots less predictable; 1 = no change)")]
        [Range(1f, 5f)]
        public float RecoilHeatJitterMultiplier = 1.75f;

        // ── Control — Recovery ───────────────────────────────────────────
        [Header("Control — Recovery")]
        [Tooltip("Speed at which accumulated recoil recovers toward zero (higher = snappier)")]
        public float RecoilRecoverySpeed = 6f;
        [Tooltip("Fraction of recoil that is returned to the aim direction (0 = BF-style stays, 1 = CoD-style full return)")]
        [Range(0f, 1f)]
        public float RecoilRecoveryFraction = 0.72f;
        [Tooltip("Seconds after the last shot before aim starts recovering. Should be slightly longer than the fire interval so recovery never fights the kick.")]
        public float RecoilRecoveryDelay = 0.12f;
        [Tooltip("Fraction of recoil returned to aim direction while ADS (1 = full return to origin)")]
        [Range(0f, 1f)]
        public float AdsRecoilRecoveryFraction = 1f;
        [Tooltip("Multiplier applied to all recoil amounts while ADS")]
        [Range(0f, 1f)]
        public float AdsRecoilMultiplier = 0.45f;
        [Tooltip("Camera vertical recoil kick while hip-firing (0 = spread only, 1 = full kick)")]
        [Range(0f, 1f)]
        public float HipRecoilVerticalMultiplier = 0.15f;
        [Tooltip("Camera horizontal recoil kick while hip-firing (0 = spread only, 1 = full kick)")]
        [Range(0f, 1f)]
        public float HipRecoilHorizontalMultiplier = 0.15f;

        // ── Handling (how the weapon moves in hand) ───────────────────────────
        [Header("Handling")]
        [Tooltip("Seconds to draw and ready the weapon after swapping to this slot")]
        public float DrawTime = 0.5f;
        [Tooltip("How heavily the weapon lags behind look input (0 = rigid, 1 = heavy). Higher = worse handling.")]
        [Range(0f, 1f)]
        public float LookSwayAmount = 0.5f;
        [Tooltip("Spring stiffness that settles the weapon back after a look input (higher = snappier)")]
        public float LookSwayRecovery = 10f;
        [Tooltip("Passive breathing sway amplitude while idle (degrees)")]
        public float IdleSwayAmount = 0.4f;
        [Tooltip("Idle sway pattern frequency (Hz)")]
        public float IdleSwaySpeed = 0.7f;
        [Tooltip("Sway amplitude while walking/sprinting, scaled by move speed. All sway fades out while aiming so the sights stay on the crosshair.")]
        public float MoveSwayAmount = 1.0f;

        // ── Audio ─────────────────────────────────────────────────────────────
        [Header("Audio")]
        public SoundBank FireSound;
        public SoundBank ReloadSound;
        [Tooltip("Played when the player tries to fire with an empty magazine")]
        public SoundBank EmptySound;
    }
}
