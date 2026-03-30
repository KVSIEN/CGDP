using UnityEngine;

public enum FireMode { Semi, Auto, Burst }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "CGD/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string WeaponName = "Rifle";

    // ── Firing ────────────────────────────────────────────────────────────
    [Header("Firing")]
    public FireMode FireMode = FireMode.Auto;
    [Tooltip("Rounds per minute")]
    public float RoundsPerMinute = 750f;
    [Tooltip("Shots per burst (Burst mode only)")]
    public int BurstCount = 3;
    [Tooltip("Delay between shots within a burst")]
    public float BurstInterval = 0.075f;

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

    // ── Ammo & Reload ────────────────────────────────────────────────────
    [Header("Ammo & Reload")]
    public int MagazineSize = 30;
    public int ReserveAmmo  = 90;
    [Tooltip("Reload from empty")]
    public float ReloadTime = 2.6f;
    [Tooltip("Reload with a round still chambered (faster)")]
    public float TacticalReloadTime = 2.1f;

    // ── Spread / Bloom ────────────────────────────────────────────────────
    [Header("Spread")]
    [Tooltip("Cone half-angle while hip-firing (degrees)")]
    public float HipSpreadDeg  = 2.5f;
    [Tooltip("Cone half-angle while ADS (degrees)")]
    public float AdsSpreadDeg  = 0.35f;
    [Tooltip("Spread added per shot (bloom)")]
    public float SpreadPerShot = 0.8f;
    [Tooltip("Spread degrees recovered per second when not shooting")]
    public float SpreadRecovery = 14f;
    [Tooltip("Maximum spread cap (degrees)")]
    public float MaxSpread = 6f;

    // ── Recoil ────────────────────────────────────────────────────────────
    [Header("Recoil — Kick")]
    [Tooltip("Upward pitch added per shot (degrees)")]
    public float RecoilVertical = 1.2f;
    [Tooltip("Random ± variation on vertical kick")]
    public float RecoilVerticalVariation = 0.2f;
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
    [Tooltip("Multiplier applied to all recoil amounts while ADS")]
    [Range(0f, 1f)]
    public float AdsRecoilMultiplier = 0.45f;
    [Tooltip("Multiplier applied to camera recoil kick when hip-firing. 0 = no camera movement (spread only), 1 = full kick. CoD-style: 0.1–0.2, BF-style: 0.4–0.6.")]
    [Range(0f, 1f)]
    public float HipRecoilMultiplier = 0.15f;
    [Tooltip("Hard cap on total accumulated upward recoil (degrees)")]
    public float MaxAccumulatedRecoil = 14f;
}
