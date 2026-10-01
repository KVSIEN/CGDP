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

        [SerializeField] private DodgeDefinition _definition;

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
            _motion.Ended += cooldown => _cooldown.Start(cooldown);
        }

        private void OnDisable()
        {
            _motion.Cancel();
            SetInvulnerable(false);
        }

        private void Update()
        {
            if (!_input.GetAction(GameAction.Dodge)) return;

            if (_motion.IsActive) _motion.PressDodge();
            else _buffered = InputBuffer;
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
                _buffered = TryStart() ? 0f : _buffered - dt;
            }

            if (_motion.Tick(dt, Flat(_movement.MoveDirection), out Vector2 velocity))
            {
                float y = _motion.Stage.IgnoreGravity ? 0f : _rb.linearVelocity.y;
                _rb.linearVelocity = new Vector3(velocity.x, y, velocity.y);
            }
            SetInvulnerable(_motion.IsInvulnerable);
        }

        private bool TryStart()
        {
            if (_motion.IsActive || _definition == null || _definition.Stages.Count == 0) return false;
            if (!_cooldown.IsReady) return false;

            bool grounded = _movement.IsGrounded;
            if (!grounded && _airDodgesUsed >= _definition.AirDodges) return false;
            if (!TryGetDirection(out Vector2 direction)) return false;
            if (!_definition.Cost.TryPay(_meters)) return false;

            if (!grounded) _airDodgesUsed++;
            _motion.Start(_definition.Stages, direction);
            return true;
        }

        private bool TryGetDirection(out Vector2 direction)
        {
            direction = Flat(_movement.MoveDirection);
            if (direction.sqrMagnitude > InputDeadzone * InputDeadzone) return true;

            Vector2 forward = Flat(_movement.CameraTransform.forward).normalized;
            switch (_definition.WithoutInput)
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
