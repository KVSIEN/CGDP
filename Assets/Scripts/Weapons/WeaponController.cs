using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Weapon controller. Attach to the player root (or a weapon child).
/// Wire up references in the Inspector, then assign a WeaponData asset.
/// Fire behaviour (hitscan or projectile) is determined by the WeaponFireBehavior
/// assigned on the WeaponData asset.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponData         _data;
    [SerializeField] private PlayerInputHandler _input;
    [SerializeField] private PlayerCamera       _camera;
    [SerializeField] private CrosshairHUD       _crosshair;
    [SerializeField] private Transform          _muzzle;    // optional: origin for visual FX
    [SerializeField] private WeaponVisuals      _visuals;   // optional: weapon model kick

    [Header("Debug")]
    [SerializeField] private bool  _debugDrawBullets = true;
    [SerializeField] private float _debugLineDuration = 2f;
    [SerializeField] private Color _debugHitColor  = Color.red;
    [SerializeField] private Color _debugMissColor = Color.yellow;

    // ── Runtime state ─────────────────────────────────────────────────────
    private int   _magazine;
    private int   _reserve;
    private CooldownTimer _fireCooldown;
    private float _drawTimer;
    private float _currentSpread;
    private bool  _isReloading;
    private bool  _burstPending;

    // Tracks how much camera recoil has been applied this burst (for the hard cap).
    // Only counts actual camera kick (vertKick/horizKick), not hip-fire shots.
    // Resets as soon as firing stops so each new burst starts fresh.
    private float _accumulatedRecoil;
    private float _accumulatedHorizontalRecoil;
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
        _reserve  = _data.GetNormalizedReserveAmmo();
        NotifyAmmoChanged();
    }

    private void OnDisable()
    {
        if (_crosshair != null)
            _crosshair.SetDynamicSpread(0f);
    }

    private void Update()
    {
        if (_data == null) return;

        if (_drawTimer > 0f)
        {
            _drawTimer -= Time.deltaTime;
            return;
        }

        if (_isReloading) return;

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
        _magazine          = data != null ? data.MagazineSize : 0;
        _reserve           = data != null ? data.GetNormalizedReserveAmmo() : 0;
        _currentSpread     = 0f;
        _accumulatedRecoil = 0f;
        _accumulatedHorizontalRecoil = 0f;
        _drawTimer         = data != null ? data.DrawTime : 0f;
        if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
        NotifyAmmoChanged();
    }

    /// <summary>Restore ammo to full — call on respawn.</summary>
    public void Refill()
    {
        if (_data == null) return;
        _magazine = _data.MagazineSize;
        _reserve  = _data.GetNormalizedReserveAmmo();
        NotifyAmmoChanged();
    }

    /// <summary>Adds reserve ammo to the currently equipped weapon — call from ammo pickups.</summary>
    public void AddReserveAmmo(int amount)
    {
        if (_data == null || amount <= 0) return;
        _reserve += amount;
        NotifyAmmoChanged();
    }

    // ── Input polling ─────────────────────────────────────────────────────
    private void HandleFireInput()
    {
        if (!_fireCooldown.IsReady) return;

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
        if (_input.GetAction(GameAction.Reload) && _magazine < _data.MagazineSize && _reserve > 0)
            StartCoroutine(Reload());
    }

    // ── Fire ──────────────────────────────────────────────────────────────
    private void TryFire()
    {
        if (_magazine <= 0) return;

        _magazine--;
        _fireCooldown.Start(60f / _data.RoundsPerMinute);
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

    // ── Fire ──────────────────────────────────────────────────────────────
    private void CastBullet()
    {
        if (_data.FireBehavior == null) return;

        float   adsT      = _camera.AdsT;
        float   spreadDeg = Mathf.Lerp(_data.HipSpreadDeg, _data.AdsSpreadDeg, adsT)
                          + _currentSpread * Mathf.Lerp(1f, _data.EffectiveAdsSpreadMultiplier, adsT);
        Vector3 forward   = _camera.transform.forward;

        // Ray originates from camera centre — avoids TP parallax where muzzle→target
        // diverges from camera forward for close geometry, causing shots to miss.
        _data.FireBehavior.Execute(new FireContext
        {
            CameraPosition    = _camera.transform.position,
            CameraForward     = forward,
            SpreadDeg         = spreadDeg,
            Direction         = WeaponFireBehavior.ComputeSpreadDirection(forward, spreadDeg),
            Muzzle            = _muzzle,
            Data              = _data,
            DebugDraw         = _debugDrawBullets,
            DebugHitColor     = _debugHitColor,
            DebugMissColor    = _debugMissColor,
            DebugLineDuration = _debugLineDuration,
        });
    }

    // ── Spread ────────────────────────────────────────────────────────────
    private void AddSpreadBloom()
    {
        _currentSpread = Mathf.Min(_currentSpread + _data.SpreadPerShot, _data.MaxSpread);
    }

    private void TickSpread()
    {
        // Only recover spread when not actively firing so bloom builds up correctly
        if (_currentSpread > 0f && _fireCooldown.IsReady)
            _currentSpread = Mathf.Max(_currentSpread - _data.SpreadRecovery * Time.deltaTime, 0f);
    }

    // ── Recoil ────────────────────────────────────────────────────────────
    private void ApplyRecoil()
    {
        float adsT      = _camera.AdsT;
        float vertMult  = Mathf.Lerp(_data.HipRecoilVerticalMultiplier,   _data.AdsRecoilMultiplier, adsT);
        float horizMult = Mathf.Lerp(_data.HipRecoilHorizontalMultiplier, _data.AdsRecoilMultiplier, adsT);

        // Shared shape for both axes: axisScale × (pattern + jitter).
        // Vertical's pattern is a constant full kick; horizontal's pattern is the
        // authored drift bias applied directly, so it reads from the first shot
        // instead of emerging over several rounds.
        float vertJitter = BlendedJitter(_data.RecoilJitter.y);
        float vertBase   = _data.RecoilScale.y * (1f + vertJitter);
        float remaining  = _data.MaxAccumulatedRecoil - _accumulatedRecoil;
        float vertKick   = Mathf.Min(vertBase * vertMult, remaining);
        _accumulatedRecoil += vertKick;

        float horizJitter    = BlendedJitter(_data.RecoilJitter.x);
        float horizRaw       = _data.RecoilScale.x * (_data.RecoilHorizontalBias + horizJitter) * horizMult;
        float horizRemaining = _data.MaxAccumulatedHorizontalRecoil - Mathf.Abs(_accumulatedHorizontalRecoil);
        float horizKick      = Mathf.Clamp(horizRaw, -horizRemaining, horizRemaining);
        _accumulatedHorizontalRecoil += horizKick;

        float recoveryFraction = Mathf.Lerp(_data.RecoilRecoveryFraction, _data.AdsRecoilRecoveryFraction, adsT);
        _camera.AddRecoil(vertKick, horizKick, _data.RecoilRecoverySpeed, recoveryFraction, _data.RecoilRecoveryDelay);
        _visuals?.AddKick(vertKick, horizKick);
    }

    // Averaging two uniform samples gives a triangular, center-weighted spread
    // over the same [-magnitude, magnitude] range instead of a flat distribution.
    private static float BlendedJitter(float magnitude)
    {
        float a = UnityEngine.Random.Range(-magnitude, magnitude);
        float b = UnityEngine.Random.Range(-magnitude, magnitude);
        return (a + b) * 0.5f;
    }

    private void TickRecoilRecovery()
    {
        bool isFiring = !_fireCooldown.IsReady;
        if (!isFiring && _wasFiringLastFrame)
        {
            // Gun just went idle — reset cap instantly so next burst starts fresh
            _accumulatedRecoil = 0f;
            _accumulatedHorizontalRecoil = 0f;
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
        float adsT    = _camera.AdsT;
        float baseDeg = Mathf.Lerp(_data.HipSpreadDeg, _data.AdsSpreadDeg, adsT);
        float adsBloomMult = _data.AdsSpreadDeg > 0f ? _data.AdsSpreadMultiplier : 0f;
        _crosshair.SetDynamicSpread(baseDeg + _currentSpread * Mathf.Lerp(1f, adsBloomMult, adsT));
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void TickFireCooldown()
    {
        _fireCooldown.Tick(Time.deltaTime);
    }

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(_magazine, _reserve, _isReloading);
    }
}
