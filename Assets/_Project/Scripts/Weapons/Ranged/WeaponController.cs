using System;
using System.Collections;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Player;
using CGD.UI;

namespace CGD.Weapons
{
    /// <summary>
    /// Fires whichever WeaponInstance is equipped (PlayerWeaponLoadout owns the carried
    /// weapons). Attach to the player root and wire up references in the Inspector.
    /// Fire behaviour (hitscan, shotgun, projectile) comes from the equipped weapon's data.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
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
        private WeaponInstance _current;
        private PlayerMovement _movement;
        private DamageSource   _damageSource;
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

        // Heat grows with each shot and decays when the trigger is released.
        // Both kick magnitude and jitter scale with heat, so early shots stay
        // controllable and sustained fire drifts progressively wilder.
        private float _recoilHeat;

        /// <summary>Fired whenever magazine, reserve, or reload state changes. Args: magazine, reserve, isReloading.</summary>
        public event Action<int, int, bool> OnAmmoChanged;

        public WeaponInstance Current => _current;
        public WeaponData Data        => _current?.Data;
        public int  Magazine          => _current?.Magazine ?? 0;
        public int  Reserve           => _current?.Reserve ?? 0;
        public bool IsReloading       => _isReloading;

        private WeaponData D        => _current.Data;
        private Vector3    SoundPos => _muzzle != null ? _muzzle.position : transform.position;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _movement     = GetComponent<PlayerMovement>();
            _damageSource = DamageSource.Of(gameObject);
        }

        private void OnDisable()
        {
            if (_crosshair != null)
                _crosshair.SetDynamicSpread(0f);
        }

        private void Update()
        {
            if (_current == null) return;

            // Sway keeps ticking through draw and reload so the weapon never freezes
            // mid-animation. Only the fire/reload/spread logic gates on those states.
            PushSwayInputs();

            if (_drawTimer > 0f)
            {
                _drawTimer -= Time.deltaTime;
                return;
            }

            if (_isReloading) return;

            TickSpread();
            TickRecoilRecovery();
            TickFireCooldown();

            if (_movement == null || _movement.CanAct)
            {
                HandleFireInput();
                HandleReloadInput();
            }

            UpdateCrosshair();
        }

        private void PushSwayInputs()
        {
            if (_visuals == null || _input == null) return;

            Vector3 vel = _movement != null ? _movement.Velocity : Vector3.zero;
            float horizSpeed = new Vector2(vel.x, vel.z).magnitude;
            bool  grounded   = _movement == null || _movement.IsGrounded;
            _visuals.SetSwayInputs(_input.LookInput, horizSpeed, grounded, _camera != null ? _camera.AdsT : 0f);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Swap the active weapon at runtime (null = unarmed).</summary>
        public void Equip(WeaponInstance weapon)
        {
            StopAllCoroutines();
            _isReloading       = false;
            _burstPending      = false;
            _current           = weapon;
            _currentSpread     = 0f;
            _accumulatedRecoil = 0f;
            _accumulatedHorizontalRecoil = 0f;
            _recoilHeat        = 0f;
            _drawTimer         = weapon != null ? weapon.Data.DrawTime : 0f;

            if (_visuals != null)   _visuals.Configure(weapon?.Data);
            if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
            NotifyAmmoChanged();
        }

        /// <summary>Adds reserve ammo to the equipped weapon. Returns false when unarmed.</summary>
        public bool AddReserveAmmo(int amount)
        {
            if (_current == null || amount <= 0) return false;
            _current.Reserve += amount;
            NotifyAmmoChanged();
            return true;
        }

        // ── Input polling ─────────────────────────────────────────────────────

        private void HandleFireInput()
        {
            if (!_fireCooldown.IsReady) return;

            bool triggerHeld  = _input.GetAction(GameAction.Attack);
            bool triggerPress = _input.WasPressed(GameAction.Attack);

            switch (D.FireMode)
            {
                case FireMode.Auto  when triggerHeld:  TryFire(); break;
                case FireMode.Semi  when triggerPress: TryFire(); break;
                case FireMode.Burst when triggerPress && !_burstPending:
                    StartCoroutine(FireBurst()); break;
            }

            if (triggerPress && _current.Magazine <= 0)
                D.EmptySound?.Play(SoundPos);
        }

        private void HandleReloadInput()
        {
            if (_input.GetAction(GameAction.Reload) && _current.Magazine < D.MagazineSize && _current.Reserve > 0)
                StartCoroutine(Reload());
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        private void TryFire()
        {
            if (_current.Magazine <= 0) return;

            _current.Magazine--;
            _fireCooldown.Start(60f / D.RoundsPerMinute);
            NotifyAmmoChanged();

            ApplyRecoil();
            CastBullet();
            AddSpreadBloom();

            D.FireSound?.Play(SoundPos);
            Noise.Emit(transform.position, D.NoiseRadius, _damageSource);
        }

        private IEnumerator FireBurst()
        {
            _burstPending = true;
            for (int i = 0; i < D.BurstCount; i++)
            {
                if (_current.Magazine <= 0) break;
                TryFire();
                if (i < D.BurstCount - 1)
                    yield return new WaitForSeconds(D.BurstInterval);
            }
            _burstPending = false;
        }

        private void CastBullet()
        {
            if (D.FireBehavior == null) return;

            float   adsT      = _camera.AdsT;
            float   spreadDeg = Mathf.Lerp(D.HipSpreadDeg, D.AdsSpreadDeg, adsT)
                              + _currentSpread * Mathf.Lerp(1f, D.EffectiveAdsSpreadMultiplier, adsT);
            Vector3 forward   = _camera.transform.forward;

            // Ray originates from camera centre — avoids TP parallax where muzzle→target
            // diverges from camera forward for close geometry, causing shots to miss.
            D.FireBehavior.Execute(new FireContext
            {
                CameraPosition    = _camera.transform.position,
                CameraForward     = forward,
                SpreadDeg         = spreadDeg,
                Direction         = WeaponFireBehavior.ComputeSpreadDirection(forward, spreadDeg),
                Muzzle            = _muzzle,
                Data              = D,
                Source            = _damageSource,
                DebugDraw         = _debugDrawBullets,
                DebugHitColor     = _debugHitColor,
                DebugMissColor    = _debugMissColor,
                DebugLineDuration = _debugLineDuration,
            });
        }

        // ── Spread ────────────────────────────────────────────────────────────

        private void AddSpreadBloom()
        {
            _currentSpread = Mathf.Min(_currentSpread + D.SpreadPerShot, D.MaxSpread);
        }

        private void TickSpread()
        {
            // Only recover spread when not actively firing so bloom builds up correctly
            if (_currentSpread > 0f && _fireCooldown.IsReady)
                _currentSpread = Mathf.Max(_currentSpread - D.SpreadRecovery * Time.deltaTime, 0f);
        }

        // ── Recoil ────────────────────────────────────────────────────────────

        private void ApplyRecoil()
        {
            float adsT      = _camera.AdsT;
            float vertMult  = Mathf.Lerp(D.HipRecoilVerticalMultiplier,   D.AdsRecoilMultiplier, adsT);
            float horizMult = Mathf.Lerp(D.HipRecoilHorizontalMultiplier, D.AdsRecoilMultiplier, adsT);

            // Heat curves multiply both the base kick and the jitter, so early shots
            // stay tight and predictable while sustained fire drifts wilder.
            float kickHeat   = Mathf.Lerp(1f, D.RecoilHeatKickMultiplier,   _recoilHeat);
            float jitterHeat = Mathf.Lerp(1f, D.RecoilHeatJitterMultiplier, _recoilHeat);

            // Shared shape for both axes: axisScale × (pattern + jitter).
            // Vertical's pattern is a constant full kick; horizontal's pattern is the
            // authored drift bias applied directly, so it reads from the first shot
            // instead of emerging over several rounds.
            float vertJitter = BlendedJitter(D.RecoilJitter.y) * jitterHeat;
            float vertBase   = D.RecoilScale.y * kickHeat * (1f + vertJitter);
            float remaining  = D.MaxAccumulatedRecoil - _accumulatedRecoil;
            float vertKick   = Mathf.Min(vertBase * vertMult, remaining);
            _accumulatedRecoil += vertKick;

            float horizJitter    = BlendedJitter(D.RecoilJitter.x) * jitterHeat;
            float horizRaw       = D.RecoilScale.x * kickHeat * (D.RecoilHorizontalBias + horizJitter) * horizMult;
            float horizRemaining = D.MaxAccumulatedHorizontalRecoil - Mathf.Abs(_accumulatedHorizontalRecoil);
            float horizKick      = Mathf.Clamp(horizRaw, -horizRemaining, horizRemaining);
            _accumulatedHorizontalRecoil += horizKick;

            _recoilHeat = Mathf.Min(1f, _recoilHeat + D.RecoilHeatPerShot);

            float recoveryFraction = Mathf.Lerp(D.RecoilRecoveryFraction, D.AdsRecoilRecoveryFraction, adsT);
            _camera.AddRecoil(vertKick, horizKick, D.RecoilRecoverySpeed, recoveryFraction, D.RecoilRecoveryDelay);
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
                // Gun just went idle — reset the hard cap instantly so the next burst
                // starts fresh. Heat decays gradually (below) so quick tap-fire still
                // carries a bit of built-up sway.
                _accumulatedRecoil = 0f;
                _accumulatedHorizontalRecoil = 0f;
            }

            if (!isFiring && _recoilHeat > 0f)
                _recoilHeat = Mathf.Max(0f, _recoilHeat - D.RecoilHeatDecay * Time.deltaTime);

            _wasFiringLastFrame = isFiring;
        }

        // ── Reload ────────────────────────────────────────────────────────────

        private IEnumerator Reload()
        {
            _isReloading = true;
            NotifyAmmoChanged();

            D.ReloadSound?.Play(SoundPos);

            float time = _current.Magazine > 0 ? D.TacticalReloadTime : D.ReloadTime;
            yield return new WaitForSeconds(time);

            int needed = D.MagazineSize - _current.Magazine;
            int taken  = Mathf.Min(needed, _current.Reserve);
            _current.Magazine += taken;
            _current.Reserve  -= taken;

            _isReloading = false;
            NotifyAmmoChanged();
        }

        // ── Crosshair ─────────────────────────────────────────────────────────

        private void UpdateCrosshair()
        {
            if (_crosshair == null) return;

            float adsT    = _camera.AdsT;
            float baseDeg = Mathf.Lerp(D.HipSpreadDeg, D.AdsSpreadDeg, adsT);
            _crosshair.SetDynamicSpread(baseDeg + _currentSpread * Mathf.Lerp(1f, D.EffectiveAdsSpreadMultiplier, adsT));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void TickFireCooldown()
        {
            _fireCooldown.Tick(Time.deltaTime);
        }

        private void NotifyAmmoChanged()
        {
            OnAmmoChanged?.Invoke(Magazine, Reserve, _isReloading);
        }
    }
}
