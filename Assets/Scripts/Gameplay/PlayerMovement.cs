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

    public bool IsGrounded  => _isGrounded;
    public bool IsCrouching => _isCrouching;
    public bool IsSprinting => _isSprinting;
    public bool IsSliding   => _isSliding;
    public bool IsMantling  => _mantle.IsMantling;
    public Vector3 Velocity => _rb.linearVelocity;

    public Vector3 MoveDirection   => _moveDirection;
    public float CoyoteTimer       => _coyoteTimer;
    public Transform CameraTransform => _cameraTransform;

    private Rigidbody _rb;
    private CapsuleCollider _col;
    private PlayerInputHandler _input;
    private PlayerDodge _dodge;
    private PlayerMantle _mantle;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _mantleBufferTimer;
    private bool _slideQueued;
    private float _slideTimer;
    private Vector3 _slideDirection;
    private Vector3 _moveDirection;
    private Rigidbody _groundRb;

    private void Awake()
    {
        _rb     = GetComponent<Rigidbody>();
        _col    = GetComponent<CapsuleCollider>();
        _input  = GetComponent<PlayerInputHandler>();
        _dodge  = GetComponent<PlayerDodge>();
        _mantle = GetComponent<PlayerMantle>();

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
        if (_input.GetAction(GameAction.Jump))
        {
            _jumpBufferTimer = _settings.JumpBufferTime;
            if (!IsGrounded)
                _mantleBufferTimer = _settings.MantleBufferTime;
        }

        _jumpBufferTimer -= Time.deltaTime;

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
        _mantle.Tick();

        if (!IsMantling)
        {
            HandleSlide();
            HandleMovement();
            _dodge.Tick();
            HandleJump();

            _mantleBufferTimer -= Time.fixedDeltaTime;
            if (_mantleBufferTimer > 0f && _coyoteTimer <= 0f)
            {
                _mantle.TryInit();
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
        if (_dodge.CurrentDodgePhase == PlayerDodge.DodgePhase.Roll && _dodge.LockDodgeDirection)
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

    public void AddImpulse(Vector3 force) => _rb.AddForce(force, ForceMode.Impulse);
}
