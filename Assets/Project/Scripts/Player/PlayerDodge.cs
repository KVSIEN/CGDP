using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Meters;

namespace CGD.Player
{
    // Performs the player's dodge as described by a DodgeDefinition (sidestep into roll,
    // committed roll, steerable boost, long dash…). DodgeMotion runs the stages; this
    // component reads input, starts dodges when allowed, applies their velocity and
    // i-frames, and cancels them when the player is stunned or mantles.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInputHandler))]
    // Runs after PlayerMovement so the dodge velocity overrides whatever movement set this step.
    [DefaultExecutionOrder(100)]
    public class PlayerDodge : MonoBehaviour
    {
        // A press this soon before the dodge is ready (cooldown, landing) still dodges.
        private const float InputBuffer = 0.15f;
        private const float InputDeadzone = 0.1f;
        private const float DoubleTapWindow = 0.3f;

        [SerializeField] private DodgeDefinition _definition;
        [Tooltip("Also dodge by double-tapping Crouch while moving")]
        [SerializeField] private bool _doubleTapCrouch;

        private readonly DodgeMotion _motion = new();
        private PlayerMovement _movement;
        private Rigidbody _rb;
        private PlayerInputHandler _input;
        private MeterSet _meters;
        private HealthManager _health;

        private CooldownTimer _cooldown;
        private float _buffered;
        private int _airDodgesUsed;
        private bool _invulnerable;
        // The running move came from an ability: its cooldown belongs to the ability, not the dodge.
        private bool _fromAbility;
        private float _lastCrouchTap = float.NegativeInfinity;

        // The player's own dodge starting (not an ability's dodge-style move), e.g. for weapon perks.
        public event System.Action Dodged;

        // Read by DodgeHUD to size the cooldown overlay.
        public CooldownTimer Cooldown => _cooldown;

        // Swappable at runtime (perks, gear, the dev console); cancels a dodge in progress.
        public DodgeDefinition Definition
        {
            get => _definition;
            set
            {
                _motion.Cancel();
                _definition = value;
            }
        }

        public bool IsDodging            => _motion.IsActive;
        public bool IsDrivingMovement    => _motion.IsMoving;
        public bool IsCommitted          => _motion.IsCommitted;
        public bool IsWaitingForFollowUp => _motion.IsWaitingForFollowUp;
        public bool IsRolling            => _motion.Pose == DodgePose.Roll;
        public bool IsDashing            => _motion.Pose == DodgePose.Dash;

        // The running stage's label, or the dodge's name when idle.
        public string Label => _motion.IsActive ? _motion.Stage.Label
                             : _definition != null ? _definition.DisplayName : string.Empty;

        private void Awake()
        {
            _rb       = GetComponent<Rigidbody>();
            _movement = GetComponent<PlayerMovement>();
            _input    = GetComponent<PlayerInputHandler>();
            TryGetComponent(out _meters);
            TryGetComponent(out _health);
            _motion.Ended += cooldown =>
            {
                if (!_fromAbility) _cooldown.Start(cooldown);
            };
        }

        private void OnDisable()
        {
            _motion.Cancel();
            SetInvulnerable(false);
        }

        private void Update()
        {
            if (_input.GetAction(GameAction.Dodge) || DoubleTappedCrouch()) PressDodge();
        }

        private void PressDodge()
        {
            if (_motion.IsActive) _motion.PressDodge();
            else _buffered = InputBuffer;
        }

        // Two Crouch presses in quick succession with a direction held. Crouch toggles, so
        // the pair leaves the stance as it was.
        private bool DoubleTappedCrouch()
        {
            if (!_doubleTapCrouch || !_input.WasPressed(GameAction.Crouch)) return false;

            bool doubleTap = Time.time - _lastCrouchTap <= DoubleTapWindow
                          && _input.MoveInput.sqrMagnitude > InputDeadzone * InputDeadzone;
            _lastCrouchTap = doubleTap ? float.NegativeInfinity : Time.time;
            return doubleTap;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_movement.IsGrounded) _airDodgesUsed = 0;

            if (_movement.IsMantling || _movement.IsStunned)
            {
                _motion.Cancel();
                _buffered = 0f;
                SetInvulnerable(false);
                return;
            }

            _cooldown.Tick(dt);
            if (_buffered > 0f)
            {
                _buffered = TryStartDodge() ? 0f : _buffered - dt;
            }

            if (_motion.Tick(dt, Flat(_movement.MoveDirection), out Vector2 velocity))
            {
                float y = _motion.Stage.IgnoreGravity ? 0f : _rb.linearVelocity.y;
                _rb.linearVelocity = new Vector3(velocity.x, y, velocity.y);
            }
            SetInvulnerable(_motion.IsInvulnerable);
        }

        // Whether `move` could start right now, ignoring the dodge's own cooldown and cost —
        // for abilities that perform a dodge-style move (Dash) and keep their own.
        public bool CanPerform(DodgeDefinition move) =>
            CanStart(move) && TryGetDirection(move, out _);

        // Performs `move` for an ability. The air-dodge limit still applies.
        public bool TryPerform(DodgeDefinition move)
        {
            if (!CanStart(move) || !TryGetDirection(move, out Vector2 direction)) return false;
            Begin(move, direction, fromAbility: true);
            return true;
        }

        private bool TryStartDodge()
        {
            if (!CanStart(_definition) || !_cooldown.IsReady) return false;
            if (!TryGetDirection(_definition, out Vector2 direction)) return false;
            if (!_definition.Cost.TryPay(_meters)) return false;

            Begin(_definition, direction, fromAbility: false);
            return true;
        }

        private bool CanStart(DodgeDefinition move)
        {
            if (move == null || move.Stages.Count == 0 || _motion.IsActive) return false;
            if (_movement.IsMantling || _movement.IsStunned) return false;
            return _movement.IsGrounded || _airDodgesUsed < move.AirDodges;
        }

        private void Begin(DodgeDefinition move, Vector2 direction, bool fromAbility)
        {
            if (!_movement.IsGrounded) _airDodgesUsed++;
            _fromAbility = fromAbility;
            _motion.Start(move.Stages, direction);
            if (!fromAbility) Dodged?.Invoke();
        }

        private bool TryGetDirection(DodgeDefinition move, out Vector2 direction)
        {
            direction = Flat(_movement.MoveDirection);
            if (direction.sqrMagnitude > InputDeadzone * InputDeadzone) return true;

            Vector2 forward = Flat(_movement.CameraTransform.forward).normalized;
            switch (move.WithoutInput)
            {
                case DodgeFallbackDirection.Backward: direction = -forward; return true;
                case DodgeFallbackDirection.Forward:  direction = forward;  return true;
                default: return false;
            }
        }

        // Held through HealthManager's counted invulnerability, so it never clears god mode
        // or another source's i-frames.
        private void SetInvulnerable(bool value)
        {
            if (value == _invulnerable || _health == null) return;
            _invulnerable = value;
            if (value) _health.AddInvulnerability();
            else _health.RemoveInvulnerability();
        }

        private static Vector2 Flat(Vector3 v) => new(v.x, v.z);
    }
}
