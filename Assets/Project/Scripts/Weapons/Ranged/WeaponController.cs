using System;
using System.Collections;
using UnityEngine;
using CGD.Audio;
using CGD.CameraEffects;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Items;
using CGD.Player;
using CGD.Stats;
using CGD.UI;

namespace CGD.Weapons
{
    /// <summary>
    /// Player weapon-firing controller. Delegates recoil to RecoilProcessor, spread to
    /// SpreadProcessor, visuals to WeaponVisuals; this class handles input, ammo and fire-dispatch.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerCamera       _camera;
        [Tooltip("Optional — adds a visual view kick on each shot")]
        [SerializeField] private CameraEffectsController _cameraEffects;
        [SerializeField] private PlayerInventory    _inventory; // reserve ammo pool; auto-fetched from same object if unset
        [SerializeField] private CrosshairHUD       _crosshair;
        [SerializeField] private Transform          _muzzle;    // optional: origin for visual FX
        [SerializeField] private WeaponVisuals      _visuals;   // optional: weapon model kick

        [Header("Debug")]
        [SerializeField] private bool  _debugDrawBullets = true;
        [SerializeField] private float _debugLineDuration = 2f;
        [SerializeField] private Color _debugHitColor  = Color.red;
        [SerializeField] private Color _debugMissColor = Color.yellow;

        private readonly RecoilProcessor _recoil = new();
        private readonly SpreadProcessor _spread = new();

        private WeaponInstance _current;
        private CharacterStats _stats;
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
        private WaitForSeconds _burstWait;
        private float          _burstWaitInterval;

        /// <summary>Fired whenever magazine, reserve, or reload state changes. Args: magazine, reserve, isReloading.</summary>
        public event Action<int, int, bool> OnAmmoChanged;

        public WeaponInstance Current => _current;
        public WeaponData     Data    => _current?.Data;
        public int  Magazine          => _current?.Magazine ?? 0;
        public int  Reserve           => _inventory != null && _current != null ? _inventory.Inventory.CountOf(_current.Data.AmmoType) : 0;
        public bool IsReloading       => _isReloading;
        /// <summary>0 at rest, 1 fully charged. Always 0 for non-Charge fire modes.</summary>
        public float ChargeRatio      => Data != null && Data.FireMode == FireMode.Charge && Data.ChargeTime > 0f
                                          ? Mathf.Clamp01(_chargeTimer / Data.ChargeTime) : 0f;

        private WeaponData D        => _current.Data;
        private Vector3    SoundPos => _muzzle != null ? _muzzle.position : transform.position;

        private void Awake()
        {
            _movement     = GetComponent<PlayerMovement>();
            _damageSource = DamageSource.Of(gameObject);
            if (_inventory == null) _inventory = GetComponentInParent<PlayerInventory>();
            _stats = GetComponentInParent<CharacterStats>();

            // Keep HUD reserve count in sync with shared pool.
            if (_inventory != null) _inventory.Inventory.Changed += NotifyAmmoChanged;
        }

        private void OnDestroy()
        {
            if (_inventory != null) _inventory.Inventory.Changed -= NotifyAmmoChanged;
        }

        private void OnDisable()
        {
            if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
        }

