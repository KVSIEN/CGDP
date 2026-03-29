using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public enum CameraMode { FirstPerson, ThirdPerson }

    [SerializeField] private PlayerInputHandler _input;
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private Transform _playerBody;
    [SerializeField] private Transform _headAnchor;
    [SerializeField] private CameraMode _startingMode = CameraMode.ThirdPerson;

    [Header("Sensitivity")]
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _gamepadSensitivity = 120f;

    [Header("Pitch Limits")]
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;

    [Header("Rotation Smoothing")]
    [SerializeField] private float _rotationSmoothing = 0.05f;

    [Header("Third Person")]
    [SerializeField] private float _tpDistance = 4f;
    [SerializeField] private float _tpMinDistance = 0.5f;
    [SerializeField] private float _shoulderOffset = 0.5f;
    [SerializeField] private float _collisionRadius = 0.2f;
    [SerializeField] private LayerMask _collisionMask = ~0;

    [Header("Transition")]
    [SerializeField] private float _transitionSmoothTime = 0.12f;

    [Header("Body Rotation")]
    [SerializeField] private float _fpBodySmoothTime = 0.02f;
    [SerializeField] private float _tpBodySmoothTime = 0.12f;

    [Header("FOV")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _baseFOV = 70f;
    [SerializeField] private float _sprintFOV = 80f;
    [SerializeField] private float _fovSpeed = 8f;

    [Header("Mesh Visibility")]
    [SerializeField] private Renderer[] _firstPersonHideRenderers;

    public CameraMode ActiveMode => _transitionT < 0.5f ? CameraMode.FirstPerson : CameraMode.ThirdPerson;

    private float _yaw;
    private float _pitch;
    private float _currentYaw;
    private float _currentPitch;
    private float _yawVelocity;
    private float _pitchVelocity;

    private float _transitionT;
    private float _transitionTarget;
    private float _transitionVelocity;

    private float _bodyRotVelocity;

    private void Awake()
    {
        _yaw = _playerBody.eulerAngles.y;
        _currentYaw = _yaw;

        _transitionTarget = _startingMode == CameraMode.FirstPerson ? 0f : 1f;
        _transitionT = _transitionTarget;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RefreshMeshVisibility();
    }

    private void LateUpdate()
    {
        HandleToggleInput();
        UpdateRotation();
        UpdateTransition();
        UpdateCamera();
        UpdateBodyRotation();
        UpdateFOV();
    }

    private void HandleToggleInput()
    {
        if (!_input.WasPressed(GameAction.TogglePerspective)) return;
        _transitionTarget = _transitionTarget < 0.5f ? 1f : 0f;
    }

    private void UpdateRotation()
    {
        Vector2 look = _input.LookInput;
        // Mouse delta is typically small (< 1); gamepad stick axes are normalized to [-1, 1].
        // A magnitude above 1.1 reliably means the input came from a gamepad.
        bool isGamepad = look.magnitude > 1.1f;
        float mult = isGamepad ? _gamepadSensitivity * Time.deltaTime : _mouseSensitivity;

        _yaw += look.x * mult;
        _pitch = Mathf.Clamp(_pitch - look.y * mult, _minPitch, _maxPitch);

        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _yaw, ref _yawVelocity, _rotationSmoothing);
        _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _pitch, ref _pitchVelocity, _rotationSmoothing);
    }

    private void UpdateTransition()
    {
        float prev = _transitionT;
        _transitionT = Mathf.SmoothDamp(_transitionT, _transitionTarget, ref _transitionVelocity, _transitionSmoothTime);

        if (Mathf.Abs(_transitionT - prev) > 0.001f)
            RefreshMeshVisibility();
    }

    private void UpdateCamera()
    {
        // Rotation is identical in both modes — the only thing that changes is position.
        // This guarantees the aim direction (and therefore crosshair aim point) is the
        // same throughout the entire transition.
        Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        transform.rotation = rotation;

        Vector3 fpsPos = _headAnchor.position;

        // Collision: cast from head along -forward to find safe TP distance
        Vector3 back = rotation * Vector3.back;
        float safeDistance = _tpDistance;
        if (Physics.SphereCast(_headAnchor.position, _collisionRadius, back,
            out RaycastHit hit, _tpDistance, _collisionMask, QueryTriggerInteraction.Ignore))
        {
            safeDistance = Mathf.Max(hit.distance - _collisionRadius, _tpMinDistance);
        }

        // Shoulder offset interpolates with transition so it doesn't affect FPS aim
        float shoulder = _shoulderOffset * _transitionT;
        Vector3 tpPos = _headAnchor.position
            + rotation * new Vector3(shoulder, 0f, -safeDistance);

        transform.position = Vector3.Lerp(fpsPos, tpPos, _transitionT);
    }

    private void UpdateBodyRotation()
    {
        float smoothTime = Mathf.Lerp(_fpBodySmoothTime, _tpBodySmoothTime, _transitionT);
        float currentY = _playerBody.eulerAngles.y;
        float newY = Mathf.SmoothDampAngle(currentY, _currentYaw, ref _bodyRotVelocity, smoothTime);
        _playerBody.rotation = Quaternion.Euler(0f, newY, 0f);
    }

    private void UpdateFOV()
    {
        if (_camera == null) return;
        float target = _movement.IsSprinting ? _sprintFOV : _baseFOV;
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, target, _fovSpeed * Time.deltaTime);
    }

    private void RefreshMeshVisibility()
    {
        bool showMesh = _transitionT > 0.35f;
        foreach (var r in _firstPersonHideRenderers)
        {
            if (r != null) r.enabled = showMesh;
        }
    }

    public void SetMode(CameraMode mode, bool instant = false)
    {
        _transitionTarget = mode == CameraMode.FirstPerson ? 0f : 1f;
        if (instant)
        {
            _transitionT = _transitionTarget;
            _transitionVelocity = 0f;
            RefreshMeshVisibility();
        }
    }
}
