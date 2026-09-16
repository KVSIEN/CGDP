using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Player;

namespace CGD.Abilities
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAbilities : MonoBehaviour
    {
        [SerializeField] private Ability[] _slots = new Ability[4];
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Transform _cameraTransform;

        // Read by AbilityHUD to draw cooldown overlays
        public Ability[] Slots => _slots;

        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private CooldownTimer[] _cooldowns;
        private AbilityContext _ctx;

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
            _cooldowns = new CooldownTimer[_slots.Length];

            _ctx = new AbilityContext
            {
                PlayerTransform  = transform,
                PlayerRigidbody  = GetComponent<Rigidbody>(),
                PlayerCollider   = GetComponent<Collider>(),
                CameraTransform  = _cameraTransform,
                Health           = _health,
                Source           = DamageSource.Of(gameObject),
            };

            if (_health != null) _health.OnRevived += ResetCooldowns;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= ResetCooldowns;
        }

        private void Update()
        {
            _ctx.MoveInput = _input.MoveInput;

            for (int i = 0; i < _slots.Length; i++)
            {
                _cooldowns[i].Tick(Time.deltaTime);

                if (_slots[i] == null)                 continue;
                if (!_input.GetAction(SlotActions[i])) continue;
                if (!_cooldowns[i].IsReady)             continue;
                if (!_movement.CanAct)                 continue;

                if (_slots[i].Execute(_ctx))
                    _cooldowns[i].Start(_slots[i].Cooldown);
            }
        }

        // Read by AbilityHUD to size the cooldown overlay.
        public CooldownTimer GetCooldown(int slot) => _cooldowns[slot];

        private void ResetCooldowns()
        {
            for (int i = 0; i < _cooldowns.Length; i++)
                _cooldowns[i].Reset();
        }
    }
}
