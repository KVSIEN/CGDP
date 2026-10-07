using System;
using UnityEngine;
using CGD.Abilities;
using CGD.Audio;
using CGD.Combat;
using CGD.Feedback;
using CGD.Input;
using CGD.Items;
using CGD.Meters;
using CGD.Player;
using CGD.Settings;
using CGD.Stats;

namespace CGD.Weapons
{
    // Swings the player's melee. The Melee key bashes with whatever is in hand: a gun's bash
    // (or the offhand knife a perk swaps in), a melee weapon's guard bash, or fists. The start
    // of every bash parries. With a melee weapon equipped, Attack swings its combo, the aim
    // input raises its guard (block), and Attack while the guard is up bashes too. As an
    // IDamageInterceptor it parries and blocks hits, so it must sit on the same object as the
    // player's HealthManager.
    // Combos can be woven: a dodge, parry, ability, consumable or weapon switch between steps keeps
    // the next step open for the weapon's weave window instead of the short idle reset.
    [RequireComponent(typeof(PlayerInputHandler))]
    public class MeleeController : MonoBehaviour, IDamageInterceptor
    {
        private enum Phase { Idle, Windup, Active, Recovery }

        [Tooltip("Fists: the bash while the weapon in hand has none of its own, or the hands are empty")]
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
        private PlayerItemSlots    _items;
        private Reflector          _reflector;
        private MeterSet           _meters;
        private CharacterStats     _stats;
        private DamageSource       _ownerSource;
        // The owner plus the weapon in hand, so kills are credited to that weapon's perks.
        private DamageSource       _damageSource;
        private Func<float, bool>  _payGuardStamina;

        private WeaponItem          _inHand;
        private MeleeWeaponInstance _equipped;
        // The bash of the gun in hand (null = fists).
        private MeleeWeaponData     _gunBash;
        private MeleeGuard          _guard;
        private readonly InputBuffer _queuedSwing = new();
        private readonly InputBuffer _queuedBash  = new();
        private bool _queuedHeavy;
        // A bash started from the guard with Attack: that press must not also swing once released.
        private bool _swallowAttack;
        private bool _bashing;

        private Phase _phase;
        private float _phaseTimer;
        private MeleeAttackStep _activeStep;
        private bool _usingTimeline;
        private ActionContext _timelineCtx;

        private bool  _heldLastFrame;
        private float _holdTimer;
        private bool  _comboBuffered;
        private bool  _bufferedHeavy;

        public const int HeavyAttackIndex = -1;
        public const int BashIndex        = -2;

        // Combo step index of a light attack, HeavyAttackIndex or BashIndex.
        public event Action<int> AttackStarted;
        public event Action Parried;

        public bool IsGuarding => _guard != null && _guard.IsRaised;
        // Before a Reflector, so reflects work on what the guard lets through.
        public int  Order      => 0;

        private MeleeWeaponData Data  => _equipped != null ? _equipped.Data : _gunBash != null ? _gunBash : _data;
        // Only melee weapons have combos; bashes don't chain.
        private ComboState      Combo => _equipped?.Combo;
        // Divides wind-up, strike and recovery; a timeline-driven strike keeps its authored frames.
        private float Speed => Mathf.Max(0.1f, Stat(ItemStat.FireRate, Data.AttackSpeed));

        // A weapon value through the equipped weapon (its passive perks and attachments), then
        // the wielder's buffs and debuffs. Fists have only the wielder's.
        private float Stat(ItemStat stat, float value)
        {
            if (_equipped != null) value = _equipped.Modify(stat, value);
            return _stats != null ? _stats.Apply(stat, value) : value;
        }

        // A timeline strike authors its own damage per event, so buffs reach it as a multiplier,
        // measured on the step's damage so flat bonuses keep their size.
        private float DamageScale(float stepDamage) =>
            stepDamage > 0f ? Stat(ItemStat.Damage, stepDamage) / stepDamage : 1f;

