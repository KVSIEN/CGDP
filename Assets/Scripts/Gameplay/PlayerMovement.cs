using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerMovementSettings _settings;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _playerMesh;

    [SerializeField] private bool _isGrounded;
    [SerializeField] private bool _isCrouching;
    [SerializeField] private bool _isSprinting;
    [SerializeField] private bool _isSliding;
    [SerializeField] private bool _isMantling;

    public bool IsGrounded  => _isGrounded;
    public bool IsCrouching => _isCrouching;
    public bool IsSprinting => _isSprinting;
    public bool IsSliding   => _isSliding;
    public bool IsMantling  => _isMantling;
    public Vector3 Velocity => _rb.linearVelocity;

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
    private float _mantleBufferTimer;
    private Vector3 _mantleTarget;
    private Vector3 _mantleLiftTarget;
    private Vector3 _mantleExitVelocity;
    private bool _mantleInLiftPhase;
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

        // Zero-friction material prevents wall/surface contact from dampening
        // vertical velocity (e.g. jump height being reduced while touching a wall).
        _col.material = new PhysicsMaterial("PlayerNoFriction")
        {
            staticFriction  = 0f,
            dynamicFriction = 0f,
            bounciness      = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine   = PhysicsMaterialCombine.Minimum,
        };

        SetColliderHeight(_settings.StandHeight);
    }

    private void Update()
    {
        if (_input.WasPressed(GameAction.Jump))
        {
            _jumpBufferTimer = _settings.JumpBufferTime;
            if (!IsGrounded)
                _mantleBufferTimer = _settings.MantleBufferTime;
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
            Vector3 hv = HorizontalVelocity;
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

            _mantleBufferTimer -= Time.fixedDeltaTime;
            if (_mantleBufferTimer > 0f && _coyoteTimer <= 0f)
            {
                TryInitMantle();
                if (IsMantling) _mantleBufferTimer = 0f;
            }

            ApplyGravity();
            ClampFallSpeed();
        }
    }

    private Vector3 HorizontalVelocity => new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
    private Vector3 VelocityWithY(float y) => new Vector3(_rb.linearVelocity.x, y, _rb.linearVelocity.z);

    private void CheckGround()
    {
        // Cast from the capsule centre so the sphere starts well above the ground.
        // SphereCast silently returns false when the sphere already overlaps a collider
        // at its origin, which happened when we cast from near the feet.
        Vector3 capsuleCenter = transform.position + _col.center;
        float castRadius      = _col.radius * 0.9f;
        float castDistance    = _col.height * 0.5f - castRadius + _settings.GroundCheckDistance;

        _isGrounded = Physics.SphereCast(capsuleCenter, castRadius, Vector3.down, out RaycastHit hit,
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
            _isSliding = true;
            _slideTimer = _settings.SlideDuration;
            Vector3 horiz = HorizontalVelocity;
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

        Vector3 current = HorizontalVelocity;
        if (_slideTimer <= 0f
            || current.magnitude < _settings.SlideMinSpeed
            || !_input.GetAction(GameAction.Crouch)
            || !IsGrounded)
        {
            _isSliding = false;
        }
    }

    private void HandleMovement()
    {
        if (_dodgePhase == DodgePhase.Roll && _lockDodgeDirection)
            return;

        Vector2 rawInput = _input.MoveInput;
        _isSprinting = _input.GetAction(GameAction.Sprint) && rawInput.magnitude > 0.1f && !IsCrouching && IsGrounded && !IsSliding;

        if (IsSliding)
        {
            Vector3 slidePlatformVel = _groundRb != null
                ? new Vector3(_groundRb.linearVelocity.x, 0f, _groundRb.linearVelocity.z)
                : Vector3.zero;
            Vector3 slideHorizontal = HorizontalVelocity - slidePlatformVel;
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

        Vector3 currentHorizontal = HorizontalVelocity - platformVel;
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

        Vector3 target = _mantleInLiftPhase ? _mantleLiftTarget : _mantleTarget;
        float dist     = Vector3.Distance(_rb.position, target);

        // Phase transition: lift complete → begin forward step
        if (_mantleInLiftPhase && dist < 0.08f)
        {
            _mantleInLiftPhase = false;
            return;
        }

        // Done: reached final target or timed out
        if (!_mantleInLiftPhase && (dist < 0.08f || _mantleTimer <= 0f))
        {
            _rb.position       = _mantleTarget;
            _rb.isKinematic    = false;
            _rb.linearVelocity = _mantleExitVelocity;
            _isMantling        = false;
            return;
        }

        // Lift slightly faster so it feels snappy upward
        float speed = _mantleInLiftPhase ? _settings.MantleSpeed * 1.5f : _settings.MantleSpeed;
        _rb.MovePosition(Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime));
    }

    private void TryInitMantle()
    {
        Vector3 forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;

        // Sweep the full mantleable height range so the wall is detected whether
        // the player is at ground level or has already risen near the ledge top.
        Vector3 sweepBase = transform.position + Vector3.up * (_col.radius + 0.05f);
        Vector3 sweepTop  = transform.position + Vector3.up * _settings.MantleMaxHeight;
        if (!Physics.CapsuleCast(sweepBase, sweepTop, _col.radius * 0.4f, forward,
                out RaycastHit wallHit, _settings.MantleReach, _settings.GroundMask, QueryTriggerInteraction.Ignore))
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
            // Low vault: kinematically lift to the ledge top to avoid clipping the edge,
            // then release with forward + upward velocity so the player arcs over.
            _mantleLiftTarget   = new Vector3(_rb.position.x, ledgeHit.point.y, _rb.position.z);
            _mantleTarget       = _mantleLiftTarget;
            _mantleInLiftPhase  = false;
            _mantleExitVelocity = new Vector3(
                forward.x * _settings.VaultForwardImpulse,
                _settings.VaultUpImpulse,
                forward.z * _settings.VaultForwardImpulse);
            _isMantling        = true;
            _mantleTimer       = 0.3f;
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic    = true;
            return;
        }

        if (relativeHeight <= _settings.MantleMaxHeight)
        {
            // High mantle: phase 1 — lift straight up alongside the wall to ledge height
            //              phase 2 — step forward onto the ledge surface.
            // Kinematic during the move prevents physics from pushing the player back
            // through the wall face.
            _mantleLiftTarget  = new Vector3(_rb.position.x, ledgeHit.point.y, _rb.position.z);
            _mantleTarget = new Vector3(
                wallHit.point.x + forward.x * _settings.MantleStepOver,
                ledgeHit.point.y,
                wallHit.point.z + forward.z * _settings.MantleStepOver);
            _mantleInLiftPhase  = true;
            _mantleExitVelocity = Vector3.zero;
            _isMantling        = true;
            _mantleTimer       = _settings.MantleTimeout;
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic    = true;
        }
    }

    private void HandleJump()
    {
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            float v = Mathf.Sqrt(2f * _settings.JumpHeight * -Physics.gravity.y * _settings.GravityScale);
            _rb.linearVelocity = VelocityWithY(v);
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
                _rb.linearVelocity = VelocityWithY(-2f);
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
            _rb.linearVelocity = VelocityWithY(-_settings.MaxFallSpeed);
    }

    private void HandleCrouch()
    {
        bool want = IsSliding || _input.GetAction(GameAction.Crouch);

        if (want && !IsCrouching)
            _isCrouching = true;
        else if (!want && IsCrouching && CanStandUp())
            _isCrouching = false;

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
