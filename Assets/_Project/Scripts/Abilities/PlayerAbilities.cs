using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Player;

namespace CGD.Abilities
{
    // Runs the four ability slots: input, charges and their recharge, and cast times.
    // Only one ability casts at a time; a cast is cancelled when the player can't act.
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAbilities : MonoBehaviour
    {
        [SerializeField] private Ability[] _slots = new Ability[4];
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Transform _cameraTransform;

        // Read by AbilityHUD
        public Ability[] Slots => _slots;
        public int  CastingSlot => _castingSlot;
        public bool IsCasting   => _castingSlot >= 0;

        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private int[] _charges;
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
            _charges   = new int[_slots.Length];
            _recharges = new CooldownTimer[_slots.Length];
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

        private void OnDisable() => _castingSlot = -1;

        private void Update()
        {
            _ctx.MoveInput = _input.MoveInput;
            TickRecharges(Time.deltaTime);

            if (IsCasting)
            {
                TickCast(Time.deltaTime);
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                Ability ability = _slots[i];
                if (ability == null)                   continue;
                if (!_input.GetAction(SlotActions[i])) continue;
                if (_charges[i] <= 0)                  continue;
                if (!_movement.CanAct)                 continue;
                if (!ability.CanExecute(_ctx))         continue;

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

        // 1 when the slot can be used; otherwise how far the next charge has recharged.
        public float GetReadyRatio(int slot) => _charges[slot] > 0 ? 1f : _recharges[slot].Ratio;

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
            if (_slots[slot].CanExecute(_ctx)) Fire(slot);
        }

        private void Fire(int slot)
        {
            Ability ability = _slots[slot];
            ability.Execute(_ctx);

            bool wasFull = _charges[slot] >= ability.MaxCharges;
            _charges[slot]--;
            if (wasFull) _recharges[slot].Start(ability.Cooldown);
        }

        private void TickRecharges(float deltaTime)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Ability ability = _slots[i];
                if (ability == null || _charges[i] >= ability.MaxCharges) continue;

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
                _charges[i] = _slots[i] != null ? _slots[i].MaxCharges : 0;
                _recharges[i].Reset();
            }
        }
    }
}
