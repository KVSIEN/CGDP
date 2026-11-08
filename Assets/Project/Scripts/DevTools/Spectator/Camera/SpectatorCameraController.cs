using UnityEngine;

namespace CGD.DevTools
{
    // Where the spectator camera should be, from the steering it gets each frame. Plain
    // maths, no Unity objects, so it can be tested.
    //   Overview  the placed viewpoint's angle, looking at a point on the ground: move pans
    //             that point (faster when zoomed out, never far from the arena), zoom moves
    //             along the view. Turning (Look) leaves it for Free from where it is.
    //   Follow    orbits a point given each frame (an agent): Look turns round it, zoom moves
    //             closer or further. Moving leaves it for Free, from where it is.
    //   Free      flies where it faces; up / down are straight up and down; zoom sets the speed.
    // The camera glides toward where it is steered (smoothing 0 = snaps there), on real time,
    // so it keeps working while the game is paused.
    public sealed class SpectatorCameraController
    {
        private const float FastMultiplier   = 3f;
        private const float ZoomStep         = 0.12f;   // share of the distance one zoom step covers
        private const float MinOverviewZoom  = 0.25f;   // × the placed distance
        private const float MaxOverviewZoom  = 1.6f;
        private const float MinFollowDistance = 3f;
        private const float MaxFollowDistance = 45f;
        private const float DefaultFollowDistance = 12f;
        private const float MinFollowPitch   = 5f;
        private const float MaxFollowPitch   = 85f;
        private const float FreePitchLimit   = 85f;
        private const float MinFreeSpeed     = 0.25f;  // × the move speed
        private const float MaxFreeSpeed     = 8f;
        private const float SnapSharpness    = 30f;    // per second, smoothing 0
        private const float GlideSharpness   = 3.5f;   // per second, smoothing 1

        private readonly Vector3 _homeFocus;
        private readonly float   _homeDistance;
        private readonly float   _homeYaw;
        private readonly float   _homePitch;
        private readonly float   _panLimit;

        private Vector3 _focus;
        private float   _overviewDistance;
        private float   _orbitYaw;
        private float   _orbitPitch;
        private float   _orbitDistance = DefaultFollowDistance;
        private float   _freeYaw;
        private float   _freePitch;
        private Vector3 _freePosition;

        private Vector3    _targetPosition;
        private Quaternion _targetRotation;

        // home: the viewpoint placed in the scene. panLimit: metres the overview may pan from
        // where that viewpoint looks.
        public SpectatorCameraController(Vector3 homePosition, Quaternion homeRotation, float panLimit)
        {
            Vector3 euler = homeRotation.eulerAngles;
            _homeYaw   = euler.y;
            _homePitch = Mathf.Clamp(SignedAngle(euler.x), 10f, 89f);
            _panLimit  = Mathf.Max(0f, panLimit);

            Vector3 forward = Quaternion.Euler(_homePitch, _homeYaw, 0f) * Vector3.forward;
            _homeDistance = homePosition.y > 0.1f ? homePosition.y / -forward.y : 20f;
            _homeFocus    = homePosition + forward * _homeDistance;

            ShowOverview();
            Position = _targetPosition;
            Rotation = _targetRotation;
        }

        public SpectatorCameraMode Mode { get; private set; }
        public Vector3    Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        // Free flight speed, as a multiple of the move speed (zoom changes it).
        public float FreeSpeed { get; private set; } = 1f;

        public void ShowOverview()
        {
            Mode              = SpectatorCameraMode.Overview;
            _focus            = _homeFocus;
            _overviewDistance = _homeDistance;
            AimOverview();
        }

        // Starts orbiting, from the side the camera is already looking from.
        public void Follow()
        {
            if (Mode != SpectatorCameraMode.Follow)
            {
                Vector3 euler = Rotation.eulerAngles;
                _orbitYaw   = euler.y;
                _orbitPitch = Mathf.Clamp(SignedAngle(euler.x), 25f, 60f);
            }
            Mode = SpectatorCameraMode.Follow;
        }

        // Flies on from exactly where the camera is.
        public void FlyFree()
        {
            Vector3 euler = Rotation.eulerAngles;
            _freeYaw      = euler.y;
            _freePitch    = Mathf.Clamp(SignedAngle(euler.x), -FreePitchLimit, FreePitchLimit);
            _freePosition = Position;
            Mode          = SpectatorCameraMode.Free;
        }

