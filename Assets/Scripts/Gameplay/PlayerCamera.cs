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
    [SerializeField] private float _mouseSensitivity = 1.5f;
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

    [Header("ADS")]
    [SerializeField] private float _adsFOV = 45f;
    [SerializeField] private float _adsTpDistance = 1.5f;
    [SerializeField] private float _adsShoulderOffset = 0f;
    [SerializeField] private float _adsSensitivityMult = 0.5f;
    [SerializeField] private float _adsSpeed = 10f;

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
    private float _adsT;

    public bool IsAiming { get; private set; }

    public float MouseSensitivity   => _mouseSensitivity;
    public float GamepadSensitivity => _gamepadSensitivity;

    public void SetSensitivity(float mouse, float gamepad)
    {
        _mouseSensitivity   = mouse;
        _gamepadSensitivity = gamepad;
    }

    private void Awake()
    {
        SettingsSave.LoadSensitivity(out _mouseSensitivity, out _gamepadSensitivity);

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
        HandleADS();
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

    private void HandleADS()
    {
        IsAiming = _input.GetAction(GameAction.AimDownSights);
        float target = IsAiming ? 1f : 0f;
        _adsT = Mathf.MoveTowards(_adsT, target, _adsSpeed * Time.deltaTime);
    }

    private void UpdateRotation()
    {
        Vector2 look = _input.LookInput;
        float sensScale = Mathf.Lerp(1f, _adsSensitivityMult, _adsT);
        float mult = (_input.IsGamepadLook ? _gamepadSensitivity * Time.deltaTime : _mouseSensitivity * 0.1f) * sensScale;

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

        float activeDistance = Mathf.Lerp(_tpDistance, _adsTpDistance, _adsT);
        float activeShoulder = Mathf.Lerp(_shoulderOffset, _adsShoulderOffset, _adsT);

        // Collision: cast from head along -forward to find safe TP distance
        Vector3 back = rotation * Vector3.back;
        float safeDistance = activeDistance;
        if (Physics.SphereCast(_headAnchor.position, _collisionRadius, back,
            out RaycastHit hit, activeDistance, _collisionMask, QueryTriggerInteraction.Ignore))
        {
            safeDistance = Mathf.Max(hit.distance - _collisionRadius, _tpMinDistance);
        }

        // Shoulder offset interpolates with transition so it doesn't affect FPS aim
        float shoulder = activeShoulder * _transitionT;
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
        float hipFOV = _movement.IsSprinting ? _sprintFOV : _baseFOV;
        float target = Mathf.Lerp(hipFOV, _adsFOV, _adsT);
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
