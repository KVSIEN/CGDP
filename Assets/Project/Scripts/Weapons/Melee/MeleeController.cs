using System;
using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Feedback;
using CGD.Input;
using CGD.Meters;
using CGD.Player;

namespace CGD.Weapons
{
    // Swings the player's melee: quick melee (Melee key) with the default data, or the
    // equipped melee weapon on Attack as well. With a melee weapon equipped the aim input
    // raises its guard instead; as an IDamageInterceptor it blocks or parries hits while
    // raised, so it must sit on the same object as the player's HealthManager.
    [RequireComponent(typeof(PlayerInputHandler))]
    public class MeleeController : MonoBehaviour, IDamageInterceptor
    {
        private enum Phase { Idle, Windup, Active, Recovery }

        [Tooltip("Quick melee (fists), used while no melee weapon is equipped")]
        [SerializeField] private MeleeWeaponData _data;
        [SerializeField] private PlayerCamera    _camera;

        [Header("Debug")]
        [SerializeField] private bool  _debugDraw     = true;
        [SerializeField] private float _debugDuration = 0.3f;

        private readonly MeleeHitResolver _resolver = new();
        private readonly ActionTimelineRunner _runner = new();

        private PlayerInputHandler _input;
        private PlayerMovement     _movement;
        private PlayerDodge        _dodge;
        private MeterSet           _meters;
        private DamageSource       _damageSource;
        private Func<float, bool>  _payGuardStamina;

        private MeleeWeaponInstance _equipped;
        private MeleeGuard          _guard;

        private Phase _phase;
        private float _phaseTimer;
        private int   _comboIndex;
        private float _comboResetTimer;
        private MeleeAttackStep _activeStep;
        private bool _usingTimeline;
        private ActionContext _timelineCtx;

        private bool  _heldLastFrame;
        private float _holdTimer;
        private bool  _comboBuffered;
        private bool  _bufferedHeavy;

        // Combo step index of a light attack, or -1 for the heavy attack.
        public event Action<int> AttackStarted;

        public bool IsGuarding => _guard != null && _guard.IsRaised;

        private MeleeWeaponData Data => _equipped != null ? _equipped.Data : _data;
        // Divides wind-up, strike and recovery; a timeline-driven strike keeps its authored frames.
        private float Speed => Mathf.Max(0.1f, Data.AttackSpeed);

        private void Awake()
        {
            _input           = GetComponent<PlayerInputHandler>();
            _movement        = GetComponent<PlayerMovement>();
            _damageSource    = DamageSource.Of(gameObject);
            _payGuardStamina = TryPayStamina;
            TryGetComponent(out _meters);
            TryGetComponent(out _dodge);
        }

        // Called by the loadout when the active slot changes (null = a firearm or empty slot).
        public void Equip(MeleeWeaponInstance weapon)
        {
            if (_equipped == weapon) return;
            InterruptSwing(keepCombo: false);
            _equipped = weapon;
            _guard    = weapon != null && weapon.Data.CanGuard ? new MeleeGuard(weapon.Data.Guard) : null;
        }

        // The tail of a swing's recovery (from its CancelFrom point) can be cut short.
        private bool InCancelWindow =>
            _phase == Phase.Recovery && _phaseTimer <= _activeStep.RecoveryTime / Speed * (1f - _activeStep.CancelFrom);

        private void Update()
        {
            if (Data == null) return;
            if (_phase == Phase.Idle) TickComboReset();
            if (UpdateDodge()) return;
            if (_movement != null && !_movement.CanAct)
            {
                _guard?.Lower();
                return;
            }

            if (UpdateGuard()) return;

            bool held = _input.IsHeld(GameAction.Melee) || (_equipped != null && _input.IsHeld(GameAction.Attack));
            bool releasedThisFrame = _heldLastFrame && !held;
            _heldLastFrame = held;
            if (held) _holdTimer += Time.deltaTime;

            if (_phase == Phase.Idle)
            {
                if (releasedThisFrame)
                {
                    StartAttack(_holdTimer >= Data.HeavyHoldThreshold);
                    _holdTimer = 0f;
                }
                return;
            }

            if (releasedThisFrame)
            {
                if (!_comboBuffered)
                {
                    _comboBuffered = true;
                    _bufferedHeavy = _holdTimer >= Data.HeavyHoldThreshold;
                }
                _holdTimer = 0f;
            }

            TickPhase(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_phase != Phase.Active || _activeStep == null) return;

            Transform cam = _camera.transform;

            if (_usingTimeline)
            {
                _timelineCtx.Origin  = cam.position;
                _timelineCtx.Forward = cam.forward;
                _timelineCtx.Up      = cam.up;

                _runner.Tick();

                if (!_runner.IsRunning)
                    ExitActive();
            }
            else
            {
                _resolver.Tick(cam.position, cam.forward, cam.up, _debugDraw, _debugDuration);
            }
        }

        // Keeps ticking while guarding or dodging, so the combo only carries over a
        // cancel for ComboResetTime — long enough to weave in a parry, not to bank a step.
        private void TickComboReset()
        {
            if (_comboResetTimer <= 0f) return;
            _comboResetTimer -= Time.deltaTime;
            if (_comboResetTimer <= 0f) _comboIndex = 0;
        }

        // A dodge is never blocked and always ends the swing: inside the cancel window that
        // keeps the combo going, earlier the swing and the combo are lost. Nothing else
        // happens while the dodge moves the player. Returns true while dodging.
        private bool UpdateDodge()
        {
            if (_dodge == null || !_dodge.IsDrivingMovement) return false;
            if (_phase != Phase.Idle) InterruptSwing(keepCombo: InCancelWindow);
            _guard?.Lower();
            return true;
        }