        // followPoint: what Follow orbits (an agent's chest); null keeps the last pose.
        public void Tick(in SpectatorMotion motion, Vector3? followPoint, float moveSpeed, float smoothing, float deltaTime)
        {
            if (Mode == SpectatorCameraMode.Overview && motion.IsLooking) FlyFree();
            if (Mode == SpectatorCameraMode.Follow && motion.IsMoving) FlyFree();

            float speed = moveSpeed * (motion.Fast ? FastMultiplier : 1f);
            switch (Mode)
            {
                case SpectatorCameraMode.Overview: TickOverview(motion, speed, deltaTime); break;
                case SpectatorCameraMode.Follow:   TickFollow(motion, followPoint);         break;
                default:                           TickFree(motion, speed, deltaTime);      break;
            }

            float sharpness = Mathf.Lerp(SnapSharpness, GlideSharpness, Mathf.Clamp01(smoothing));
            float t = 1f - Mathf.Exp(-sharpness * Mathf.Max(0f, deltaTime));
            Position = Vector3.Lerp(Position, _targetPosition, t);
            Rotation = Quaternion.Slerp(Rotation, _targetRotation, t);
        }

        // --- Modes --------------------------------------------------------------------

        private void TickOverview(in SpectatorMotion motion, float speed, float deltaTime)
        {
            if (motion.Zoom != 0)
                _overviewDistance = Mathf.Clamp(_overviewDistance * Mathf.Pow(1f - ZoomStep, motion.Zoom),
                                                _homeDistance * MinOverviewZoom, _homeDistance * MaxOverviewZoom);

            // Pans over the ground in the camera's facing; further out, faster.
            Quaternion yaw = Quaternion.Euler(0f, _homeYaw, 0f);
            Vector3 pan = yaw * new Vector3(motion.Move.x, 0f, motion.Move.z);
            _focus += pan * (speed * (_overviewDistance / _homeDistance) * deltaTime);

            Vector3 offset = _focus - _homeFocus;
            offset.y = 0f;
            if (offset.sqrMagnitude > _panLimit * _panLimit) _focus = _homeFocus + offset.normalized * _panLimit;
            AimOverview();
        }

        private void AimOverview()
        {
            _targetRotation = Quaternion.Euler(_homePitch, _homeYaw, 0f);
            _targetPosition = _focus - _targetRotation * Vector3.forward * _overviewDistance;
        }

        private void TickFollow(in SpectatorMotion motion, Vector3? followPoint)
        {
            _orbitYaw  += motion.Look.x;
            _orbitPitch = Mathf.Clamp(_orbitPitch - motion.Look.y, MinFollowPitch, MaxFollowPitch);
            if (motion.Zoom != 0)
                _orbitDistance = Mathf.Clamp(_orbitDistance * Mathf.Pow(1f - ZoomStep, motion.Zoom), MinFollowDistance, MaxFollowDistance);
            if (followPoint == null) return;

            Orbit(followPoint.Value, _orbitYaw, _orbitPitch, _orbitDistance, out _targetPosition, out _targetRotation);
        }

        private void TickFree(in SpectatorMotion motion, float speed, float deltaTime)
        {
            _freeYaw  += motion.Look.x;
            _freePitch = Mathf.Clamp(_freePitch - motion.Look.y, -FreePitchLimit, FreePitchLimit);
            if (motion.Zoom != 0) FreeSpeed = Mathf.Clamp(FreeSpeed * Mathf.Pow(1.25f, motion.Zoom), MinFreeSpeed, MaxFreeSpeed);

            Quaternion rotation = Quaternion.Euler(_freePitch, _freeYaw, 0f);
            Vector3 move = rotation * new Vector3(motion.Move.x, 0f, motion.Move.z) + Vector3.up * motion.Move.y;
            _freePosition += move * (speed * FreeSpeed * deltaTime);

            _targetPosition = _freePosition;
            _targetRotation = rotation;
        }

        // --- Maths ----------------------------------------------------------------------

        // A camera orbiting `pivot` at yaw / pitch degrees and `distance` metres, looking at it.
        public static void Orbit(Vector3 pivot, float yaw, float pitch, float distance, out Vector3 position, out Quaternion rotation)
        {
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            position = pivot - rotation * Vector3.forward * distance;
        }

        // 0…360 → −180…180.
        public static float SignedAngle(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f) degrees -= 360f;
            if (degrees < -180f) degrees += 360f;
            return degrees;
        }
    }
}
