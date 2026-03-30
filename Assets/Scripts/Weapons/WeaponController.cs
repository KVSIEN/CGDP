using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Hitscan gun controller. Attach to the player root (or a weapon child).
/// Wire up references in the Inspector, then assign a WeaponData asset.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponData         _data;
    [SerializeField] private PlayerInputHandler _input;
    [SerializeField] private PlayerCamera       _camera;
    [SerializeField] private CrosshairHUD       _crosshair;
    [SerializeField] private Transform          _muzzle;    // optional: origin for visual FX

    [Header("Debug")]
    [SerializeField] private bool  _debugDrawBullets = true;
    [SerializeField] private float _debugLineDuration = 2f;
    [SerializeField] private Color _debugHitColor  = Color.red;
    [SerializeField] private Color _debugMissColor = Color.yellow;

    // ── Runtime state ─────────────────────────────────────────────────────
    private int   _magazine;
    private int   _reserve;
    private float _fireCooldown;
    private float _currentSpread;
    private float _recoilDriftDir;   // -1..1, wanders over time
    private bool  _isReloading;
    private bool  _burstPending;

    // Tracks how much camera recoil has been applied this burst (for the hard cap).
    // Only counts actual camera kick (vertKick), not hip-fire shots.
    // Resets as soon as firing stops so each new burst starts fresh.
    private float _accumulatedRecoil;
    private bool  _wasFiringLastFrame;

    /// <summary>Fired whenever magazine, reserve, or reload state changes. Args: magazine, reserve, isReloading.</summary>
    public event Action<int, int, bool> OnAmmoChanged;

    public WeaponData Data      => _data;
    public int  Magazine        => _magazine;
    public int  Reserve         => _reserve;
    public bool IsReloading     => _isReloading;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Awake()
    {
        if (_data == null) return;
        _magazine = _data.MagazineSize;
        _reserve  = _data.ReserveAmmo;
        NotifyAmmoChanged();
    }

    private void OnDisable()
    {
        _crosshair?.SetDynamicSpread(0f);
    }

    private void Update()
    {
        if (_data == null || _isReloading) return;

        TickSpread();
        TickRecoilRecovery();
        TickFireCooldown();
        HandleFireInput();
        HandleReloadInput();
        UpdateCrosshair();
    }

    // ── Public API ────────────────────────────────────────────────────────
    /// <summary>Swap the active weapon asset at runtime.</summary>
    public void Equip(WeaponData data)
    {
        StopAllCoroutines();
        _isReloading       = false;
        _data              = data;
        _magazine          = data.MagazineSize;
        _reserve           = data.ReserveAmmo;
        _currentSpread     = 0f;
        _accumulatedRecoil = 0f;
        NotifyAmmoChanged();
    }

    /// <summary>Restore ammo to full — call on respawn.</summary>
    public void Refill()
    {
        if (_data == null) return;
        _magazine = _data.MagazineSize;
        _reserve  = _data.ReserveAmmo;
        NotifyAmmoChanged();
    }

    // ── Input polling ─────────────────────────────────────────────────────
    private void HandleFireInput()
    {
        if (_fireCooldown > 0f) return;

        bool triggerHeld  = _input.GetAction(GameAction.Attack);
        bool triggerPress = _input.WasPressed(GameAction.Attack);

        switch (_data.FireMode)
        {
            case FireMode.Auto  when triggerHeld:  TryFire(); break;
            case FireMode.Semi  when triggerPress: TryFire(); break;
            case FireMode.Burst when triggerPress && !_burstPending:
                StartCoroutine(FireBurst()); break;
        }
    }

    private void HandleReloadInput()
    {
        if (_input.WasPressed(GameAction.Reload) && _magazine < _data.MagazineSize && _reserve > 0)
            StartCoroutine(Reload());
    }

    // ── Fire ──────────────────────────────────────────────────────────────
    private void TryFire()
    {
        if (_magazine <= 0)
        {
            if (_reserve > 0) StartCoroutine(Reload());
            return;
        }

        _magazine--;
        _fireCooldown = 60f / _data.RoundsPerMinute;
        NotifyAmmoChanged();

        ApplyRecoil();
        CastBullet();
        AddSpreadBloom();
    }

    private IEnumerator FireBurst()
    {
        _burstPending = true;
        for (int i = 0; i < _data.BurstCount; i++)
        {
            if (_magazine <= 0) break;
            TryFire();
            if (i < _data.BurstCount - 1)
                yield return new WaitForSeconds(_data.BurstInterval);
        }
        _burstPending = false;
    }

    // ── Hitscan ───────────────────────────────────────────────────────────
    private void CastBullet()
    {
        float spreadDeg = Mathf.Lerp(_data.HipSpreadDeg, _data.AdsSpreadDeg, _camera.AdsT)
                        + _currentSpread;
        Vector3 dir    = SpreadDirection(_camera.transform.forward, spreadDeg);
        Vector3 origin = _muzzle != null ? _muzzle.position : _camera.transform.position;

        bool didHit = Physics.Raycast(origin, dir, out RaycastHit hit, _data.RangeFalloffEnd, _data.HitMask, QueryTriggerInteraction.Ignore);

        if (_debugDrawBullets)
        {
            Vector3 end = didHit ? hit.point : origin + dir * _data.RangeFalloffEnd;
            Debug.DrawLine(origin, end, didHit ? _debugHitColor : _debugMissColor, _debugLineDuration);
        }

        if (!didHit) return;

        float damage = CalculateDamage(hit.distance, false);

        if (hit.collider.TryGetComponent<PlayerStats>(out var targetStats))
            targetStats.TakeDamage(damage);
    }

    private float CalculateDamage(float distance, bool headshot)
    {
        float t = Mathf.InverseLerp(_data.RangeOptimal, _data.RangeFalloffEnd, distance);
        float falloff = Mathf.Lerp(1f, _data.DamageFalloffMin, t);
        float dmg = _data.Damage * falloff;
        if (headshot) dmg *= _data.HeadshotMultiplier;
        return dmg;
    }

    // ── Spread ────────────────────────────────────────────────────────────
    private void AddSpreadBloom()
    {
        _currentSpread = Mathf.Min(_currentSpread + _data.SpreadPerShot, _data.MaxSpread);
    }

    private void TickSpread()
    {
        // Only recover spread when not actively firing so bloom builds up correctly
        if (_currentSpread > 0f && _fireCooldown <= 0f)
            _currentSpread = Mathf.Max(_currentSpread - _data.SpreadRecovery * Time.deltaTime, 0f);
    }

    private static Vector3 SpreadDirection(Vector3 forward, float spreadDeg)
    {
        if (spreadDeg <= 0f) return forward;
        float radius = Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
        Vector2 offset = Random.insideUnitCircle * radius;
        Quaternion rot = Quaternion.LookRotation(forward);
        return (rot * new Vector3(offset.x, offset.y, 1f)).normalized;
    }

    // ── Recoil ────────────────────────────────────────────────────────────
    private void ApplyRecoil()
    {
        float adsT   = _camera.AdsT;
        // Hip = 0 camera kick; ADS = full kick scaled by AdsRecoilMultiplier
        float camMult = Mathf.Lerp(0f, _data.AdsRecoilMultiplier, adsT);

        float vertBase = _data.RecoilVertical + Random.Range(-_data.RecoilVerticalVariation, _data.RecoilVerticalVariation);
        float remaining = _data.MaxAccumulatedRecoil - _accumulatedRecoil;
        float vertKick  = Mathf.Min(vertBase * camMult, remaining);
        // Only accumulate what was actually applied to the camera
        _accumulatedRecoil += vertKick;

        WanderDrift();
        float horizKick = (Random.Range(-_data.RecoilHorizontalMax, _data.RecoilHorizontalMax)
                         + _recoilDriftDir * _data.RecoilHorizontalMax * _data.RecoilHorizontalBias) * camMult;

        _camera.AddRecoil(vertKick, horizKick, _data.RecoilRecoverySpeed, _data.RecoilRecoveryFraction, _data.RecoilRecoveryDelay);
    }

    private void WanderDrift()
    {
        _recoilDriftDir += Random.Range(-0.4f, 0.4f);
        _recoilDriftDir  = Mathf.Clamp(_recoilDriftDir, -1f, 1f);
    }

    private void TickRecoilRecovery()
    {
        bool isFiring = _fireCooldown > 0f;
        if (!isFiring && _wasFiringLastFrame)
        {
            // Gun just went idle — reset cap instantly so next burst starts fresh
            _accumulatedRecoil = 0f;
            _recoilDriftDir    = 0f;
        }
        _wasFiringLastFrame = isFiring;
    }

    // ── Reload ────────────────────────────────────────────────────────────
    private IEnumerator Reload()
    {
        _isReloading = true;
        NotifyAmmoChanged();

        float time = _magazine > 0 ? _data.TacticalReloadTime : _data.ReloadTime;
        yield return new WaitForSeconds(time);

        int needed = _data.MagazineSize - _magazine;
        int taken  = Mathf.Min(needed, _reserve);
        _magazine += taken;
        _reserve  -= taken;

        _isReloading = false;
        NotifyAmmoChanged();
    }

    // ── Crosshair ─────────────────────────────────────────────────────────
    private void UpdateCrosshair()
    {
        if (_crosshair == null || _data == null) return;
        float baseDeg   = Mathf.Lerp(_data.HipSpreadDeg, _data.AdsSpreadDeg, _camera.AdsT);
        _crosshair.SetDynamicSpread(baseDeg + _currentSpread);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void TickFireCooldown()
    {
        if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;
    }

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(_magazine, _reserve, _isReloading);
    }
}
