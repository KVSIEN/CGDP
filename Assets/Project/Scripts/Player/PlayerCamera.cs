using UnityEngine;
using CGD.Core;
using CGD.Input;
using CGD.Settings;

namespace CGD.Player
{
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

        [Header("Crouch")]
        [SerializeField] private float _crouchHeadLower = 0.65f;
        [SerializeField] private float _crouchHeadSmoothTime = 0.08f;

        [Header("ADS")]
        [Tooltip("Fallback ADS FOV used before a weapon is equipped. WeaponData.AdsFovDeg overrides at runtime via SetAdsProfile().")]
        [SerializeField] private float _adsFOV = 45f;
        [SerializeField] private float _adsTpDistance = 1.5f;
        [Tooltip("Shoulder offset while ADS in third-person. Keep non-zero so the camera stays beside the player, not behind their head.")]
        [SerializeField] private float _adsTpShoulderOffset = 0.25f;
        [SerializeField] private float _adsSensitivityMult = 0.5f;
        [Tooltip("Fallback ADS transition speed used before a weapon is equipped. WeaponData.AdsSpeed overrides at runtime via SetAdsProfile().")]
        [SerializeField] private float _adsSpeed = 10f;

        [Header("FOV")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _baseFOV = 70f;
        [SerializeField] private float _sprintFOV = 80f;
        [SerializeField] private float _fovSpeed = 8f;

        [Header("Lock-On")]
        [Tooltip("Degrees per second the view turns toward a locked target")]
        [SerializeField] private float _lockOnTurnSpeed = 360f;

        [Header("Mesh Visibility")]
        [SerializeField] private Renderer[] _firstPersonHideRenderers;

        private float _yaw;
        private float _pitch;
        private float _currentYaw;
        private float _currentPitch;
        private float _yawVelocity;
        private float _pitchVelocity;

        private float _transitionT;
        private float _transitionTarget;
        private float _transitionVelocity;

        private float _shoulderTarget  = 1f;
        private float _shoulderCurrent = 1f;
        private float _shoulderVelocity;
        private float _bodyRotVelocity;
        private float _adsT;
        private float _lastZoomT;
        private float _crouchHeadOffset;
        private float _crouchHeadVelocity;
        private float _recoilPitch;
        private float _recoilYaw;
        private float _recoilRecoverySpeed    = 8f;
        private float _recoilRecoveryFraction = 0.75f;
        private float _recoilIdleTimer;
        private float _recoilOriginPitch;
        private float _recoilOriginYaw;
        private float _counterplayAccum;
        private Transform _lockTarget;
        private float     _lockHeight;
        [SerializeField] private float _counterplayThreshold = 2f;

        public bool    IsAimObstructed  { get; private set; }
        public Vector3 ObstructionPoint { get; private set; }
        public Camera  Camera           => _camera;
        /// <summary>0 = hip, 1 = fully aimed. Used by WeaponController for spread/recoil scaling.</summary>
        public float AdsT     => _adsT;
        // How far the view is zoomed in (FOV and aim sensitivity), per the ADS Zoom setting:
        // follows AdsT when Gradual; 0 until fully aimed in, then 1, when Snap. Accuracy and the
        // weapon pose always follow AdsT, so the setting is purely how the zoom looks.
        public float ZoomT    => GameSettings.Current.AdsZoom == AdsZoomMode.Snap ? (_adsT >= 1f ? 1f : 0f) : _adsT;

        public Transform LockTarget => _lockTarget;


        public void SetSensitivity(float mouse, float gamepad)
        {
            _mouseSensitivity   = mouse;
            _gamepadSensitivity = gamepad;
        }

        // Steers the aim toward target (at heightOffset above its pivot) until cleared.
        // Real aim, not a visual effect: shots follow it. Look input still adds on top.
        public void SetLockTarget(Transform target, float heightOffset)
        {
            _lockTarget = target;
            _lockHeight = heightOffset;
        }

        public void ClearLockTarget() => _lockTarget = null;

        // Pushed by WeaponController on Equip so each weapon can carry its own aim
        // zoom (sniper vs pistol) and transition speed (heavy vs snappy). Non-positive
        // arguments are ignored so a caller can leave one side untouched.
        public void SetAdsProfile(float fovDeg, float speed)
        {
            if (fovDeg > 0f) _adsFOV   = fovDeg;
            if (speed  > 0f) _adsSpeed = speed;
        }

        // Set by WeaponController: off while no firearm is equipped, so a melee weapon's
        // guard (same input) doesn't zoom.
        public bool AdsAllowed { get; set; } = true;
        // Set by PlayerOffhand: a held shield or active artifact uses the aim input instead.
        public bool AimTakenByOffhand { get; set; }

        // Aiming down sights began (the aim input went down while aiming is allowed).
        public event System.Action AimStarted;
        private bool _aimHeld;

        private void Awake()
        {
            SettingsSave.LoadSensitivity(out _mouseSensitivity, out _gamepadSensitivity);

            _yaw = _playerBody.eulerAngles.y;
            _currentYaw = _yaw;

            _transitionTarget = _startingMode == CameraMode.FirstPerson ? 0f : 1f;
            _transitionT = _transitionTarget;

            CursorLock.Set(true);

            RefreshMeshVisibility();
        }

        private void LateUpdate()
        {
            HandleToggleInput();
            HandleADS();
            RecoverRecoil();
            UpdateRotation();
            UpdateTransition();
            UpdateCamera();
            UpdateBodyRotation();
            UpdateFOV();
            CheckAimObstruction();
        }

        private void HandleToggleInput()
        {
            if (_input.GetAction(GameAction.TogglePerspective))
                _transitionTarget = _transitionTarget < 0.5f ? 1f : 0f;

            if (_input.GetAction(GameAction.ShoulderSwap))
                _shoulderTarget *= -1f;
        }

        private void HandleADS()
        {
            bool aiming = AdsAllowed && !AimTakenByOffhand && _input.GetAction(GameAction.AimDownSights);
            if (aiming && !_aimHeld) AimStarted?.Invoke();
            _aimHeld = aiming;
            float target = aiming ? 1f : 0f;
            _adsT = Mathf.MoveTowards(_adsT, target, _adsSpeed * Time.deltaTime);
        }

        private void RecoverRecoil()
        {
            if (_recoilPitch == 0f && _recoilYaw == 0f) return;

            // Don't recover while shots are still landing — wait until the gun goes idle.
            _recoilIdleTimer -= Time.deltaTime;
            if (_recoilIdleTimer > 0f) return;

            float dt = Time.deltaTime;
            float prevPitch = _recoilPitch;
            float prevYaw   = _recoilYaw;

            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, _recoilRecoverySpeed * dt);
            _recoilYaw   = Mathf.Lerp(_recoilYaw,   0f, _recoilRecoverySpeed * dt);

            // Pitch: clamp so recovery never pushes past the pre-burst origin.
            float pitchRecovery = (prevPitch - _recoilPitch) * _recoilRecoveryFraction;
            pitchRecovery = Mathf.Clamp(pitchRecovery, 0f, Mathf.Max(0f, _recoilOriginPitch - _pitch));
            _pitch += pitchRecovery;

            float yawStep = Mathf.Abs((prevYaw - _recoilYaw) * _recoilRecoveryFraction);
            float yawGap  = _recoilOriginYaw - _yaw;
            _yaw += Mathf.Clamp(yawGap, -yawStep, yawStep);
        }

