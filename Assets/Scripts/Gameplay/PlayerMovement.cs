using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerMovementSettings _settings;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _playerMesh;

    public bool IsGrounded { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsMantling { get; private set; }
    public Vector3 Velocity => _rb.linearVelocity;

    [Header("Debug")]
    [SerializeField] private bool _isGrounded;
    [SerializeField] private bool _isCrouching;
    [SerializeField] private bool _isSprinting;
    [SerializeField] private bool _isSliding;
    [SerializeField] private bool _isMantling;

    private Rigidbody _rb;
    private CapsuleCollider _col;
    private PlayerInputHandler _input;

    public enum DodgePhase { None, Sidestep, Roll }

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _dodgeCooldownTimer;
    private float _dodgeCooldownMax;
    private DodgePhase _dodgePhase;
    private float _dodgePhaseTimer;
    private Vector3 _dodgeDir;
    private bool _dodgeQueued;
    private bool _rollQueued;
    private bool _slideQueued;
    private float _slideTimer;
    private Vector3 _slideDirection;
    private Vector3 _moveDirection;
    private Rigidbody _groundRb;
    private bool _mantleQueued;
    private Vector3 _mantleTarget;
    private float _mantleTimer;

    [SerializeField] private bool _lockDodgeDirection;

    public float DodgeReadyRatio =>
        _dodgePhase != DodgePhase.None ? 0f :
        _dodgeCooldownTimer <= 0f      ? 1f :
        1f - _dodgeCooldownTimer / _dodgeCooldownMax;

    public DodgePhase CurrentDodgePhase => _dodgePhase;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>();
        _input = GetComponent<PlayerInputHandler>();

        _rb.useGravity = false;
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        SetColliderHeight(_settings.StandHeight);
    }

    private void Update()
    {
        if (_input.WasPressed(GameAction.Jump))
        {
            _jumpBufferTimer = _settings.JumpBufferTime;
            if (!IsGrounded)
                _mantleQueued = true;
        }

        _jumpBufferTimer -= Time.deltaTime;

        if (_input.WasPressed(GameAction.Dodge))
        {
            if (_dodgePhase == DodgePhase.None && _dodgeCooldownTimer <= 0f)
                _dodgeQueued = true;
            else if (_dodgePhase == DodgePhase.Sidestep)
                _rollQueued = true;
        }

        if (_input.WasPressed(GameAction.Crouch) && IsGrounded)
        {
            Vector3 hv = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            if (hv.magnitude >= _settings.SprintSpeed * 0.75f)
                _slideQueued = true;
        }
    }

    private void FixedUpdate()
    {
        HandleCrouch();
        CheckGround();
        HandleMantle();

        if (!IsMantling)
        {
            HandleSlide();
            HandleMovement();
            HandleDodge();
            HandleJump();

            if (_mantleQueued && _coyoteTimer <= 0f)
                TryInitMantle();

            ApplyGravity();
            ClampFallSpeed();
        }

        _mantleQueued = false;

        _isGrounded  = IsGrounded;
        _isCrouching = IsCrouching;
        _isSprinting = IsSprinting;
        _isSliding   = IsSliding;
        _isMantling  = IsMantling;
    }

    private void CheckGround()
    {
        // Cast from the capsule centre so the sphere starts well above the ground.
        // SphereCast silently returns false when the sphere already overlaps a collider
        // at its origin, which happened when we cast from near the feet.
        Vector3 capsuleCenter = transform.position + _col.center;
        float castRadius      = _col.radius * 0.9f;
        float castDistance    = _col.height * 0.5f - castRadius + _settings.GroundCheckDistance;

        IsGrounded = Physics.SphereCast(capsuleCenter, castRadius, Vector3.down, out RaycastHit hit,
            castDistance, _settings.GroundMask, QueryTriggerInteraction.Ignore);

        if (IsGrounded)
        {
            _coyoteTimer = _settings.CoyoteTime;
            _groundRb = hit.rigidbody;
        }
        else
        {
            _coyoteTimer -= Time.fixedDeltaTime;
            _groundRb = null;
        }
    }

    private void HandleSlide()
    {
        if (_slideQueued && IsGrounded)
        {
            IsSliding = true;
            _slideTimer = _settings.SlideDuration;
            Vector3 horiz = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _slideDirection = horiz.magnitude > 0.1f
                ? horiz.normalized
                : -Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            float boostedSpeed = Mathf.Max(horiz.magnitude, _settings.SprintSpeed) + _settings.SlideBoost;
            _rb.linearVelocity = new Vector3(
                _slideDirection.x * boostedSpeed,
                _rb.linearVelocity.y,
                _slideDirection.z * boostedSpeed);
        }
        _slideQueued = false;

        if (!IsSliding) return;

        _slideTimer -= Time.fixedDeltaTime;

        Vector3 current = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (_slideTimer <= 0f
            || current.magnitude < _settings.SlideMinSpeed
            || !_input.GetAction(GameAction.Crouch)
            || !IsGrounded)
        {
            IsSliding = false;
        }
    }

    private void HandleMovement()
    {
        if (_dodgePhase == DodgePhase.Roll && _lockDodgeDirection)
            return;

        Vector2 rawInput = _input.MoveInput;
        IsSprinting = _input.GetAction(GameAction.Sprint) && rawInput.magnitude > 0.1f && !IsCrouching && IsGrounded && !IsSliding;

        if (IsSliding)
        {
            Vector3 slidePlatformVel = _groundRb != null
                ? new Vector3(_groundRb.linearVelocity.x, 0f, _groundRb.linearVelocity.z)
                : Vector3.zero;
            Vector3 slideHorizontal = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z) - slidePlatformVel;
            float slideT = 1f - Mathf.Exp(-_settings.SlideDeceleration * Time.fixedDeltaTime);
            Vector3 slideNewHorizontal = Vector3.Lerp(slideHorizontal, Vector3.zero, slideT) + slidePlatformVel;
            _rb.linearVelocity = new Vector3(slideNewHorizontal.x, _rb.linearVelocity.y, slideNewHorizontal.z);
            return;
        }

        float targetSpeed = IsCrouching ? _settings.CrouchSpeed
            : IsSprinting ? _settings.SprintSpeed
            : _settings.WalkSpeed;

        _moveDirection = Vector3.zero;
        if (rawInput.magnitude > 0.01f)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized;
            _moveDirection = (camForward * rawInput.y + camRight * rawInput.x).normalized;
        }

        Vector3 targetDir = _moveDirection;

        if (IsGrounded && Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down,
            out RaycastHit slopeHit, 0.4f, _settings.GroundMask, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (angle > 1f && angle < _settings.MaxSlopeAngle)
                targetDir = Vector3.ProjectOnPlane(_moveDirection, slopeHit.normal).normalized;
        }

        Vector3 platformVel = _groundRb != null
            ? new Vector3(_groundRb.linearVelocity.x, 0f, _groundRb.linearVelocity.z)
            : Vector3.zero;

        Vector3 currentHorizontal = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z) - platformVel;
        Vector3 targetHorizontal = targetDir * targetSpeed;

        float accel;
        if (!IsGrounded)
            accel = _settings.Acceleration * _settings.AirControl;
        else if (_moveDirection.magnitude > 0.01f)
            accel = _settings.Acceleration;
        else
            accel = _settings.Deceleration;

        float t = 1f - Mathf.Exp(-accel * Time.fixedDeltaTime);
        Vector3 newHorizontal = Vector3.Lerp(currentHorizontal, targetHorizontal, t) + platformVel;

        _rb.linearVelocity = new Vector3(newHorizontal.x, _rb.linearVelocity.y, newHorizontal.z);

        if (IsGrounded && _moveDirection.magnitude > 0.1f)
            TryClimbStep();
    }

    private void TryClimbStep()
    {
        Vector3 lower = transform.position + Vector3.up * 0.05f;
        if (!Physics.Raycast(lower, _moveDirection, _settings.StepCheckDistance, _settings.GroundMask))
            return;

        Vector3 upper = transform.position + Vector3.up * (_settings.MaxStepHeight + 0.05f);
        if (Physics.Raycast(upper, _moveDirection, _settings.StepCheckDistance, _settings.GroundMask))
            return;

        _rb.position = Vector3.MoveTowards(_rb.position,
            _rb.position + Vector3.up * _settings.MaxStepHeight,
            _settings.StepClimbSpeed * Time.fixedDeltaTime);
    }

    private void HandleMantle()
    {
        if (!IsMantling) return;

        _mantleTimer -= Time.fixedDeltaTime;

        float dist = Vector3.Distance(_rb.position, _mantleTarget);
        if (dist < 0.08f || _mantleTimer <= 0f)
        {
            _rb.MovePosition(_mantleTarget);
            _rb.linearVelocity = Vector3.zero;
            IsMantling = false;
            return;
        }

        _rb.MovePosition(Vector3.MoveTowards(_rb.position, _mantleTarget, _settings.MantleSpeed * Time.fixedDeltaTime));
        _rb.linearVelocity = Vector3.zero;
    }

    private void TryInitMantle()
    {
        Vector3 forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        Vector3 chestOrigin = transform.position + Vector3.up * _settings.MantleDetectHeight;

        if (!Physics.Raycast(chestOrigin, forward, out RaycastHit wallHit,
                _settings.MantleReach, _settings.GroundMask, QueryTriggerInteraction.Ignore))
            return;

        // Cast down from above the wall hit point to find the ledge top surface.
        float castTopY = transform.position.y + _settings.MantleMaxHeight + 0.2f;
        Vector3 castFrom = new Vector3(
            wallHit.point.x + forward.x * 0.05f,
            castTopY,
            wallHit.point.z + forward.z * 0.05f);

        if (!Physics.Raycast(castFrom, Vector3.down, out RaycastHit ledgeHit,
                _settings.MantleMaxHeight + 0.5f, _settings.GroundMask, QueryTriggerInteraction.Ignore))
            return;

        float relativeHeight = ledgeHit.point.y - transform.position.y;
        if (relativeHeight < 0f) return;

        if (relativeHeight <= _settings.VaultMaxHeight)
        {
            // Low vault: boost velocity up and forward.
            Vector3 vel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(
                forward.x * _settings.VaultForwardImpulse,
                Mathf.Max(vel.y, _settings.VaultUpImpulse),
                forward.z * _settings.VaultForwardImpulse);
            return;
        }

        if (relativeHeight <= _settings.MantleMaxHeight)
        {
            // High mantle: kinematically move player to stand on the ledge.
            _mantleTarget = new Vector3(
                wallHit.point.x + forward.x * _settings.MantleStepOver,
                ledgeHit.point.y,
                wallHit.point.z + forward.z * _settings.MantleStepOver);
            IsMantling = true;
            _mantleTimer = _settings.MantleTimeout;
            _rb.linearVelocity = Vector3.zero;
        }
    }

    private void HandleJump()
    {
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            float v = Mathf.Sqrt(2f * _settings.JumpHeight * -Physics.gravity.y * _settings.GravityScale);
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, v, _rb.linearVelocity.z);
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
        }
    }

    private void ApplyGravity()
    {
        if (IsGrounded)
        {
            // Keep a small constant downward velocity so the player stays pressed
            // onto slopes. Only applied when already moving down so jumps are unaffected.
            if (_rb.linearVelocity.y < 0f)
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -2f, _rb.linearVelocity.z);
            return;
        }

        _rb.AddForce(Physics.gravity * GetGravityScale() * _rb.mass, ForceMode.Force);
    }

    // Picks a gravity multiplier based on what the player is doing mid-air.
    // Heavier fall, lighter short-hop, or neutral rise all use different scales.
    private float GetGravityScale()
    {
        if (_rb.linearVelocity.y < 0f)
            return _settings.FallGravityScale;
        if (!_input.IsHeld(GameAction.Jump) && _rb.linearVelocity.y > 0f)
            return _settings.LowJumpGravityScale;
        return _settings.GravityScale;
    }

    private void ClampFallSpeed()
    {
        if (_rb.linearVelocity.y < -_settings.MaxFallSpeed)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -_settings.MaxFallSpeed, _rb.linearVelocity.z);
    }

    private void HandleCrouch()
    {
        bool want = IsSliding || _input.GetAction(GameAction.Crouch);

        if (want && !IsCrouching)
            IsCrouching = true;
        else if (!want && IsCrouching && CanStandUp())
            IsCrouching = false;

        float targetHeight = IsCrouching ? _settings.CrouchHeight : _settings.StandHeight;
        float current = _col.height;
        float next = Mathf.MoveTowards(current, targetHeight, _settings.CrouchTransitionSpeed * Time.fixedDeltaTime);

        if (Mathf.Approximately(next, current)) return;

        SetColliderHeight(next);
    }

    private bool CanStandUp()
    {
        float radius = _col.radius * 0.9f;
        float checkDist = _settings.StandHeight - _settings.CrouchHeight - radius;
        Vector3 origin = transform.position + Vector3.up * (_settings.CrouchHeight + radius);
        return !Physics.SphereCast(origin, radius, Vector3.up, out _, checkDist,
            _settings.GroundMask, QueryTriggerInteraction.Ignore);
    }

    private void SetColliderHeight(float height)
    {
        _col.height = height;
        _col.center = Vector3.up * (height * 0.5f);

        if (_playerMesh == null) return;
        // Reposition and squish the mesh so it always sits on the ground (y=0)
        // and matches the collider height visually.
        _playerMesh.localPosition = new Vector3(0f, height * 0.5f, 0f);
        _playerMesh.localScale    = new Vector3(1f, height / _settings.StandHeight, 1f);
    }

    private void HandleDodge()
    {
        _dodgeCooldownTimer = Mathf.Max(_dodgeCooldownTimer - Time.fixedDeltaTime, 0f);

        // Phase 1 — sidestep: capture direction and start the window
        if (_dodgeQueued)
        {
            _dodgeQueued = false;
            _dodgeDir = _moveDirection.magnitude > 0.1f
                ? _moveDirection
                : -Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
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
                if (_moveDirection.magnitude > 0.1f)
                    _dodgeDir = _moveDirection;
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
            if (!_lockDodgeDirection && _moveDirection.magnitude > 0.1f)
                _dodgeDir = _moveDirection;

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

    public void AddImpulse(Vector3 force) => _rb.AddForce(force, ForceMode.Impulse);
}