        // The guard goes up between swings or in a swing's cancel window (keeping the combo);
        // while it is up nothing else happens, and a swing held during it starts fresh once
        // it drops. Returns true while guarding.
        private bool UpdateGuard()
        {
            if (_guard == null) return false;

            bool canRaise = _phase == Phase.Idle || InCancelWindow;
            if (canRaise && _input.GetAction(GameAction.AimDownSights))
            {
                if (_phase != Phase.Idle) InterruptSwing(keepCombo: true);
                _guard.Raise(Time.time);
            }
            else
            {
                _guard.Lower();
            }

            if (!_guard.IsRaised) return false;
            _heldLastFrame = false;
            _holdTimer     = 0f;
            return true;
        }

        // Returns false (and swings nothing) when the stamina can't be paid.
        private bool StartAttack(bool heavy)
        {
            MeleeWeaponData data = Data;
            float cost = data.StaminaCost.Amount * (heavy ? data.HeavyStaminaMultiplier : 1f);
            if (!TryPayStamina(cost)) return false;

            AttackStarted?.Invoke(heavy ? -1 : _comboIndex);
            _activeStep = heavy ? data.HeavyAttack : data.LightCombo[_comboIndex];
            _comboIndex = heavy ? 0 : (_comboIndex + 1) % data.LightCombo.Length;

            _phase      = Phase.Windup;
            _phaseTimer = _activeStep.WindupTime / Speed;

            _activeStep.SwingSound.TryPlay(transform.position);
            Noise.Emit(transform.position, data.NoiseRadius, _damageSource);
            return true;
        }

        // Stamina is optional on melee: unlike MeterCost.TryPay, a character without the
        // weapon's meter swings and blocks for free.
        private bool TryPayStamina(float amount)
        {
            MeterDefinition definition = Data.StaminaCost.Meter;
            if (definition == null || amount <= 0f || _meters == null || !_meters.TryGet(definition, out var meter)) return true;
            return meter.TrySpend(amount);
        }

        private void TickPhase(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer > 0f) return;

            switch (_phase)
            {
                case Phase.Windup:
                    EnterActive();
                    break;

                case Phase.Active:
                    if (!_usingTimeline)
                        ExitActive();
                    break;

                case Phase.Recovery:
                    EndAttack();
                    break;
            }
        }

        private void EnterActive()
        {
            _phase = Phase.Active;
            _usingTimeline = _activeStep.Timeline != null;

            if (_usingTimeline)
            {
                ActionTimeline timeline = _activeStep.Timeline;
                _phaseTimer = timeline.TotalFrames * Time.fixedDeltaTime;

                Transform cam = _camera.transform;
                _timelineCtx = new ActionContext
                {
                    Origin        = cam.position,
                    Forward       = cam.forward,
                    Up            = cam.up,
                    SourceRoot    = transform.root,
                    Source        = _damageSource,
                    HitMask       = timeline.HitMask,
                    DebugDraw     = _debugDraw,
                    DebugDuration = _debugDuration,
                };

                _runner.Begin(timeline, _timelineCtx);
            }
            else
            {
                _phaseTimer = _activeStep.ActiveTime / Speed;

                var info = new DamageInfo(
                    _activeStep.Damage,
                    _activeStep.ArmorPenetration,
                    _activeStep.DamageType,
                    _activeStep.CriticalMultiplier,
                    _damageSource,
                    _activeStep.OnHitEffects);

                _resolver.Begin(_activeStep, info, _data.HitMask, transform.root);
            }
        }

        private void ExitActive()
        {
            if (_usingTimeline)
                _runner.Stop();
            else
                _resolver.End();

            bool hitAnything = _usingTimeline ? _runner.HitAnything : _resolver.HitAnything;
            if (hitAnything)
                _activeStep.HitSound.TryPlay(transform.position);

            _phase      = Phase.Recovery;
            _phaseTimer = _activeStep.RecoveryTime / Speed;
        }

        private void EndAttack()
        {
            if (_comboBuffered)
            {
                _comboBuffered = false;
                if (StartAttack(_bufferedHeavy)) return;
            }

            _phase           = Phase.Idle;
            _comboResetTimer = Data.ComboResetTime;
        }

        // Ends the swing under way (a cancel, dodge or weapon swap). Hits already dealt stand.
        private void InterruptSwing(bool keepCombo)
        {
            if (_phase == Phase.Active)
            {
                if (_usingTimeline) _runner.Stop();
                else                _resolver.End();
            }

            _phase         = Phase.Idle;
            _activeStep    = null;
            _comboBuffered = false;
            _heldLastFrame = false;
            _holdTimer     = 0f;

            if (keepCombo)
            {
                _comboResetTimer = Data.ComboResetTime;
                return;
            }
            _comboIndex      = 0;
            _comboResetTimer = 0f;
        }

        public float Intercept(in DamageInfo info, float amount, Vector3 point)
        {
            // Only hits with an attacker can be guarded; status ticks and hazards go through.
            GameObject attacker = info.Source.Owner;
            if (!IsGuarding || attacker == null) return amount;

            Vector3 toAttacker = attacker.transform.position - transform.position;
            GuardOutcome outcome = _guard.Resolve(Time.time, _camera.transform.forward, toAttacker, amount,
                                                  _payGuardStamina, out float through);
            switch (outcome)
            {
                case GuardOutcome.Parried:
                    Stunnable stunnable = attacker.GetComponentInParent<Stunnable>();
                    if (stunnable != null) stunnable.ApplyStun(_equipped.Data.Guard.ParryStun);
                    FeedbackBus.Notify("Parried!", NotificationStyle.Success);
                    break;
                case GuardOutcome.Broken:
                    FeedbackBus.Notify("Guard broken", NotificationStyle.Danger);
                    break;
            }
            return through;
        }
    }
}
