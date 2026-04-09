using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerDodge : MonoBehaviour
{
    public enum DodgePhase { None, Sidestep, Roll }

    [SerializeField] private PlayerMovementSettings _settings;
    [SerializeField] private bool _lockDodgeDirection;

    public float DodgeReadyRatio =>
        _dodgePhase != DodgePhase.None ? 0f :
        _dodgeCooldownTimer <= 0f      ? 1f :
        1f - _dodgeCooldownTimer / _dodgeCooldownMax;

    public DodgePhase CurrentDodgePhase => _dodgePhase;
    public bool LockDodgeDirection => _lockDodgeDirection;

    private PlayerMovement _movement;
    private Rigidbody _rb;
    private PlayerInputHandler _input;

    private float _dodgeCooldownTimer;
    private float _dodgeCooldownMax;
    private DodgePhase _dodgePhase;
    private float _dodgePhaseTimer;
    private Vector3 _dodgeDir;
    private bool _dodgeQueued;
    private bool _rollQueued;

    private void Awake()
    {
        _rb       = GetComponent<Rigidbody>();
        _movement = GetComponent<PlayerMovement>();
        _input    = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (_input.GetAction(GameAction.Dodge))
        {
            if (_dodgePhase == DodgePhase.None && _dodgeCooldownTimer <= 0f)
                _dodgeQueued = true;
            else if (_dodgePhase == DodgePhase.Sidestep)
                _rollQueued = true;
        }
    }

    public void Tick()
    {
        _dodgeCooldownTimer = Mathf.Max(_dodgeCooldownTimer - Time.fixedDeltaTime, 0f);

        // Phase 1 — sidestep: capture direction and start the window
        if (_dodgeQueued)
        {
            _dodgeQueued = false;
            _dodgeDir = _movement.MoveDirection.magnitude > 0.1f
                ? _movement.MoveDirection
                : -Vector3.ProjectOnPlane(_movement.CameraTransform.forward, Vector3.up).normalized;
            _dodgePhase      = DodgePhase.Sidestep;
            _dodgePhaseTimer = _settings.RollWindowDuration;
        }

        if (_dodgePhase == DodgePhase.None) return;

        _dodgePhaseTimer -= Time.fixedDeltaTime;

        if (_dodgePhase == DodgePhase.Sidestep)
        {
            // Sustain sidestep velocity for SidestepDuration
            float elapsed = _settings.RollWindowDuration - _dodgePhaseTimer;
            if (elapsed < _settings.SidestepDuration)
            {
                _rb.linearVelocity = new Vector3(
                    _dodgeDir.x * _settings.SidestepForce,
                    _rb.linearVelocity.y,
                    _dodgeDir.z * _settings.SidestepForce);
            }

            // Second press within the window → commit to the full roll, re-capture direction from current input
            if (_rollQueued)
            {
                _rollQueued = false;
                if (_movement.MoveDirection.magnitude > 0.1f)
                    _dodgeDir = _movement.MoveDirection;
                _dodgePhase      = DodgePhase.Roll;
                _dodgePhaseTimer = _settings.RollDuration;
                return;
            }

            // Window expired without a roll → short cooldown
            if (_dodgePhaseTimer <= 0f)
            {
                _rollQueued         = false;
                _dodgePhase         = DodgePhase.None;
                _dodgeCooldownTimer = _settings.SidestepCooldown;
                _dodgeCooldownMax   = _settings.SidestepCooldown;
            }
            return;
        }

        // Phase 2 — roll: sustain velocity at DodgeForce for the full duration
        if (_dodgePhase == DodgePhase.Roll)
        {
            if (!_lockDodgeDirection && _movement.MoveDirection.magnitude > 0.1f)
                _dodgeDir = _movement.MoveDirection;

            _rb.linearVelocity = new Vector3(
                _dodgeDir.x * _settings.DodgeForce,
                _rb.linearVelocity.y,
                _dodgeDir.z * _settings.DodgeForce);

            if (_dodgePhaseTimer <= 0f)
            {
                _dodgePhase         = DodgePhase.None;
                _dodgeCooldownTimer = _settings.DodgeCooldown;
                _dodgeCooldownMax   = _settings.DodgeCooldown;
            }
        }
    }
}
