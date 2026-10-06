using System;
using System.Collections;
using UnityEngine;
using CGD.Abilities;
using CGD.Audio;
using CGD.CameraEffects;
using CGD.Combat;
using CGD.Core;
using CGD.Feedback;
using CGD.Input;
using CGD.Items;
using CGD.Player;
using CGD.Settings;
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
        private readonly InputBuffer     _fireBuffer = new();

        // Seconds a weapon swapped away from after its reload's commit point comes back without a draw.
        private const float QuickReturnWindow = 1f;

        private WeaponInstance _current;
        private CharacterStats _stats;
        private PlayerMovement _movement;
        private PlayerDodge    _dodge;
        private PlayerAbilities _abilities;
        private PlayerItemSlots _items;
        private DamageSource   _ownerSource;
        // The owner plus the weapon in hand, so kills are credited to that weapon's perks.
        private DamageSource   _damageSource;
        private CooldownTimer  _fireCooldown;
        private float _drawTimer;
        private bool  _wasDodging;
        private bool  _wasSprinting;
        private bool  _couldAct = true;
        private bool  _isReloading;
        private bool  _reloadCommitted;
        private Coroutine _reload;
        private bool  _holstered;
        private bool  _burstPending;

        // Charge fire mode: accumulates while the trigger is held, releases on the frame
        // the trigger goes up. Reset on Equip so a swap can never carry someone else's charge.
        private float _chargeTimer;
        private bool  _wasChargeHeld;
        private WaitForSeconds _burstWait;
        private float          _burstWaitInterval;

        /// <summary>Fired whenever magazine, reserve, or reload state changes. Args: magazine, reserve, isReloading.</summary>
        public event Action<int, int, bool> OnAmmoChanged;
        // Once per shot: every round of a burst, one per shotgun blast.
        public event Action Fired;

        public WeaponInstance Current => _current;
        public WeaponData     Data    => _current?.Data;
        public int  Magazine          => _current?.Magazine ?? 0;
        public int  Reserve           => _inventory != null && _current != null ? _inventory.Inventory.CountOf(_current.Data.AmmoType) : 0;
        public bool IsReloading       => _isReloading;
        public bool IsHolstered       => _holstered;
        /// <summary>0 at rest, 1 fully charged. Always 0 for non-Charge fire modes.</summary>
        public float ChargeRatio      => Data != null && Data.FireMode == FireMode.Charge && ChargeTime > 0f
                                          ? Mathf.Clamp01(_chargeTimer / ChargeTime) : 0f;

        // A bow's draw orientation reshapes its draw time, damage and spread.
        private DrawStance Stance    => D.StanceFor(_current.Draw);
        private float      ChargeTime => D.ChargeTime * Stance.ChargeTimeMultiplier;

        private WeaponData D        => _current.Data;
        private Vector3    SoundPos => _muzzle != null ? _muzzle.position : transform.position;

        private void Awake()
        {
            _movement     = GetComponent<PlayerMovement>();
            TryGetComponent(out _dodge);
            TryGetComponent(out _abilities);
            TryGetComponent(out _items);
            _ownerSource  = DamageSource.Of(gameObject);
            _damageSource = _ownerSource;
            if (_inventory == null) _inventory = GetComponentInParent<PlayerInventory>();
            _stats = GetComponentInParent<CharacterStats>();

            // Keep HUD reserve count in sync with shared pool.
            if (_inventory != null) _inventory.Inventory.Changed += NotifyAmmoChanged;
            if (_abilities != null) _abilities.AbilityStarted   += OnAbilityStarted;
            if (_items     != null) _items.UseStarted           += OnItemUseStarted;
        }

        private void OnDestroy()
        {
            if (_inventory != null) _inventory.Inventory.Changed -= NotifyAmmoChanged;
            if (_abilities != null) _abilities.AbilityStarted   -= OnAbilityStarted;
            if (_items     != null) _items.UseStarted           -= OnItemUseStarted;
        }

        // Using an ability or a consumable takes the hands off the reload, like a sprint or
        // dodge: lost before the commit point, done early after it. The weapon still waits for
        // the ability or item, so these mostly just save the time.
        private void OnAbilityStarted(Ability ability) => CancelReloadByAction();
        private void OnItemUseStarted(ConsumableDefinition item) => CancelReloadByAction();

        private void CancelReloadByAction()
        {
            if (_isReloading) InterruptReload();
        }

        private void OnDisable()
        {
            if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
        }

        // Put away while something else is in hand (a readied grenade): no firing or
        // reloading and the weapon model is hidden. Taking it out again draws it.
        public void SetHolstered(bool holstered)
        {
            if (_holstered == holstered) return;
            _holstered = holstered;
            if (_visuals != null) _visuals.gameObject.SetActive(!holstered);

            if (holstered)
            {
                StopAllCoroutines();
                _isReloading  = false;
                _burstPending = false;
                if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
            }
            else Equip(_current);
        }

        private void Update()
        {
            bool dodgeStarted  = DodgeStartedThisFrame();
            bool sprintStarted = SprintStartedThisFrame();
            bool lostControl   = ControlLostThisFrame();
            if (_current == null || _holstered) return;

            // Sway ticks through draw/reload so the weapon never freezes mid-animation.
            PushSwayInputs();

            if (GameSettings.Current.InputBuffering && _input.WasPressed(GameAction.Attack))
                _fireBuffer.Press(Time.time);

            if (_drawTimer > 0f)
            {
                // Swap-dodge cancel: a dodge started during the draw covers the rest of it, so
                // the weapon is ready as the dodge ends. Swapping mid-dodge draws in full —
                // swap first, then roll.
                if (dodgeStarted) _drawTimer = 0f;
                else
                {
                    _drawTimer -= Time.deltaTime;
                    return;
                }
            }

            // Anything that interrupts the player stops the reload — a sprint, a dodge, or losing
            // control (stun, mantle, a committed roll): lost before its commit point, done early
            // after it, since once the rounds are in the reload is technically finished.
            if (_isReloading)
            {
                if (!sprintStarted && !dodgeStarted && !lostControl) return;
                InterruptReload();
            }

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
                HandleStanceInput();
                HandleFireInput();
                HandleReloadInput();
            }

            UpdateCrosshair();
        }

        // Tracked every frame, even unarmed, so a dodge already under way at the swap
        // doesn't count as starting during the draw.
        private bool DodgeStartedThisFrame()
        {
            bool dodging = _dodge != null && _dodge.IsDrivingMovement;
            bool started = dodging && !_wasDodging;
            _wasDodging  = dodging;
            return started;
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
            // Reload swap-cancel: leaving a weapon once its rounds are in lets it come
            // straight back, so swap away and back beats waiting out the reload's tail.
            if (_current != null && _isReloading && _reloadCommitted)
                _current.QuickDrawUntil = Time.time + QuickReturnWindow;

            StopAllCoroutines();
            _reload          = null;
            _isReloading     = false;
            _reloadCommitted = false;
            _burstPending    = false;
            _current         = weapon;
            _damageSource    = _ownerSource.WithWeapon(weapon);
            _chargeTimer     = 0f;
            _wasChargeHeld   = false;
            _drawTimer       = weapon != null && Time.time > weapon.QuickDrawUntil ? weapon.Data.DrawTime : 0f;
            if (weapon != null) weapon.QuickDrawUntil = float.NegativeInfinity;
            _fireBuffer.Clear(); // a press meant for the previous weapon doesn't carry over

            _recoil.Configure(weapon?.Data);
            _spread.Configure(weapon?.Data);

            if (_visuals != null)   _visuals.Configure(weapon?.Data);
            if (_crosshair != null) _crosshair.SetDynamicSpread(0f);
            if (_camera != null) _camera.AdsAllowed = weapon != null;
            if (_camera != null && weapon != null)
                _camera.SetAdsProfile(weapon.Data.AdsFovDeg, weapon.Data.AdsSpeed);
            NotifyAmmoChanged();
        }


        private void HandleFireInput()
        {
            if (!_fireCooldown.IsReady) return;

            bool triggerHeld  = _input.GetAction(GameAction.Attack);
            // With input buffering, a press made while the gun couldn't fire (drawing,
            // dodging, reloading, between shots) counts now. Held fire modes need no buffer.
            bool triggerPress = _input.WasPressed(GameAction.Attack) | _fireBuffer.Consume(Time.time);

            if (_current.Magazine <= 0)
            {
                // Sync charge to trigger so post-reload hold doesn't instantly fire.
                _chargeTimer   = 0f;
                _wasChargeHeld = triggerHeld;
                if (_burstPending || !(triggerHeld || triggerPress)) return;
                if (CanReload) StartReload();
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

        // Switching draw orientation drops the current draw, as re-gripping a bow would.
        private void HandleStanceInput()
        {
            if (!D.HasDrawStances || !_input.GetAction(GameAction.WeaponMode)) return;

            _current.Draw = _current.Draw == DrawOrientation.Vertical ? DrawOrientation.Horizontal : DrawOrientation.Vertical;
            _chargeTimer  = 0f;
            FeedbackBus.Notify(_current.Draw == DrawOrientation.Horizontal
                ? "Horizontal draw: faster, wider"
                : "Vertical draw: slower, precise");
        }

        // Hold to charge, release to fire. Releasing before MinChargeToFire cancels the
        // shot with no ammo spent so tap-firing a bow doesn't waste arrows.
        private void HandleChargeInput(bool triggerHeld)
        {
            if (triggerHeld)
            {
                _chargeTimer = Mathf.Min(_chargeTimer + Time.deltaTime, ChargeTime);
            }
            else if (_wasChargeHeld)
            {
                float charge = ChargeTime > 0f ? Mathf.Clamp01(_chargeTimer / ChargeTime) : 1f;
                if (charge >= D.MinChargeToFire) TryFire(charge);
                _chargeTimer = 0f;
            }
            _wasChargeHeld = triggerHeld;
        }

        private void HandleReloadInput()
        {
            if (!_isReloading && _input.GetAction(GameAction.Reload) && CanReload)
                StartReload();
        }

        private bool CanReload => _current.Magazine < _current.MagazineSize && Reserve > 0;

        private void TryFire(float charge = 1f)
        {
            if (_current.Magazine <= 0) return;

            _current.Magazine--;
            _fireCooldown.Start(60f / RoundsPerMinute);
            NotifyAmmoChanged();

            ApplyRecoil();
            CastBullet(charge);
            _spread.AddBloom();

            D.FireSound.TryPlay(SoundPos);
            Noise.Emit(transform.position, D.NoiseRadius, _damageSource);
            Fired?.Invoke();
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
            ? Mathf.Min(60f / RoundsPerMinute, D.BurstInterval)
            : 60f / RoundsPerMinute;

        private void CastBullet(float charge)
        {
            if (D.FireBehavior == null) return;

            float   adsT      = _camera.AdsT;
            DrawStance stance = Stance;
            float   spreadDeg = _spread.EffectiveConeDeg(adsT, _recoil.Heat) * stance.SpreadMultiplier;
            Vector3 forward   = _camera.transform.forward;
            Vector3 volleyAxis = _current.Draw == DrawOrientation.Horizontal ? _camera.transform.right : _camera.transform.up;

            // Camera-origin ray avoids muzzle parallax in third-person.
            D.FireBehavior.Execute(new FireContext
            {
                CameraPosition    = _camera.transform.position,
                CameraForward     = forward,
                SpreadDeg         = spreadDeg,
                Direction         = WeaponFireBehavior.ComputeSpreadDirection(forward, spreadDeg),
                Muzzle            = _muzzle,
                Data              = D,
                Damage            = ResolveDamage() * D.GetChargeDamageScale(charge) * stance.DamageMultiplier,
                Source            = _damageSource,
                Charge            = charge,
                VolleyAxis        = volleyAxis,
                DebugDraw         = _debugDrawBullets,
                DebugHitColor     = _debugHitColor,
                DebugMissColor    = _debugMissColor,
                DebugLineDuration = _debugLineDuration,
            });
        }

        // Base damage → the weapon's attachments → the wielder's buffs and debuffs.
        private float ResolveDamage() => Wielder(ItemStat.Damage, _current.Damage);

        private float RoundsPerMinute => Mathf.Max(1f, Wielder(ItemStat.FireRate, _current.RoundsPerMinute));

        // A weapon value with the wielder's buffs and debuffs (e.g. a perk's faster reloads) applied.
        private float Wielder(ItemStat stat, float value) => _stats != null ? _stats.Apply(stat, value) : value;

        private void StartReload() => _reload = StartCoroutine(Reload());

        // The rounds go in at the commit point; the rest is the tail of the animation, which
        // sprinting or a swap can skip. The magazine filling up is the cue that it's safe.
        private IEnumerator Reload()
        {
            _isReloading     = true;
            _reloadCommitted = false;
            NotifyAmmoChanged();

            D.ReloadSound.TryPlay(SoundPos);

            float time   = Mathf.Max(0.1f, Wielder(ItemStat.ReloadTime,
                _current.Magazine > 0 ? _current.TacticalReloadTime : _current.ReloadTime));
            float commit = time * D.ReloadCommitPoint;
            yield return new WaitForSeconds(commit);

            LoadMagazine();
            _reloadCommitted = true;
            if (time > commit) yield return new WaitForSeconds(time - commit);

            EndReload();
        }

        private void LoadMagazine() => LoadRounds(_current.MagazineSize - _current.Magazine, free: false);

        // Loads up to `count` rounds into the weapon in hand, outside a reload (perks use this).
        // Rounds come from the reserve unless `free`. Returns how many went in.
        public int LoadRounds(int count, bool free)
        {
            if (_current == null) return 0;

            int room      = _current.MagazineSize - _current.Magazine;
            int available = free ? room : Reserve;
            int taken     = Mathf.Min(Mathf.Min(count, room), available);
            if (taken <= 0) return 0;

            _current.Magazine += taken;
            // Remove fires Changed, which covers both mag and reserve in one event.
            if (!free) _inventory.Inventory.Remove(D.AmmoType, taken);
            else       NotifyAmmoChanged();
            return taken;
        }

        // Ends the reload where it is: finished if the rounds are in, lost if not.
        private void InterruptReload()
        {
            if (_reload != null) StopCoroutine(_reload);
            EndReload();
        }

        private void EndReload()
        {
            _reload          = null;
            _isReloading     = false;
            _reloadCommitted = false;
            NotifyAmmoChanged();
        }

        // Stunned, mantling or in a committed roll since last frame. A future knockback that
        // takes control away cancels reloads through here too.
        private bool ControlLostThisFrame()
        {
            bool canAct = _movement == null || _movement.CanAct;
            bool lost   = _couldAct && !canAct;
            _couldAct   = canAct;
            return lost;
        }

        private bool SprintStartedThisFrame()
        {
            bool sprinting = _movement != null && _movement.IsSprinting;
            bool started   = sprinting && !_wasSprinting;
            _wasSprinting  = sprinting;
            return started;
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
