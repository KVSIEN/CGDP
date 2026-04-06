using UnityEngine;

public enum FireMode { Semi, Auto, Burst }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "CGD/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string WeaponName = "Rifle";

    // ── Fire Behavior ─────────────────────────────────────────────────────
    [Header("Fire Behavior")]
    public WeaponFireBehavior FireBehavior;

    // ── Firing ────────────────────────────────────────────────────────────
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
    [Tooltip("Multiplier applied when hitting a collider tagged 'Head'")]
    public float HeadshotMultiplier = 2f;
    [Tooltip("Full damage up to this distance (metres)")]
    public float RangeOptimal = 50f;
    [Tooltip("Damage reaches minimum at this distance")]
    public float RangeFalloffEnd = 150f;
    [Tooltip("Damage multiplier at maximum range (0–1)")]
    [Range(0.1f, 1f)]
    public float DamageFalloffMin = 0.4f;
    public LayerMask HitMask = ~0;

    // ── Handling ──────────────────────────────────────────────────────────
    [Header("Handling")]
    [Tooltip("Seconds to draw and ready the weapon after swapping to this slot")]
    public float DrawTime = 0.5f;

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

    // ── Spread / Bloom ────────────────────────────────────────────────────
    [Header("Spread")]
    [Tooltip("Cone half-angle while hip-firing (degrees)")]
    public float HipSpreadDeg  = 2.5f;
    [Tooltip("Cone half-angle while ADS (degrees)")]
    public float AdsSpreadDeg  = 0.35f;
    [Tooltip("How much bloom (SpreadPerShot) applies while fully ADS (0 = no bloom, 1 = same as hip). Ignored when AdsSpreadDeg is 0.")]
    [Range(0f, 1f)]
    public float AdsSpreadMultiplier = 0f;
    public float EffectiveAdsSpreadMultiplier => AdsSpreadDeg > 0f ? AdsSpreadMultiplier : 0f;
    [Tooltip("Spread added per shot (bloom)")]
    public float SpreadPerShot = 0.8f;
    [Tooltip("Spread degrees recovered per second when not shooting")]
    public float SpreadRecovery = 14f;
    [Tooltip("Maximum spread cap (degrees)")]
    public float MaxSpread = 6f;

    // ── Recoil ────────────────────────────────────────────────────────────
    [Header("Recoil — Kick")]
    [Tooltip("Upward pitch added per shot (degrees)")]
    public float RecoilVerticalMax = 1.2f;
    [Tooltip("Random ± variation on vertical kick")]
    public float RecoilVerticalBias = 0.2f;
    [Tooltip("Maximum horizontal kick per shot (degrees); actual value is random each shot")]
    public float RecoilHorizontalMax = 0.55f;
    [Tooltip("Persistent sideways drift bias (-1 full left, 0 none, 1 full right)")]
    [Range(-1f, 1f)]
    public float RecoilHorizontalBias = 0.15f;

    [Header("Recoil — Recovery")]
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
    [Tooltip("Hard cap on total accumulated upward recoil (degrees)")]
    public float MaxAccumulatedRecoil = 14f;
}
