using System;
using UnityEngine;
using CGD.Abilities;
using CGD.Audio;
using CGD.Combat;
using CGD.Feedback;
using CGD.Input;
using CGD.Meters;
using CGD.Player;
using CGD.Settings;

namespace CGD.Weapons
{
    // Swings the player's melee: quick melee (Melee key) with the default data, or the
    // equipped melee weapon on Attack as well. With a melee weapon equipped the aim input
    // raises its guard instead; as an IDamageInterceptor it blocks or parries hits while
    // raised, so it must sit on the same object as the player's HealthManager.
    // Combos can be woven: a dodge, parry, ability or weapon switch between steps keeps
    // the next step open for the weapon's weave window instead of the short idle reset.
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
        private PlayerAbilities    _abilities;
        private Reflector          _reflector;
        private MeterSet           _meters;
        private DamageSource       _damageSource;
        private Func<float, bool>  _payGuardStamina;

        private MeleeWeaponInstance _equipped;
        private MeleeGuard          _guard;
        private readonly ComboState _fistCombo = new();
        private readonly InputBuffer _queuedSwing = new();
        private bool _queuedHeavy;

        private Phase _phase;
        private float _phaseTimer;
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
        // Before a Reflector, so reflects work on what the guard lets through.
        public int  Order      => 0;

        private MeleeWeaponData Data  => _equipped != null ? _equipped.Data  : _data;
        private ComboState      Combo => _equipped != null ? _equipped.Combo : _fistCombo;
        private bool AttackHeld => _input.IsHeld(GameAction.Melee) || (_equipped != null && _input.IsHeld(GameAction.Attack));
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
            TryGetComponent(out _abilities);
            TryGetComponent(out _reflector);
        }

        private void OnEnable()
        {
            if (_abilities != null) _abilities.AbilityUsed += OnAbilityUsed;
        }

        private void OnDisable()
        {
            if (_abilities != null) _abilities.AbilityUsed -= OnAbilityUsed;
        }

        // Called by the loadout when the active slot changes (null = a firearm or empty slot).
        public void Equip(MeleeWeaponInstance weapon)
        {
            if (_equipped == weapon) return;
            // Switching away is a weave like a dodge: the combo waits for the switch back,
            // unless the swing is dropped before its cancel window.
            if (_phase != Phase.Idle) InterruptSwing(keepCombo: InCancelWindow);
            else                      Weave();
            _queuedSwing.Clear();
            _equipped = weapon;
            _guard    = weapon != null && weapon.Data.CanGuard ? new MeleeGuard(weapon.Data.Guard) : null;
        }

        // The tail of a swing's recovery (from its CancelFrom point) can be cut short.
        private bool InCancelWindow =>
            _phase == Phase.Recovery && _phaseTimer <= _activeStep.RecoveryTime / Speed * (1f - _activeStep.CancelFrom);

        private void Update()
        {
            if (Data == null) return;
            bool dodging = UpdateDodge();
            if (dodging || (_movement != null && !_movement.CanAct))
            {
                _guard?.Lower();
                QueueWhileBusy();
                return;
            }

            if (UpdateGuard()) return;

            bool held = AttackHeld;
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
                else if (_queuedSwing.Consume(Time.time))
                {
                    StartAttack(_queuedHeavy);
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

        // A dodge is never blocked and always ends the swing: inside the cancel window that
        // keeps the combo going, earlier the swing and the combo are lost. The weave window
        // counts from the end of the dodge. Nothing else happens while the dodge moves the
        // player. Returns true while dodging.
        private bool UpdateDodge()
        {
            if (_dodge == null || !_dodge.IsDrivingMovement) return false;
            if (_phase != Phase.Idle) InterruptSwing(keepCombo: InCancelWindow);
            Weave();
            return true;
        }

        // Swings are still read while dodging or stunned; with input buffering on, one
        // released then is swung as soon as the player can act again.
        private void QueueWhileBusy()
        {
            bool held = AttackHeld;
            bool released = _heldLastFrame && !held;
            _heldLastFrame = held;
            if (held) _holdTimer += Time.deltaTime;
            if (!released) return;

            if (GameSettings.Current.InputBuffering)
            {
                _queuedSwing.Press(Time.time);
                _queuedHeavy = _holdTimer >= Data.HeavyHoldThreshold;
            }
            _holdTimer = 0f;
        }

        // An ability fired mid-combo. Before a swing's cancel window the swing simply carries on.
        private void OnAbilityUsed(Ability ability)
        {
            if (InCancelWindow)            InterruptSwing(keepCombo: true);
            else if (_phase == Phase.Idle) Weave();
        }

        private void Weave()
        {
            if (Data != null) Combo.Extend(Time.time, Data.WeaveWindow);
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
                _queuedSwing.Clear();
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

            if (heavy)
            {
                AttackStarted?.Invoke(-1);
                _activeStep = data.HeavyAttack;
                Combo.Reset();
            }
            else
            {
                int step = Combo.StepAt(Time.time);
                AttackStarted?.Invoke(step);
                _activeStep = data.LightCombo[step];
                Combo.Begin(step, data.LightCombo.Length);
            }

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

            _phase = Phase.Idle;
            Combo.Release(Time.time, Data.ComboResetTime);
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

            if (keepCombo) Combo.Release(Time.time, Data.WeaveWindow);
            else           Combo.Reset();
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
                    if (_equipped.Data.ParryReflect != null && _reflector != null)
                        _reflector.Release(_equipped.Data.ParryReflect, info, amount, attacker);
                    Weave();
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