        /// <summary>
        /// Called by WeaponController on each shot. Pitch is degrees upward; yaw is degrees right.
        /// recoverySpeed and recoveryFraction come from WeaponData so each gun feels different.
        /// </summary>
        public void AddRecoil(float pitch, float yaw, float recoverySpeed, float recoveryFraction, float recoveryDelay)
        {
            // Capture where the player was aiming before this burst started.
            // Used by recovery to avoid overshooting past origin when the player manually counteracted.
            if (_recoilPitch == 0f && _recoilYaw == 0f)
            {
                _recoilOriginPitch = _pitch;
                _recoilOriginYaw   = _yaw;
                _counterplayAccum  = 0f;
            }

            _recoilPitch += pitch;
            _recoilYaw   += yaw;
            _pitch       -= pitch;
            _yaw         += yaw;
            _recoilRecoverySpeed    = recoverySpeed;
            _recoilRecoveryFraction = recoveryFraction;
            _recoilIdleTimer = recoveryDelay;
        }

        /// <summary>
        /// Returns part of an earlier upward kick while shots are still landing, without
        /// touching the recovery timing. Pitch is degrees; positive moves the aim back down.
        /// </summary>
        public void SettleRecoil(float pitch)
        {
            _recoilPitch -= pitch;
            _pitch       += pitch;
        }