        private void Awake()
        {
            _input           = GetComponent<PlayerInputHandler>();
            _movement        = GetComponent<PlayerMovement>();
            _ownerSource     = DamageSource.Of(gameObject);
            _damageSource    = _ownerSource;
            _stats           = GetComponentInParent<CharacterStats>();
            _payGuardStamina = TryPayStamina;
            TryGetComponent(out _meters);
            TryGetComponent(out _dodge);
            TryGetComponent(out _abilities);
            TryGetComponent(out _items);
            TryGetComponent(out _reflector);
            RebuildGuard();
        }

        private void OnEnable()
        {
            if (_abilities != null) _abilities.AbilityUsed += OnAbilityUsed;
            if (_items     != null) _items.UseEnded         += OnItemUsed;
        }

        private void OnDisable()
        {
            if (_abilities != null) _abilities.AbilityUsed -= OnAbilityUsed;
            if (_items     != null) _items.UseEnded         -= OnItemUsed;
        }

        // Called by the loadout when the active slot changes (null = an empty slot).
        public void Equip(WeaponItem weapon)
        {
            if (_inHand == weapon) return;
            // Switching away is a weave like a dodge: the combo waits for the switch back,
            // unless the swing is dropped before its cancel window.
            if (_phase != Phase.Idle) InterruptSwing(keepCombo: InCancelWindow);
            else                      Weave();
            _queuedSwing.Clear();
            _queuedBash.Clear();
            _inHand   = weapon;
            _equipped = weapon as MeleeWeaponInstance;
            _gunBash  = weapon is WeaponInstance gun ? gun.QuickMelee : null;
            // A gun's bash credits the gun, so its kill and hit perks answer to it.
            _damageSource = _ownerSource.WithWeapon(weapon);
            RebuildGuard();
        }

        private void RebuildGuard() => _guard = Data != null ? new MeleeGuard(Data.Guard) : null;

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

            if (TryBash()) return;
            if (UpdateGuard()) return;

            bool held = ReadAttackHeld();
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
            if (GameSettings.Current.InputBuffering && _input.WasPressed(GameAction.Melee))
                _queuedBash.Press(Time.time);

            bool held = ReadAttackHeld();
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

        private void OnAbilityUsed(Ability ability) => WeaveAction();

        // A consumable used mid-combo weaves like an ability; a cancelled channel doesn't.
        private void OnItemUsed(ConsumableDefinition item, bool completed)
        {
            if (completed) WeaveAction();
        }

        // An ability or consumable between steps. Before a swing's cancel window the swing
        // simply carries on.
        private void WeaveAction()
        {
            if (InCancelWindow)            InterruptSwing(keepCombo: true);
            else if (_phase == Phase.Idle) Weave();
        }

        private void Weave()
        {
            if (Data != null) Combo?.Extend(Time.time, Data.WeaveWindow);
        }

        // Attack only swings a melee weapon, and not while the press that bashed is still held.
        private bool ReadAttackHeld()
        {
            bool held = _equipped != null && _input.IsHeld(GameAction.Attack);
            if (!held) _swallowAttack = false;
            return held && !_swallowAttack;
        }

        // The Melee key bashes; with the guard up, so does Attack. A bash starts between swings
        // or in a swing's cancel window; pressed earlier (or while dodging) it is buffered when
        // input buffering is on. Returns true when a bash started.
        private bool TryBash()
        {
            bool pressed = _input.WasPressed(GameAction.Melee) || (IsGuarding && _input.WasPressed(GameAction.Attack));
            if (_phase != Phase.Idle && !InCancelWindow)
            {
                if (pressed && GameSettings.Current.InputBuffering) _queuedBash.Press(Time.time);
                return false;
            }

            if (!pressed && !_queuedBash.Consume(Time.time)) return false;
            _queuedBash.Clear();
            return StartBash();
        }

