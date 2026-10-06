using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Meters;
using CGD.Player;
using CGD.Settings;

namespace CGD.Abilities
{
    // Runs the four ability slots: input, charges and their recharge, resource costs and
    // cast times. Only one ability casts at a time; a cast is cancelled when the player
    // can't act. Costs are paid from the MeterSet on the player, when the ability fires.
    // With input buffering on, a press that can't go off yet (another ability on the same
    // frame or casting, a dodge, a stun) waits briefly, so 1→2 in quick succession both fire.
    // Abilities with resource scaling spend more than their cost for a stronger cast, and
    // abilities with earned charges refill them from what their meter gains in combat.
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAbilities : MonoBehaviour
    {
        [SerializeField] private Ability[] _slots = new Ability[4];
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Transform _cameraTransform;

        // Raised when an ability fires, e.g. so a melee combo can be held open across it.
        public event System.Action<Ability> AbilityUsed;
        // Raised when an ability is activated: its cast begins, or it fires if instant.
        // The hands are busy from here, so a reload in progress is cut off.
        public event System.Action<Ability> AbilityStarted;

        // Read by AbilityHUD
        public Ability[] Slots => _slots;
        public int  CastingSlot => _castingSlot;
        public bool IsCasting   => _castingSlot >= 0;

        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private MeterSet _meters;
        private int[] _charges;
        private float[] _earned;
        private readonly List<(Meter meter, System.Action<float> handler)> _meterHooks = new();
        private InputBuffer[] _buffers;
        private CooldownTimer[] _recharges;
        private AbilityContext _ctx;

        private int   _castingSlot = -1;
        private float _castTimer;

        // Maps slot index to the matching GameAction
        private static readonly GameAction[] SlotActions =
        {
            GameAction.Ability1,
            GameAction.Ability2,
            GameAction.Ability3,
            GameAction.Ability4,
        };

        private void Awake()
        {
            _input     = GetComponent<PlayerInputHandler>();
            _movement  = GetComponent<PlayerMovement>();
            TryGetComponent(out _meters);
            _charges   = new int[_slots.Length];
            _recharges = new CooldownTimer[_slots.Length];
            _earned    = new float[_slots.Length];
            _buffers   = new InputBuffer[_slots.Length];
            for (int i = 0; i < _buffers.Length; i++) _buffers[i] = new InputBuffer();
            RefillCharges();

            _ctx = new AbilityContext
            {
                PlayerTransform  = transform,
                PlayerRigidbody  = GetComponent<Rigidbody>(),
                PlayerCollider   = GetComponent<Collider>(),
                CameraTransform  = _cameraTransform,
                Health           = _health,
                Source           = DamageSource.Of(gameObject),
            };

            if (_health != null) _health.OnRevived += RefillCharges;
        }

        // MeterSet builds its meters in Awake, so the hooks wait for Start.
        private void Start()
        {
            if (_meters == null) return;
            foreach (Meter meter in _meters.Meters)
            {
                Meter m = meter;
                System.Action<float> handler = amount => OnMeterGained(m, amount);
                m.Restored += handler;
                _meterHooks.Add((m, handler));
            }
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= RefillCharges;
            foreach (var (meter, handler) in _meterHooks) meter.Restored -= handler;
        }

        private void OnDisable()
        {
            _castingSlot = -1;
            foreach (InputBuffer buffer in _buffers) buffer.Clear();
        }

        private void Update()
        {
            _ctx.MoveInput = _input.MoveInput;
            TickRecharges(Time.deltaTime);
            BufferPresses();

            if (IsCasting)
            {
                TickCast(Time.deltaTime);
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                Ability ability = _slots[i];
                if (ability == null)                   continue;
                if (!Pressed(i))                       continue;
                if (_charges[i] <= 0)                  continue;
                if (!_movement.CanAct)                 continue;
                if (!ability.Cost.CanAfford(_meters))  continue;
                if (!ability.CanExecute(_ctx))         continue;

                _buffers[i].Clear();
                AbilityStarted?.Invoke(ability);

                if (ability.CastTime > 0f)
                {
                    _castingSlot = i;
                    _castTimer   = ability.CastTime;
                }
                else
                {
                    Fire(i);
                }
                return;
            }
        }

        // Charges available in a slot (0 for an empty slot).
        public int GetCharges(int slot) => _slots[slot] != null ? _charges[slot] : 0;

        // False while the slot's resource cost (e.g. earned Rage) can't be paid.
        public bool CanAfford(int slot) => _slots[slot] != null && _slots[slot].Cost.CanAfford(_meters);

        // 1 when the slot can be used; otherwise how far the next charge has recharged.
        public float GetReadyRatio(int slot)
        {
            if (_charges[slot] > 0) return 1f;
            Ability ability = _slots[slot];
            float earned = ability != null && ability.ChargeSource != ChargeSource.Cooldown
                ? Mathf.Clamp01(_earned[slot] / ability.EarnedPerCharge) : 0f;
            return ability != null && ability.ChargeSource == ChargeSource.Earned
                ? earned : Mathf.Max(earned, _recharges[slot].Ratio);
        }

        // 0..1 progress of the current cast (0 when not casting).
        public float CastProgress
        {
            get
            {
                if (!IsCasting) return 0f;
                float castTime = _slots[_castingSlot].CastTime;
                return castTime > 0f ? 1f - _castTimer / castTime : 1f;
            }
        }

        private void BufferPresses()
        {
            if (!GameSettings.Current.InputBuffering) return;
            for (int i = 0; i < _slots.Length; i++)
                if (_input.GetAction(SlotActions[i])) _buffers[i].Press(Time.time);
        }

        // A press this frame, or (with input buffering) one still waiting. It is only used up
        // when the ability actually goes off, so a press that fails a check can retry.
        private bool Pressed(int slot) =>
            _input.GetAction(SlotActions[slot]) || _buffers[slot].IsPending(Time.time);

        private void TickCast(float deltaTime)
        {
            if (!_movement.CanAct)
            {
                _castingSlot = -1;
                return;
            }

            _castTimer -= deltaTime;
            if (_castTimer > 0f) return;

            int slot = _castingSlot;
            _castingSlot = -1;
            Ability ability = _slots[slot];
            if (ability.Cost.CanAfford(_meters) && ability.CanExecute(_ctx)) Fire(slot);
        }

        private void Fire(int slot)
        {
            Ability ability = _slots[slot];
            if (!TryPayCost(ability, out float spent)) return;

            _ctx.Power        = ResourceSpend.Power(spent, ability.Cost.Amount, ability.Scaling.MaxSpend, ability.Scaling.MaxPower);
            _ctx.BonusEffects = ability.Scaling.IsActive(ability.Cost) ? ability.Scaling.EffectsFor(spent) : null;
            ability.Execute(_ctx);
            AbilityUsed?.Invoke(ability);

            bool wasFull = _charges[slot] >= ability.MaxCharges;
            _charges[slot]--;
            if (wasFull && ability.ChargeSource != ChargeSource.Earned) _recharges[slot].Start(ability.Cooldown);
        }

        // A scaled ability spends everything available up to its MaxSpend; others pay the cost.
        private bool TryPayCost(Ability ability, out float spent)
        {
            spent = ability.Cost.Amount;
            if (!ability.Scaling.IsActive(ability.Cost)) return ability.Cost.TryPay(_meters);
            if (_meters == null || !_meters.TryGet(ability.Cost.Meter, out Meter meter)) return false;

            spent = ResourceSpend.Amount(meter.Current, ability.Cost.Amount, ability.Scaling.MaxSpend);
            return meter.TrySpend(spent);
        }

        // Earned charges: what a meter gains in combat loads the charges of abilities tied to it.
        private void OnMeterGained(Meter meter, float amount)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Ability ability = _slots[i];
                if (ability == null || ability.ChargeSource == ChargeSource.Cooldown || ability.ChargeMeter == null) continue;
                if (_charges[i] >= ability.MaxCharges) continue;
                if (!_meters.TryGet(ability.ChargeMeter, out Meter chargeMeter) || chargeMeter != meter) continue;

                _earned[i] += amount;
                while (_earned[i] >= ability.EarnedPerCharge && _charges[i] < ability.MaxCharges)
                {
                    _earned[i] -= ability.EarnedPerCharge;
                    _charges[i]++;
                }
                if (_charges[i] >= ability.MaxCharges) _earned[i] = 0f;
            }
        }

        private void TickRecharges(float deltaTime)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Ability ability = _slots[i];
                if (ability == null || ability.ChargeSource == ChargeSource.Earned || _charges[i] >= ability.MaxCharges) continue;

                _recharges[i].Tick(deltaTime);
                if (!_recharges[i].IsReady) continue;

                _charges[i]++;
                if (_charges[i] < ability.MaxCharges)
                    _recharges[i].Start(ability.Cooldown);
            }
        }

        private void RefillCharges()
        {
            _castingSlot = -1;
            for (int i = 0; i < _slots.Length; i++)
            {
                // Earned-only charges start empty: they have to be fought for.
                Ability ability = _slots[i];
                _charges[i] = ability == null || ability.ChargeSource == ChargeSource.Earned ? 0 : ability.MaxCharges;
                _recharges[i].Reset();
                _earned[i]  = 0f;
            }
        }
    }
}
