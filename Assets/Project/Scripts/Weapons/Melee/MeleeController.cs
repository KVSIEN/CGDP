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
    // The player's melee, both hands.
    //   Main hand: a melee weapon swings its combo on Attack (tap light, hold heavy); a gun fires
    //   through WeaponController instead.
    //   Melee key (the other hand): an offhand weapon (PlayerOffhand) swings its own combo — its
    //   opening strike parries; otherwise it bashes with a shield, the melee weapon in hand, the
    //   gun's bash (or a Tactical Knife), or fists. The start of every bash parries.
    //   Aim input: raises the guard — a shield's if one is held (a gun then can't aim; a tower
    //   shield blocks by itself and leaves the aim free), else the melee weapon's. Attack while
    //   the guard is up bashes with it.
    // As an IDamageInterceptor it parries and blocks hits, so it must sit on the same object as
    // the player's HealthManager. Combos can be woven: a dodge, parry, ability, consumable or
    // weapon switch between steps keeps the next step open for the weapon's weave window.
    [RequireComponent(typeof(PlayerInputHandler))]
    public class MeleeController : MonoBehaviour, IDamageInterceptor
    {
        private enum Phase { Idle, Windup, Active, Recovery }
        private enum Hand  { None, Main, Off }

        [Tooltip("Fists: the bash while the weapon in hand has none of its own, or the hands are empty")]
        [SerializeField] private MeleeWeaponData _data;
        [SerializeField] private PlayerCamera    _camera;

        [Header("Parry")]
        [Tooltip("A parried hit from an attacker farther than this was a shot: it is deflected (Ranged Parry Reflect) instead of staggering the attacker")]
        [SerializeField, Min(0f)] private float _meleeParryRange = 4f;
        [Tooltip("What a parried shot becomes, e.g. DeflectReflect (fired back where you aim). Needs a Reflector on the player")]
        [SerializeField] private ReflectProfile _rangedParryReflect;

        [Header("Debug")]
        [SerializeField] private bool  _debugDraw     = true;
        [SerializeField] private float _debugDuration = 0.3f;

        public const int HeavyAttackIndex = -1;
        public const int BashIndex        = -2;

        private readonly MeleeStrike _strike    = new();
        private readonly MeleeGuard  _guard     = new();
        private readonly SwingInput  _mainInput = new();
        private readonly SwingInput  _offInput  = new();
        private readonly InputBuffer _queuedBash = new();

        private PlayerInputHandler _input;
        private PlayerMovement     _movement;
        private PlayerDodge        _dodge;
        private PlayerAbilities    _abilities;
        private PlayerItemSlots    _items;
        private Reflector          _reflector;
        private MeterSet           _meters;
        private CharacterStats     _stats;
        private DamageSource       _ownerSource;
        private Func<float, bool>  _payGuardStamina;

        private WeaponItem          _inHand;
        private MeleeWeaponInstance _mainWeapon;
        // The bash of the gun in hand (null = fists).
        private MeleeWeaponData     _gunBash;
        private IOffhand            _offhand;
        private MeleeWeaponInstance _offWeapon;
        // Whose guard blocks (a shield, or the melee weapon in hand); null = nothing blocks.
        private MeleeWeaponData     _guardData;
        // What opened the current parry window: its stun and parry reflect answer a parry.
        private MeleeWeaponData     _parryData;

        // The swing under way.
        private Phase _phase;
        private float _phaseTimer;
        private MeleeAttackStep     _activeStep;
        private MeleeWeaponData     _swingData;
        private MeleeWeaponInstance _swingWeapon;
        private DamageSource        _swingSource;
        private bool                _swingIsBash;
        // The next swing, asked for while this one plays.
        private Hand _bufferedHand;
        private bool _bufferedHeavy;

        // Combo step index of a light attack, HeavyAttackIndex or BashIndex.
        public event Action<int> AttackStarted;
        public event Action Parried;

        public bool IsGuarding => _guard.IsRaised;
        // Before a Reflector, so reflects work on what the guard lets through.
        public int  Order      => 0;

        // What the Melee key bashes with when the offhand holds no weapon (an artifact leaves V to the main hand).
        private MeleeWeaponData BashData =>
            _offhand != null && _offhand.OffhandData != null ? _offhand.OffhandData
            : _mainWeapon != null ? _mainWeapon.Data
            : _gunBash != null    ? _gunBash
            : _data;

        // Divides wind-up, strike and recovery; a timeline-driven strike keeps its authored frames.
        private float Speed => Mathf.Max(0.1f, Stat(ItemStat.FireRate, _swingData.AttackSpeed));

        // The tail of a swing's recovery (from its CancelFrom point) can be cut short.
        private bool InCancelWindow =>
            _phase == Phase.Recovery && _phaseTimer <= _activeStep.RecoveryTime / Speed * (1f - _activeStep.CancelFrom);

        private bool CanStartAction => (_phase == Phase.Idle || InCancelWindow) && !_guard.IsExposed(Time.time);

        // A value through the swinging weapon (its passive perks and attachments), then the
        // wielder's buffs and debuffs. Bashes with fists, guns and shields have only the wielder's.
        private float Stat(ItemStat stat, float value)
        {
            if (_swingWeapon != null) value = _swingWeapon.Modify(stat, value);
            return _stats != null ? _stats.Apply(stat, value) : value;
        }

        private void Awake()
        {
            _input           = GetComponent<PlayerInputHandler>();
            _movement        = GetComponent<PlayerMovement>();
            _ownerSource     = DamageSource.Of(gameObject);
            _stats           = GetComponentInParent<CharacterStats>();
            _payGuardStamina = amount => TryPayStamina(_guardData, amount);
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
            ChangeHands();
            _mainInput.ClearQueue();
            _inHand     = weapon;
            _mainWeapon = weapon as MeleeWeaponInstance;
            _gunBash    = weapon is WeaponInstance gun ? gun.QuickMelee : null;
            RebuildGuard();
        }

        // Called by PlayerOffhand: what the offhand holds while the main hand leaves room for it (null = nothing).
        public void SetOffhand(IOffhand offhand)
        {
            if (_offhand == offhand) return;
            ChangeHands();
            _offInput.ClearQueue();
            _offhand   = offhand;
            _offWeapon = offhand as MeleeWeaponInstance;
            RebuildGuard();
        }

        // Switching what's in hand is a weave like a dodge: a combo waits for the switch back,
        // unless the swing is dropped before its cancel window.
        private void ChangeHands()
        {
            if (_phase != Phase.Idle) InterruptSwing(keepCombo: InCancelWindow);
            else                      Weave();
            _queuedBash.Clear();
        }

        // A shield always takes the guard. Otherwise a melee weapon in hand guards with its own,
        // unless an active artifact has taken the aim input.
        private void RebuildGuard()
        {
            bool shield = _offhand != null && _offhand.IsShield;
            bool aimTaken = _offhand != null && _offhand.TakesAim;
            _guardData = shield ? _offhand.OffhandData
                       : _mainWeapon != null && _mainWeapon.Data.CanGuard && !aimTaken ? _mainWeapon.Data
                       : null;

            bool passive = shield && _offhand.BlocksPassively;
            _guard.SetBlock(_guardData != null ? _guardData.Guard : (GuardSettings?)null, passive);
        }

        private void Update()
        {
            bool dodging = UpdateDodge();
            if (dodging || (_movement != null && !_movement.CanAct))
            {
                _guard.Lower();
                QueueWhileBusy();
                return;
            }

            if (TryBash()) return;
            if (UpdateGuard()) return;

            float now = Time.time;
            bool mainReleased = ReadSwing(_mainInput, Hand.Main, out bool mainHeavy);
            bool offReleased  = ReadSwing(_offInput,  Hand.Off,  out bool offHeavy);

            if (_phase == Phase.Idle)
            {
                if      (mainReleased) StartAttack(Hand.Main, mainHeavy);
                else if (offReleased)  StartAttack(Hand.Off,  offHeavy);
                else if (_mainInput.ConsumeQueued(now, out bool heavy)) StartAttack(Hand.Main, heavy);
                else if (_offInput.ConsumeQueued(now,  out heavy))      StartAttack(Hand.Off,  heavy);
                return;
            }

            if      (mainReleased) BufferNext(Hand.Main, mainHeavy);
            else if (offReleased)  BufferNext(Hand.Off,  offHeavy);
            TickPhase(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_phase != Phase.Active || _activeStep == null) return;
            if (!_strike.Tick(_camera.transform, _debugDraw, _debugDuration)) ExitActive();
        }

        private MeleeWeaponInstance WeaponOf(Hand hand) => hand switch
        {
            Hand.Main => _mainWeapon,
            Hand.Off  => _offWeapon,
            _         => null,
        };

        // Attack swings the melee weapon in hand; the Melee key swings an offhand weapon.
        private bool ReadSwing(SwingInput input, Hand hand, out bool heavy)
        {
            MeleeWeaponInstance weapon = WeaponOf(hand);
            GameAction button = hand == Hand.Main ? GameAction.Attack : GameAction.Melee;
            bool held = weapon != null && _input.IsHeld(button);
            return input.Read(held, Time.deltaTime, weapon != null ? weapon.Data.HeavyHoldThreshold : 0f, out heavy);
        }

        private void BufferNext(Hand hand, bool heavy)
        {
            if (_bufferedHand != Hand.None) return;
            _bufferedHand  = hand;
            _bufferedHeavy = heavy;
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

        // Swings and bashes are still read while dodging or stunned; with input buffering on,
        // one pressed then happens as soon as the player can act again.
        private void QueueWhileBusy()
        {
            bool buffering = GameSettings.Current.InputBuffering;
            if (buffering && _offWeapon == null && _input.WasPressed(GameAction.Melee))
                _queuedBash.Press(Time.time);

            if (ReadSwing(_mainInput, Hand.Main, out bool heavy) && buffering) _mainInput.Queue(Time.time, heavy);
            if (ReadSwing(_offInput,  Hand.Off,  out heavy)      && buffering) _offInput.Queue(Time.time, heavy);
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
            float now = Time.time;
            if (_mainWeapon != null) _mainWeapon.Combo.Extend(now, _mainWeapon.Data.WeaveWindow);
            if (_offWeapon  != null) _offWeapon.Combo.Extend(now, _offWeapon.Data.WeaveWindow);
        }

        // The Melee key bashes (unless the offhand holds a weapon, which swings instead); with
        // the guard up and no gun in hand, Attack bashes with the guard. A bash starts between swings or in a
        // swing's cancel window, never while exposed; pressed earlier (or while dodging) it is
        // buffered when input buffering is on. Returns true when a bash started.
        private bool TryBash()
        {
            // With a gun in hand Attack keeps firing (from behind a raised shield); V bashes.
            bool guardBash = IsGuarding && _inHand is not WeaponInstance && _input.WasPressed(GameAction.Attack);
            bool pressed   = guardBash || (_offWeapon == null && _input.WasPressed(GameAction.Melee));
            if (!CanStartAction)
            {
                if (pressed && GameSettings.Current.InputBuffering) _queuedBash.Press(Time.time);
                return false;
            }

            if (!pressed && !_queuedBash.Consume(Time.time)) return false;
            _queuedBash.Clear();
            return StartBash(guardBash ? _guardData : BashData);
        }

        // Returns false (and bashes nothing) when the stamina can't be paid.
        private bool StartBash(MeleeWeaponData data)
        {
            if (!TryPayStamina(data, data.StaminaCost.Amount)) return false;

            // Like raising the guard: a swing in its cancel window ends, keeping the combo.
            if (_phase != Phase.Idle) InterruptSwing(keepCombo: true);
            _guard.Lower();
            OpenParry(data);
            // The Attack press that bashed from the guard must not also swing when released.
            if (_input.IsHeld(GameAction.Attack)) _mainInput.Swallow();

            MeleeWeaponInstance weapon = _mainWeapon != null && data == _mainWeapon.Data ? _mainWeapon : null;
            BeginSwing(data, weapon, data.Bash, bash: true, BashIndex);
            return true;
        }

        private void OpenParry(MeleeWeaponData data)
        {
            _guard.OpenParry(Time.time, data.Guard);
            _parryData = data;
        }

        // The guard goes up between swings or in a swing's cancel window (keeping the combo);
        // while it is up nothing else happens, and a swing held during it starts fresh once
        // it drops. A passive (tower) shield needs no raising. Returns true while guarding.
        private bool UpdateGuard()
        {
            if (!_guard.CanBlock || _guard.IsPassive) return false;

            if (CanStartAction && _input.GetAction(GameAction.AimDownSights))
            {
                if (_phase != Phase.Idle) InterruptSwing(keepCombo: true);
                _mainInput.ClearQueue();
                _offInput.ClearQueue();
                _guard.Raise();
            }
            else
            {
                _guard.Lower();
            }

            if (!_guard.IsRaised) return false;
            _mainInput.Reset();
            _offInput.Reset();
            return true;
        }

        // Returns false (and swings nothing) when the stamina can't be paid.
        private bool StartAttack(Hand hand, bool heavy)
        {
            MeleeWeaponInstance weapon = WeaponOf(hand);
            if (weapon == null) return false;

            MeleeWeaponData data = weapon.Data;
            float cost = data.StaminaCost.Amount * (heavy ? data.HeavyStaminaMultiplier : 1f);
            if (!TryPayStamina(data, cost)) return false;

            if (heavy)
            {
                weapon.Combo.Reset();
                BeginSwing(data, weapon, data.HeavyAttack, bash: false, HeavyAttackIndex);
                return true;
            }

            int step = weapon.Combo.StepAt(Time.time);
            weapon.Combo.Begin(step, data.LightCombo.Length);
            // An offhand weapon's opening strike parries, like a bash; the rest of its string doesn't.
            if (hand == Hand.Off && step == 0) OpenParry(data);
            BeginSwing(data, weapon, data.LightCombo[step], bash: false, step);
            return true;
        }

        private void BeginSwing(MeleeWeaponData data, MeleeWeaponInstance weapon, MeleeAttackStep step, bool bash, int index)
        {
            _swingData    = data;
            _swingWeapon  = weapon;
            _swingIsBash  = bash;
            // Kills are credited to the swinging weapon, or for a bash to the gun in hand.
            _swingSource  = _ownerSource.WithWeapon(weapon != null ? weapon : _inHand);
            _activeStep   = step;
            _bufferedHand = Hand.None;
            _phase        = Phase.Windup;
            _phaseTimer   = step.WindupTime / Speed;

            AttackStarted?.Invoke(index);
            step.SwingSound.TryPlay(transform.position);
            Noise.Emit(transform.position, data.NoiseRadius, _swingSource);
        }

        // Stamina is optional on melee: unlike MeterCost.TryPay, a character without the
        // weapon's meter swings and blocks for free.
        private bool TryPayStamina(MeleeWeaponData data, float amount)
        {
            MeterDefinition definition = data != null ? data.StaminaCost.Meter : null;
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
                    if (!_strike.UsesTimeline) ExitActive();
                    break;

                case Phase.Recovery:
                    EndAttack();
                    break;
            }
        }

        private void EnterActive()
        {
            _phase = Phase.Active;
            Transform aim = _camera.transform;

            if (_activeStep.Timeline != null)
            {
                _phaseTimer = _strike.BeginTimeline(_activeStep.Timeline, aim, transform.root, _swingSource,
                                                    DamageScale(_activeStep.Damage), _debugDraw, _debugDuration);
                return;
            }

            _phaseTimer = _activeStep.ActiveTime / Speed;

            bool crit = UnityEngine.Random.value < Stat(ItemStat.CritChance, 0f);
            var info = new DamageInfo(
                Stat(ItemStat.Damage, _activeStep.Damage),
                Mathf.Clamp01(Stat(ItemStat.ArmorPenetration, _activeStep.ArmorPenetration)),
                _activeStep.DamageType,
                Mathf.Max(1f, Stat(ItemStat.CritDamage, _activeStep.CriticalMultiplier)),
                _swingSource,
                _activeStep.OnHitEffects,
                bonuses: new HitBonuses(crit, Stat(ItemStat.StatusChance, 0f), Stat(ItemStat.StatusDamage, 0f)));

            float reach = Mathf.Max(0.1f, Stat(ItemStat.Range, 1f));
            _strike.BeginShapes(_activeStep, info, _swingData.HitMask, transform.root, reach);
        }

        // A timeline strike authors its own damage per event, so buffs reach it as a multiplier,
        // measured on the step's damage so flat bonuses keep their size.
        private float DamageScale(float stepDamage) =>
            stepDamage > 0f ? Stat(ItemStat.Damage, stepDamage) / stepDamage : 1f;

        private void ExitActive()
        {
            if (_strike.End()) _activeStep.HitSound.TryPlay(transform.position);

            _phase      = Phase.Recovery;
            _phaseTimer = _activeStep.RecoveryTime / Speed;
        }

        private void EndAttack()
        {
            if (_bufferedHand != Hand.None)
            {
                Hand next = _bufferedHand;
                _bufferedHand = Hand.None;
                if (StartAttack(next, _bufferedHeavy)) return;
            }

            _phase = Phase.Idle;
            // A bash leaves the combo where it was, like raising the guard.
            if (!_swingIsBash && _swingWeapon != null) _swingWeapon.Combo.Release(Time.time, _swingData.ComboResetTime);
        }

        // Ends the swing under way (a cancel, dodge or weapon swap). Hits already dealt stand.
        private void InterruptSwing(bool keepCombo)
        {
            if (_phase == Phase.Active) _strike.End();

            _phase        = Phase.Idle;
            _activeStep   = null;
            _bufferedHand = Hand.None;
            _mainInput.Reset();
            _offInput.Reset();

            if (_swingWeapon == null) return;
            if (keepCombo) _swingWeapon.Combo.Release(Time.time, _swingData.WeaveWindow);
            else           _swingWeapon.Combo.Reset();
        }

        public float Intercept(in DamageInfo info, float amount, Vector3 point)
        {
            // Only hits with an attacker can be parried or guarded; status ticks and hazards go through.
            GameObject attacker = info.Source.Owner;
            if (attacker == null) return amount;

            Vector3 toAttacker = attacker.transform.position - transform.position;
            GuardOutcome outcome = _guard.Resolve(Time.time, _camera.transform.forward, toAttacker, amount,
                                                  _payGuardStamina, out float through);
            switch (outcome)
            {
                case GuardOutcome.Parried:
                    if (toAttacker.magnitude > _meleeParryRange) Deflect(info, amount, attacker);
                    else                                          Stagger(info, amount, attacker);
                    Weave();
                    Parried?.Invoke();
                    break;
                case GuardOutcome.Broken:
                    FeedbackBus.Notify("Guard broken", NotificationStyle.Danger);
                    break;
            }
            return through;
        }

        // A parried strike up close staggers the attacker, and the parrying weapon's own parry
        // reflect (a sword's riposte) answers it.
        private void Stagger(in DamageInfo info, float amount, GameObject attacker)
        {
            Stunnable stunnable = attacker.GetComponentInParent<Stunnable>();
            if (stunnable != null) stunnable.ApplyStun(_parryData.Guard.ParryStun);
            if (_parryData.ParryReflect != null && _reflector != null)
                _reflector.Release(_parryData.ParryReflect, info, amount, attacker);
            FeedbackBus.Notify("Parried!", NotificationStyle.Success);
        }

        // A parried shot is knocked away, and sent back when a ranged parry reflect is set.
        private void Deflect(in DamageInfo info, float amount, GameObject attacker)
        {
            if (_rangedParryReflect != null && _reflector != null && !info.IsReflected)
                _reflector.Release(_rangedParryReflect, info, amount, attacker);
            else
                FeedbackBus.Notify("Parried!", NotificationStyle.Success);
        }
    }
}
