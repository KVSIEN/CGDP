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
    // Surge abilities (uncommon) each have their own gauge, filled by the player's combat
    // actions and draining per the ability's settings: it can pay a Surge cost, be spent in
    // bulk for a stronger cast (scaling), or turn into a charge each time it fills.
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
        private CombatActions _actions;
        private int[] _charges;
        private SurgeGauge[] _surges;
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
            TryGetComponent(out _actions);
            _charges   = new int[_slots.Length];
            _recharges = new CooldownTimer[_slots.Length];
            _surges    = new SurgeGauge[_slots.Length];
            _buffers   = new InputBuffer[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _surges[i]  = new SurgeGauge();
                _buffers[i] = new InputBuffer();
            }
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

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= RefillCharges;
        }

        private void OnEnable()
        {
            if (_actions != null) _actions.Performed += OnCombatAction;
        }

        private void OnDisable()
        {
            if (_actions != null) _actions.Performed -= OnCombatAction;
            _castingSlot = -1;
            foreach (InputBuffer buffer in _buffers) buffer.Clear();
        }

        private void Update()
        {
            _ctx.MoveInput = _input.MoveInput;
            TickRecharges(Time.deltaTime);
            TickSurges(Time.deltaTime);
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
                if (!CanAfford(i))                     continue;
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

        // False while the slot's resource or Surge cost can't be paid.
        public bool CanAfford(int slot)
        {
            Ability ability = _slots[slot];
            return ability != null && ability.Cost.CanAfford(_meters) && _surges[slot].Has(ability.SurgeCost);
        }

        // The slot's Surge gauge (0..1); 0 for abilities without Surge.
        public float GetSurge(int slot) => _slots[slot] != null && _slots[slot].Surge.IsActive ? _surges[slot].Value : 0f;

        // 1 when the slot can be used; otherwise how far the next charge has recharged.
        public float GetReadyRatio(int slot)
        {
            if (_charges[slot] > 0) return 1f;
            Ability ability = _slots[slot];
            if (ability == null) return 0f;
            return ability.ChargeSource switch
            {
                ChargeSource.Surge => _surges[slot].Value,
                ChargeSource.Both  => Mathf.Max(_surges[slot].Value, _recharges[slot].Ratio),
                _                  => _recharges[slot].Ratio,
            };
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
            if (CanAfford(slot) && ability.CanExecute(_ctx)) Fire(slot);
        }

        private void Fire(int slot)
        {
            Ability ability = _slots[slot];
            if (!TryPayCost(slot, out float surgeSpent)) return;

            _ctx.Power        = ResourceSpend.Power(surgeSpent, ability.SurgeCost, ability.Scaling.MaxSpend, ability.Scaling.MaxPower);
            _ctx.BonusEffects = ability.Scaling.IsActive(ability.SurgeCost) ? ability.Scaling.EffectsFor(surgeSpent) : null;
            ability.Execute(_ctx);
            AbilityUsed?.Invoke(ability);

            bool wasFull = _charges[slot] >= ability.MaxCharges;
            _charges[slot]--;
            if (wasFull && ability.ChargeSource != ChargeSource.Surge) _recharges[slot].Start(ability.Cooldown);
        }

        // Pays the meter cost (mana…) and the Surge cost. A Surge-scaled ability spends all its
        // Surge up to MaxSpend; `surgeSpent` is what it took.
        private bool TryPayCost(int slot, out float surgeSpent)
        {
            Ability ability = _slots[slot];
            SurgeGauge surge = _surges[slot];
            surgeSpent = ability.Scaling.IsActive(ability.SurgeCost)
                ? ResourceSpend.Amount(surge.Value, ability.SurgeCost, ability.Scaling.MaxSpend)
                : ability.SurgeCost;

            if (!surge.Has(surgeSpent) || !ability.Cost.TryPay(_meters)) return false;
            surge.TrySpend(surgeSpent);
            return true;
        }

        // Each combat action fills every Surge ability's gauge by its own rate; a full gauge
        // becomes a charge for abilities charged by Surge.
        private void OnCombatAction()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Ability ability = _slots[i];
                if (ability == null || !ability.Surge.IsActive) continue;

                _surges[i].Gain(ability.Surge.GainPerAction, Time.time);
                if (ability.ChargeSource != ChargeSource.Cooldown && _charges[i] < ability.MaxCharges && _surges[i].TrySpend(1f))
                    _charges[i]++;
            }
        }

        private void TickSurges(float deltaTime)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null && _slots[i].Surge.IsActive) _surges[i].Tick(deltaTime, Time.time, _slots[i].Surge);
        }

        private void TickRecharges(float deltaTime)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Ability ability = _slots[i];
                if (ability == null || ability.ChargeSource == ChargeSource.Surge || _charges[i] >= ability.MaxCharges) continue;

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
                // Surge-only charges start empty, like every Surge gauge: they have to be fought for.
                Ability ability = _slots[i];
                _charges[i] = ability == null || ability.ChargeSource == ChargeSource.Surge ? 0 : ability.MaxCharges;
                _recharges[i].Reset();
                _surges[i].Reset();
            }
        }
    }
}
