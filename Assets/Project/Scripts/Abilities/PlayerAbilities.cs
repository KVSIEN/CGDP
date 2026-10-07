using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Meters;
using CGD.Player;
using CGD.Settings;
using CGD.Items;
using CGD.Stats;

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
        // What an ability executes with: the artifacts held in the offhand cast through the same one.
        public AbilityContext Context => _ctx;
        public int  CastingSlot => _castingSlot;
        public bool IsCasting   => _castingSlot >= 0;

        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private MeterSet _meters;
        private CombatActions _actions;
        private CharacterStats _stats;
        private AbilityState[] _states;
        private InputBuffer[] _buffers;
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
            _stats     = GetComponentInParent<CharacterStats>();
            _states    = new AbilityState[_slots.Length];
            _buffers   = new InputBuffer[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null) _states[i] = new AbilityState(_slots[i]);
                _buffers[i] = new InputBuffer();
            }

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
            TickStates(Time.deltaTime);
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
                if (!_states[i].HasCharge)             continue;
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

        // Tells perks, melee combos and reloads about a cast made outside the four slots (a held tome).
        public void ReportStarted(Ability ability) => AbilityStarted?.Invoke(ability);
        public void ReportUsed(Ability ability)    => AbilityUsed?.Invoke(ability);

        // Charges available in a slot (0 for an empty slot).
        public int GetCharges(int slot) => _states[slot] != null ? _states[slot].Charges : 0;

        // False while the slot's resource or Surge cost can't be paid.
        public bool CanAfford(int slot) => _states[slot] != null && _states[slot].CanAfford(_meters);

        // The slot's Surge gauge (0..1); 0 for abilities without Surge.
        public float GetSurge(int slot) => _states[slot] != null ? _states[slot].Surge : 0f;

        // 1 when the slot can be used; otherwise how far the next charge has recharged.
        public float GetReadyRatio(int slot) => _states[slot] != null ? _states[slot].ReadyRatio : 0f;

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
            AbilityState state = _states[slot];
            if (!state.TryPay(_meters, out float surgeSpent)) return;

            state.Prepare(_ctx, surgeSpent);
            state.Ability.Execute(_ctx);
            AbilityUsed?.Invoke(state.Ability);
            state.Spend(AbilityState.CooldownOf(state.Ability, _stats));
        }

        // Each combat action fills every Surge ability's gauge by its own rate; a full gauge
        // becomes a charge for abilities charged by Surge.
        private void OnCombatAction()
        {
            foreach (AbilityState state in _states)
                state?.OnCombatAction(Time.time);
        }

        private void TickStates(float deltaTime)
        {
            foreach (AbilityState state in _states)
                state?.Tick(deltaTime, Time.time, AbilityState.CooldownOf(state.Ability, _stats));
        }

        private void RefillCharges()
        {
            _castingSlot = -1;
            foreach (AbilityState state in _states)
                state?.Reset();
        }
    }
}