        private void UpdateRotation()
        {
            Vector2 look = _input.LookInput;
            float sensScale = Mathf.Lerp(1f, _adsSensitivityMult, ZoomT);
            float mult = (_input.IsGamepadLook ? _gamepadSensitivity * Time.deltaTime : _mouseSensitivity * 0.1f) * sensScale;

            _yaw += look.x * mult;
            _pitch = Mathf.Clamp(_pitch - look.y * mult, _minPitch, _maxPitch);

            // Deliberate counter-pull shifts the recovery origin so it settles where the player aimed.
            if (_recoilPitch > 0f && look.y < 0f)
            {
                _counterplayAccum += (-look.y) * mult;
                if (_counterplayAccum >= _counterplayThreshold)
                    _recoilOriginPitch = _pitch;
            }

            // Keep the yaw origin in sync with deliberate horizontal movement so recovery
            // never pulls the aim sideways against the player's intent.
            if (_recoilYaw != 0f && Mathf.Abs(look.x) > 0.01f)
                _recoilOriginYaw = _yaw;

            SteerToLockTarget();

            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _yaw, ref _yawVelocity, _rotationSmoothing);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _pitch, ref _pitchVelocity, _rotationSmoothing);
        }

        private void SteerToLockTarget()
        {
            if (_lockTarget == null) return;

            Vector3 toTarget = _lockTarget.position + Vector3.up * _lockHeight - transform.position;
            if (toTarget.sqrMagnitude < 0.01f) return;

            float targetYaw   = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(Mathf.Clamp(toTarget.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            float step        = _lockOnTurnSpeed * Time.deltaTime;

            _yaw   = Mathf.MoveTowardsAngle(_yaw, targetYaw, step);
            _pitch = Mathf.Clamp(Mathf.MoveTowardsAngle(_pitch, targetPitch, step), _minPitch, _maxPitch);
        }

        private void UpdateTransition()
        {
            float prev = _transitionT;
            _transitionT = Mathf.SmoothDamp(_transitionT, _transitionTarget, ref _transitionVelocity, _transitionSmoothTime);
            _shoulderCurrent = Mathf.SmoothDamp(_shoulderCurrent, _shoulderTarget, ref _shoulderVelocity, _transitionSmoothTime);

            if (Mathf.Abs(_transitionT - prev) > 0.001f)
                RefreshMeshVisibility();
        }

        private void UpdateCamera()
        {
            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            transform.rotation = rotation;

            float targetCrouchOffset = _movement.IsCrouching ? -_crouchHeadLower : 0f;
            _crouchHeadOffset = Mathf.SmoothDamp(_crouchHeadOffset, targetCrouchOffset, ref _crouchHeadVelocity, _crouchHeadSmoothTime);
            Vector3 fpsPos = _headAnchor.position + Vector3.up * _crouchHeadOffset;

            float activeDistance = Mathf.Lerp(_tpDistance, _adsTpDistance, _adsT);
            // In TP, keep a meaningful shoulder offset while ADS so the camera sits beside
            // the player rather than clipping through the back of their head.
            float activeShoulder = Mathf.Lerp(_shoulderOffset, _adsTpShoulderOffset, _adsT);

            // Collision: cast from head along -forward to find safe TP distance
            Vector3 back = rotation * Vector3.back;
            float safeDistance = activeDistance;
            if (Physics.SphereCast(_headAnchor.position, _collisionRadius, back,
                out RaycastHit hit, activeDistance, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                safeDistance = Mathf.Max(hit.distance - _collisionRadius, _tpMinDistance);
            }

            // Shoulder offset only applies in TP (transitionT = 0 in FP, so no effect there)
            float shoulder = activeShoulder * _transitionT * _shoulderCurrent;
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
            // The player's FOV setting replaces the authored base; sprint keeps its authored widening.
            float baseFOV = GameSettings.Current.FieldOfView;
            float hipFOV  = _movement.IsSprinting ? baseFOV + (_sprintFOV - _baseFOV) : baseFOV;
            float zoomT  = ZoomT;
            float target = Mathf.Lerp(hipFOV, AdsFov(baseFOV), zoomT);

            // In Snap mode the zoom itself jumps; sprint widening still eases.
            bool snapped = zoomT != _lastZoomT && GameSettings.Current.AdsZoom == AdsZoomMode.Snap;
            _lastZoomT = zoomT;
            _camera.fieldOfView = snapped ? target : Mathf.Lerp(_camera.fieldOfView, target, _fovSpeed * Time.deltaTime);
        }

        // Weapon ADS FOVs are authored against the default hip FOV (_baseFOV). Independent uses
        // them as they are; Affected keeps the weapon's magnification — the ratio of the view
        // widths, tan(fov/2) — and applies it to the player's chosen FOV instead.
        private float AdsFov(float playerFov)
        {
            if (GameSettings.Current.AdsFov != AdsFovMode.Affected) return _adsFOV;

            float magnification = Mathf.Tan(_baseFOV * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(_adsFOV * 0.5f * Mathf.Deg2Rad);
            float halfWidth = Mathf.Tan(playerFov * 0.5f * Mathf.Deg2Rad) / magnification;
            return 2f * Mathf.Atan(halfWidth) * Mathf.Rad2Deg;
        }

        private void RefreshMeshVisibility()
        {
            bool showMesh = _transitionT > 0.35f;
            foreach (var r in _firstPersonHideRenderers)
            {
                if (r != null) r.enabled = showMesh;
            }
        }

        private void CheckAimObstruction()
        {
            // Only relevant when in or transitioning to third-person.
            if (_transitionT < 0.1f)
            {
                IsAimObstructed = false;
                return;
            }

            Vector3 camForward = transform.forward;
            float maxDist = _tpDistance + 100f;
            Vector3 aimPoint = Physics.Raycast(transform.position, camForward, out RaycastHit camHit, maxDist, _collisionMask, QueryTriggerInteraction.Ignore)
                ? camHit.point
                : transform.position + camForward * maxDist;

            Vector3 toAim = aimPoint - _headAnchor.position;
            if (Physics.Raycast(_headAnchor.position, toAim.normalized, out RaycastHit headHit, toAim.magnitude - 0.05f, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                IsAimObstructed  = true;
                ObstructionPoint = headHit.point;
            }
            else
            {
                IsAimObstructed = false;
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
}