        private void Update()
        {
            if (_current == null) return;

            // Sway ticks through draw/reload so the weapon never freezes mid-animation.
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


        private void HandleFireInput()
        {
            if (!_fireCooldown.IsReady) return;

            bool triggerHeld  = _input.GetAction(GameAction.Attack);
            bool triggerPress = _input.WasPressed(GameAction.Attack);

            if (_current.Magazine <= 0)
            {
                // Sync charge to trigger so post-reload hold doesn't instantly fire.
                _chargeTimer   = 0f;
                _wasChargeHeld = triggerHeld;
                if (_burstPending || !(triggerHeld || triggerPress)) return;
                if (CanReload) StartCoroutine(Reload());
                else if (triggerPress) D.EmptySound.TryPlay(SoundPos);
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

        private bool CanReload => _current.Magazine < _current.MagazineSize && Reserve > 0;

        private void TryFire(float charge = 1f)
        {
            if (_current.Magazine <= 0) return;

            _current.Magazine--;
            _fireCooldown.Start(60f / _current.RoundsPerMinute);
            NotifyAmmoChanged();

            ApplyRecoil();
            CastBullet(charge);
            _spread.AddBloom();

            D.FireSound.TryPlay(SoundPos);
            Noise.Emit(transform.position, D.NoiseRadius, _damageSource);
        }

        private IEnumerator FireBurst()
        {
            _burstPending = true;
            float interval = D.BurstInterval;
            if (_burstWait == null || !Mathf.Approximately(_burstWaitInterval, interval))
            {
                _burstWait = new WaitForSeconds(interval);
                _burstWaitInterval = interval;
            }
            for (int i = 0; i < D.BurstCount; i++)
            {
                if (_current.Magazine <= 0) break;
                TryFire();
                if (i < D.BurstCount - 1)
                    yield return _burstWait;
            }
            _burstPending = false;
        }

        private void ApplyRecoil()
        {
            float adsT = _camera.AdsT;
            RecoilShot shot = _recoil.Fire(adsT);
            _camera.AddRecoil(shot.VertKick, shot.HorizKick, D.RecoilRecoverySpeed,
                              shot.RecoveryFraction, D.RecoilRecoveryDelay);
            if (_visuals != null) _visuals.AddKick(shot.GunVert, shot.GunHoriz, adsT, ShotInterval);
            if (_cameraEffects != null) _cameraEffects.AddRecoil(shot.VertKick, shot.HorizKick);
        }

        // Soonest the next round can fire: burst shots follow BurstInterval rather than RPM.
        private float ShotInterval => D.FireMode == FireMode.Burst
            ? Mathf.Min(60f / _current.RoundsPerMinute, D.BurstInterval)
            : 60f / _current.RoundsPerMinute;

        private void CastBullet(float charge)
        {
            if (D.FireBehavior == null) return;

            float   adsT      = _camera.AdsT;
            float   spreadDeg = _spread.EffectiveConeDeg(adsT, _recoil.Heat);
            Vector3 forward   = _camera.transform.forward;

            // Camera-origin ray avoids muzzle parallax in third-person.
            D.FireBehavior.Execute(new FireContext
            {
                CameraPosition    = _camera.transform.position,
                CameraForward     = forward,
                SpreadDeg         = spreadDeg,
                Direction         = WeaponFireBehavior.ComputeSpreadDirection(forward, spreadDeg),
                Muzzle            = _muzzle,
                Data              = D,
                Damage            = ResolveDamage(),
                Source            = _damageSource,
                Charge            = charge,
                DebugDraw         = _debugDrawBullets,
                DebugHitColor     = _debugHitColor,
                DebugMissColor    = _debugMissColor,
                DebugLineDuration = _debugLineDuration,
            });
        }

        // Base damage → the weapon's attachments → the wielder's buffs and debuffs.
        private float ResolveDamage() =>
            _stats != null ? _stats.Apply(ItemStat.Damage, _current.Damage) : _current.Damage;

        private IEnumerator Reload()
        {
            _isReloading = true;
            NotifyAmmoChanged();

            D.ReloadSound.TryPlay(SoundPos);

            float time = _current.Magazine > 0 ? _current.TacticalReloadTime : _current.ReloadTime;
            yield return new WaitForSeconds(time);

            int needed    = _current.MagazineSize - _current.Magazine;
            int available = _inventory != null ? _inventory.Inventory.CountOf(D.AmmoType) : 0;
            int taken     = Mathf.Min(needed, available);
            if (taken > 0)
            {
                _current.Magazine += taken;
                // Remove fires Changed, which covers both mag and reserve in one event.
                _inventory.Inventory.Remove(D.AmmoType, taken);
            }

            _isReloading = false;
            NotifyAmmoChanged();
        }

        private void UpdateCrosshair()
        {
            if (_crosshair == null) return;
            _crosshair.SetDynamicSpread(_spread.EffectiveConeDeg(_camera.AdsT, _recoil.Heat));
        }

        private void NotifyAmmoChanged()
        {
            OnAmmoChanged?.Invoke(Magazine, Reserve, _isReloading);
        }
    }
}