        // Returns false (and bashes nothing) when the stamina can't be paid.
        private bool StartBash()
        {
            MeleeWeaponData data = Data;
            if (!TryPayStamina(data.StaminaCost.Amount)) return false;

            // Like raising the guard: a swing in its cancel window ends, keeping the combo.
            if (_phase != Phase.Idle) InterruptSwing(keepCombo: true);
            _guard.Lower();
            _guard.OpenParry(Time.time);
            _swallowAttack = _input.IsHeld(GameAction.Attack);
            _heldLastFrame = false;
            _holdTimer     = 0f;

            AttackStarted?.Invoke(BashIndex);
            _bashing    = true;
            _activeStep = data.Bash;
            _phase      = Phase.Windup;
            _phaseTimer = _activeStep.WindupTime / Speed;

            _activeStep.SwingSound.TryPlay(transform.position);
            Noise.Emit(transform.position, data.NoiseRadius, _damageSource);
            return true;
        }

        // The guard goes up between swings or in a swing's cancel window (keeping the combo);
        // while it is up nothing else happens, and a swing held during it starts fresh once
        // it drops. Returns true while guarding.
        private bool UpdateGuard()
        {
            if (!Data.CanGuard) return false;

            bool canRaise = _phase == Phase.Idle || InCancelWindow;
            if (canRaise && _input.GetAction(GameAction.AimDownSights))
            {
                if (_phase != Phase.Idle) InterruptSwing(keepCombo: true);
                _queuedSwing.Clear();
                _guard.Raise();
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

            _bashing = false;
            if (heavy)
            {
                AttackStarted?.Invoke(HeavyAttackIndex);
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
                    DamageScale   = DamageScale(_activeStep.Damage),
                    HitMask       = timeline.HitMask,
                    DebugDraw     = _debugDraw,
                    DebugDuration = _debugDuration,
                };

                _runner.Begin(timeline, _timelineCtx);
            }
            else
            {
                _phaseTimer = _activeStep.ActiveTime / Speed;

                bool crit = UnityEngine.Random.value < Stat(ItemStat.CritChance, 0f);
                var info = new DamageInfo(
                    Stat(ItemStat.Damage, _activeStep.Damage),
                    Mathf.Clamp01(Stat(ItemStat.ArmorPenetration, _activeStep.ArmorPenetration)),
                    _activeStep.DamageType,
                    Mathf.Max(1f, Stat(ItemStat.CritDamage, _activeStep.CriticalMultiplier)),
                    _damageSource,
                    _activeStep.OnHitEffects,
                    bonuses: new HitBonuses(crit, Stat(ItemStat.StatusChance, 0f), Stat(ItemStat.StatusDamage, 0f)));

                float reach = Mathf.Max(0.1f, Stat(ItemStat.Range, 1f));
                _resolver.Begin(_activeStep, info, Data.HitMask, transform.root, reach);
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
            // A bash leaves the combo where it was, like raising the guard.
            if (!_bashing) Combo?.Release(Time.time, Data.ComboResetTime);
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

            if (keepCombo) Combo?.Release(Time.time, Data.WeaveWindow);
            else           Combo?.Reset();
        }

        public float Intercept(in DamageInfo info, float amount, Vector3 point)
        {
            // Only hits with an attacker can be parried or guarded; status ticks and hazards go through.
            GameObject attacker = info.Source.Owner;
            if (_guard == null || attacker == null) return amount;

            Vector3 toAttacker = attacker.transform.position - transform.position;
            GuardOutcome outcome = _guard.Resolve(Time.time, _camera.transform.forward, toAttacker, amount,
                                                  _payGuardStamina, out float through);
            switch (outcome)
            {
                case GuardOutcome.Parried:
                    Stunnable stunnable = attacker.GetComponentInParent<Stunnable>();
                    if (stunnable != null) stunnable.ApplyStun(Data.Guard.ParryStun);
                    if (Data.ParryReflect != null && _reflector != null)
                        _reflector.Release(Data.ParryReflect, info, amount, attacker);
                    Weave();
                    Parried?.Invoke();
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
