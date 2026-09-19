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
        private float _lastShotTime = float.NegativeInfinity;
        private bool  _isReloading;
        private bool  _burstPending;

        // Tracks how much camera recoil has been applied this burst (for the hard cap).
        // Only counts actual camera kick (vertKick/horizKick), not hip-fire shots.
        // Resets as soon as firing stops so each new burst starts fresh.
        private float _accumulatedRecoil;
        private float _accumulatedHorizontalRecoil;
        private bool  _wasFiringLastFrame;

        // 0–1; grows with sustained fire and scales each shot's kick and jitter, so early
        // shots stay controllable and sustained fire drifts wilder. Unlike the caps above
        // it cools off gradually, so quick follow-up bursts stay hot.
        private float _recoilHeat;
        private float _horizontalDriftSign = 1f;   // flips each time drift hits the horizontal cap

        // Charge fire mode: accumulates while the trigger is held, releases on the frame
        // the trigger goes up. Reset on Equip so a swap can never carry someone else's charge.
        private float _chargeTimer;
        private bool  _wasChargeHeld;

        /// <summary>Fired whenever magazine, reserve, or reload state changes. Args: magazine, reserve, isReloading.</summary>
        public event Action<int, int, bool> OnAmmoChanged;

        public WeaponInstance Current => _current;
        public WeaponData Data        => _current?.Data;
        public int  Magazine          => _current?.Magazine ?? 0;
        public int  Reserve           => _current?.Reserve ?? 0;
        public bool IsReloading       => _isReloading;
        /// <summary>0 at rest, 1 fully charged. Always 0 for non-Charge fire modes.</summary>
        public float ChargeRatio      => Data != null && Data.FireMode == FireMode.Charge && Data.ChargeTime > 0f
                                          ? Mathf.Clamp01(_chargeTimer / Data.ChargeTime) : 0f;

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
            _horizontalDriftSign = 1f;
            _chargeTimer       = 0f;
            _wasChargeHeld     = false;
            _drawTimer         = weapon != null ? weapon.Data.DrawTime : 0f;

            if (_visuals != null)   _visuals.Configure(weapon?.Data);
            if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
            if (_camera != null && weapon != null)
                _camera.SetAdsProfile(weapon.Data.AdsFovDeg, weapon.Data.AdsSpeed);
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

            if (_current.Magazine <= 0)
            {
                // Pulling the trigger on an empty gun, or still holding it as the magazine
                // runs dry, reloads; with no reserve left it just clicks. Charge state is
                // synced to the current trigger position so a still-held trigger after a
                // reload doesn't instantly fire whatever was queued before.
                _chargeTimer   = 0f;
                _wasChargeHeld = triggerHeld;
                if (_burstPending || !(triggerHeld || triggerPress)) return;
                if (CanReload) StartCoroutine(Reload());
                else if (triggerPress) D.EmptySound?.Play(SoundPos);
                return;
            }

            switch (D.FireMode)
            {
                case FireMode.Auto  when triggerHeld:  TryFire(); break;
                case FireMode.Semi  when triggerPress: TryFire(); break;
                case FireMode.Burst when triggerPress && !_burstPending:
                    StartCoroutine(FireBurst()); break;
                case FireMode.Charge:
                    HandleChargeInput(triggerHeld);
                    break;
            }
        }

        // Hold to charge, release to fire. Releasing before MinChargeToFire cancels the
        // shot with no ammo spent so tap-firing a bow doesn't waste arrows.
        private void HandleChargeInput(bool triggerHeld)
        {
            if (triggerHeld)
            {
                _chargeTimer = Mathf.Min(_chargeTimer + Time.deltaTime, D.ChargeTime);
            }
            else if (_wasChargeHeld)
            {
                float charge = D.ChargeTime > 0f ? Mathf.Clamp01(_chargeTimer / D.ChargeTime) : 1f;
                if (charge >= D.MinChargeToFire) TryFire(charge);
                _chargeTimer = 0f;
            }
            _wasChargeHeld = triggerHeld;
        }

        private void HandleReloadInput()
        {
            if (!_isReloading && _input.GetAction(GameAction.Reload) && CanReload)
                StartCoroutine(Reload());
        }

        private bool CanReload => _current.Magazine < D.MagazineSize && _current.Reserve > 0;

        // ── Fire ──────────────────────────────────────────────────────────────

        private void TryFire(float charge = 1f)
        {
            if (_current.Magazine <= 0) return;

            _current.Magazine--;
            _fireCooldown.Start(60f / D.RoundsPerMinute);
            _lastShotTime = Time.time;
            NotifyAmmoChanged();

            ApplyRecoil();
            CastBullet(charge);
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

        private void CastBullet(float charge)
        {
            if (D.FireBehavior == null) return;

            float   adsT      = _camera.AdsT;
            float   spreadDeg = Mathf.Lerp(D.HipSpreadDeg, D.AdsSpreadDeg, adsT)
                              + _currentSpread * BloomScale(adsT);
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
                Charge            = charge,
                DebugDraw         = _debugDrawBullets,
                DebugHitColor     = _debugHitColor,
                DebugMissColor    = _debugMissColor,
                DebugLineDuration = _debugLineDuration,
            });
        }

        // ── Spread ────────────────────────────────────────────────────────────

        // Bloom may overshoot MaxSpread by one shot's worth; TickSpread settles it back before
        // the next shot, so the crosshair still pulses with each shot at max while the spread
        // bullets actually use (read before this is called) never exceeds MaxSpread.
        private void AddSpreadBloom()
        {
            _currentSpread = Mathf.Min(_currentSpread, D.MaxSpread) + D.SpreadPerShot;
        }

        private void TickSpread()
        {
            // Bloom recovers once the gun has been quiet for RecoilRecoveryDelay. Automatic
            // weapons fire faster than that, so bloom still builds while spraying; slow weapons
            // (shotguns, snipers) start closing soon after each shot instead of staying fully
            // bloomed until the next round is ready.
            bool recovering = _fireCooldown.IsReady || Time.time - _lastShotTime >= D.RecoilRecoveryDelay;
            if (!recovering)
            {
                // Still firing: only settle the at-cap overshoot.
                if (_currentSpread > D.MaxSpread)
                    _currentSpread -= (_currentSpread - D.MaxSpread) * SettleFraction();
                return;
            }

            if (_currentSpread > 0f)
                _currentSpread = Mathf.Max(_currentSpread - D.SpreadRecovery * Time.deltaTime, 0f);
        }

        // Share of an at-cap overshoot to return this frame so it is fully gone by the time
        // the next shot is ready — the value eases back over exactly one fire interval.
        private float SettleFraction()
        {
            float remaining = _fireCooldown.Remaining;
            return remaining > Time.deltaTime ? Time.deltaTime / remaining : 1f;
        }

        // ── Recoil ────────────────────────────────────────────────────────────

        private void ApplyRecoil()
        {
            float adsT      = _camera.AdsT;
            float vertMult  = Mathf.Lerp(D.HipRecoilVerticalMultiplier,   D.AdsRecoilMultiplier, adsT);
            float horizMult = Mathf.Lerp(D.HipRecoilHorizontalMultiplier, D.AdsRecoilMultiplier, adsT);

            // Heat is read before this shot adds to it, so the first shot always kicks at 1×.
            // It scales both the kick and its jitter: sustained fire kicks harder and less
            // predictably.
            float kickHeat   = Mathf.Lerp(1f, D.MaxHeatRecoilMultiplier,    _recoilHeat);
            float jitterHeat = Mathf.Lerp(1f, D.RecoilHeatJitterMultiplier, _recoilHeat);
            _recoilHeat = Mathf.Min(_recoilHeat + D.RecoilHeatPerShot, 1f);

            // gunVert/gunHoriz are the weapon's own kick this shot. The camera takes a hip/ADS
            // share of it (vertMult/horizMult); WeaponVisuals shows the gun's side of it.
            // Shared shape for both axes: axisScale × (pattern + jitter).
            // Vertical's pattern is a constant full kick; horizontal's pattern is the
            // authored drift bias applied directly, so it reads from the first shot
            // instead of emerging over several rounds.
            // Vertical kicks in full even at the cap; TickRecoilRecovery eases the overshoot
            // back before the next shot, so the aim holds at the cap but still visibly kicks
            // and settles with each round instead of freezing.
            float gunVert  = D.RecoilScale.y * (1f + BlendedJitter(D.RecoilJitter.y) * jitterHeat) * kickHeat;
            float vertKick = gunVert * vertMult;
            _accumulatedRecoil += vertKick;

            float horizCap = D.MaxAccumulatedHorizontalRecoil;
            float gunHoriz = HorizontalKick(kickHeat, jitterHeat);
            // Alternating mode: when drift reaches the cap, flip the bias direction so
            // long sprays sway back the other way instead of pinning at the limit.
            // OneWay mode keeps the sign of RecoilHorizontalBias throughout — the clamp
            // below stops kicks that would push past the cap, so drift settles there.
            if (D.HorizontalDriftMode == HorizontalDriftMode.Alternating &&
                Mathf.Abs(_accumulatedHorizontalRecoil + gunHoriz * horizMult) > horizCap)
            {
                _horizontalDriftSign = -_horizontalDriftSign;
                gunHoriz = HorizontalKick(kickHeat, jitterHeat);
            }
            float horizKick = Mathf.Clamp(gunHoriz * horizMult,
                                          -horizCap - _accumulatedHorizontalRecoil, horizCap - _accumulatedHorizontalRecoil);
            _accumulatedHorizontalRecoil += horizKick;

            float recoveryFraction = Mathf.Lerp(D.RecoilRecoveryFraction, D.AdsRecoilRecoveryFraction, adsT);
            _camera.AddRecoil(vertKick, horizKick, D.RecoilRecoverySpeed, recoveryFraction, D.RecoilRecoveryDelay);
            _visuals?.AddKick(gunVert, gunHoriz, adsT, ShotInterval);
        }

        // Soonest the next round can fire: burst shots follow BurstInterval rather than RPM.
        private float ShotInterval => D.FireMode == FireMode.Burst
            ? Mathf.Min(60f / D.RoundsPerMinute, D.BurstInterval)
            : 60f / D.RoundsPerMinute;

        private float HorizontalKick(float kickHeat, float jitterHeat)
        {
            float jitter = BlendedJitter(D.RecoilJitter.x) * jitterHeat;
            return D.RecoilScale.x * (D.RecoilHorizontalBias * _horizontalDriftSign + jitter) * kickHeat;
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
                // Gun just went idle — reset the caps instantly so the next burst starts
                // fresh. Heat cools gradually (below) so quick follow-up bursts stay hot.
                _accumulatedRecoil = 0f;
                _accumulatedHorizontalRecoil = 0f;
                _horizontalDriftSign = 1f;
            }
            _wasFiringLastFrame = isFiring;

            float vertOvershoot = _accumulatedRecoil - D.MaxAccumulatedRecoil;
            if (isFiring && vertOvershoot > 0f)
            {
                float settle = vertOvershoot * SettleFraction();
                _accumulatedRecoil -= settle;
                _camera.SettleRecoil(settle);
            }

            if (!isFiring && _recoilHeat > 0f)
                _recoilHeat = Mathf.Max(_recoilHeat - D.RecoilHeatCooldown * Time.deltaTime, 0f);
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
            _crosshair.SetDynamicSpread(baseDeg + _currentSpread * BloomScale(adsT));
        }

        private float BloomScale(float adsT) => Mathf.Lerp(1f, D.GetAdsSpreadMultiplier(_recoilHeat), adsT);

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
