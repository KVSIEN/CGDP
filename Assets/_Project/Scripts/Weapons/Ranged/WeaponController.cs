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
    /// Recoil and spread state live in RecoilProcessor and SpreadProcessor so this class
    /// stays focused on input, ammo, reload and fire-dispatch.
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
        private readonly RecoilProcessor _recoil = new();
        private readonly SpreadProcessor _spread = new();

        private WeaponInstance _current;
        private PlayerMovement _movement;
        private DamageSource   _damageSource;
        private CooldownTimer  _fireCooldown;
        private float _drawTimer;
        private bool  _isReloading;
        private bool  _burstPending;

        // Charge fire mode: accumulates while the trigger is held, releases on the frame
        // the trigger goes up. Reset on Equip so a swap can never carry someone else's charge.
        private float _chargeTimer;
        private bool  _wasChargeHeld;

        /// <summary>Fired whenever magazine, reserve, or reload state changes. Args: magazine, reserve, isReloading.</summary>
        public event Action<int, int, bool> OnAmmoChanged;

        public WeaponInstance Current => _current;
        public WeaponData     Data    => _current?.Data;
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
            if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
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

            float dt = Time.deltaTime;
            bool  cooldownReady   = _fireCooldown.IsReady;
            float cooldownRem     = _fireCooldown.Remaining;
            float settleFraction  = cooldownRem > dt ? dt / cooldownRem : 1f;

            _spread.Tick(dt, cooldownReady, settleFraction);
            float verticalSettle = _recoil.Tick(dt, !cooldownReady, settleFraction);
            if (verticalSettle > 0f) _camera.SettleRecoil(verticalSettle);
            _fireCooldown.Tick(dt);

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
            _isReloading   = false;
            _burstPending  = false;
            _current       = weapon;
            _chargeTimer   = 0f;
            _wasChargeHeld = false;
            _drawTimer     = weapon != null ? weapon.Data.DrawTime : 0f;

            _recoil.Configure(weapon?.Data);
            _spread.Configure(weapon?.Data);

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
            NotifyAmmoChanged();

            ApplyRecoil();
            CastBullet(charge);
            _spread.AddBloom();

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

        private void ApplyRecoil()
        {
            float adsT = _camera.AdsT;
            RecoilShot shot = _recoil.Fire(adsT);
            _camera.AddRecoil(shot.VertKick, shot.HorizKick, D.RecoilRecoverySpeed,
                              shot.RecoveryFraction, D.RecoilRecoveryDelay);
            _visuals?.AddKick(shot.GunVert, shot.GunHoriz, adsT, ShotInterval);
        }

        // Soonest the next round can fire: burst shots follow BurstInterval rather than RPM.
        private float ShotInterval => D.FireMode == FireMode.Burst
            ? Mathf.Min(60f / D.RoundsPerMinute, D.BurstInterval)
            : 60f / D.RoundsPerMinute;

        private void CastBullet(float charge)
        {
            if (D.FireBehavior == null) return;

            float   adsT      = _camera.AdsT;
            float   spreadDeg = _spread.EffectiveConeDeg(adsT, _recoil.Heat);
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
            _crosshair.SetDynamicSpread(_spread.EffectiveConeDeg(_camera.AdsT, _recoil.Heat));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void NotifyAmmoChanged()
        {
            OnAmmoChanged?.Invoke(Magazine, Reserve, _isReloading);
        }
    }
}
