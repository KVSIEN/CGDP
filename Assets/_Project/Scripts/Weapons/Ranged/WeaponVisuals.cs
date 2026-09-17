using UnityEngine;

namespace CGD.Weapons
{
    // Weapon-pivot visual behaviour. Combines two independent motions on the same
    // transform, both spring-damped to look natural:
    //   * Kick   - pushed by AddKick each shot, always relaxes back to rest.
    //   * Sway   - pulled by look input, movement and idle breathing, relaxes
    //              back to rest when the player stops moving/aiming.
    // Both motions live in local space so the artist's rest pose is preserved.
    public class WeaponVisuals : MonoBehaviour
    {
        [Header("Kick")]
        [SerializeField] private float _kickPitchScale   = 8f;
        [SerializeField] private float _kickZPushback    = 0.04f;
        [SerializeField] private float _kickRollScale    = 0.4f;

        [Header("Kick Spring")]
        [SerializeField] private float _rotationStiffness = 18f;
        [SerializeField] private float _rotationDamping   = 6f;
        [SerializeField] private float _positionStiffness = 22f;
        [SerializeField] private float _positionDamping   = 7f;

        [Header("Sway Scaling")]
        [Tooltip("Rotation applied per unit of look input (multiplied by LookSwayAmount from WeaponData)")]
        [SerializeField] private float _lookPitchScale = 0.06f;
        [SerializeField] private float _lookYawScale   = 0.08f;
        [Tooltip("Slight roll added when swinging horizontally, as a fraction of yaw sway")]
        [SerializeField] private float _lookRollFactor = 0.15f;
        [Tooltip("Vertical bob amplitude in metres per unit of MoveSwayAmount at full speed")]
        [SerializeField] private float _moveBobAmplitude = 0.012f;
        [Tooltip("Bob cycles per second while sprinting; scales with speed")]
        [SerializeField] private float _moveBobSpeed = 5f;
        [Tooltip("Move speed at which bob reaches full amplitude (metres per second)")]
        [SerializeField] private float _moveBobMaxSpeed = 6f;
        [Tooltip("Damping applied to sway springs (lower = looser weapon)")]
        [SerializeField] private float _swayDamping = 4f;

        private WeaponData _data;

        // Kick state
        private Vector3 _kickRotOffset;
        private Vector3 _kickRotVelocity;
        private Vector3 _kickPosOffset;
        private Vector3 _kickPosVelocity;

        // Sway state
        private Vector3 _swayRotOffset;
        private Vector3 _swayRotVelocity;
        private Vector3 _swayPosOffset;
        private Vector3 _swayPosVelocity;

        // Live inputs pushed by WeaponController
        private Vector2 _lookInput;
        private float   _horizontalSpeed;
        private bool    _isGrounded = true;
        private float   _adsT;
        private float   _idlePhaseOffset;

        // Called by WeaponController when the equipped weapon changes.
        public void Configure(WeaponData data)
        {
            _data = data;
            _kickRotOffset = Vector3.zero;
            _kickRotVelocity = Vector3.zero;
            _kickPosOffset = Vector3.zero;
            _kickPosVelocity = Vector3.zero;
            _swayRotOffset = Vector3.zero;
            _swayRotVelocity = Vector3.zero;
            _swayPosOffset = Vector3.zero;
            _swayPosVelocity = Vector3.zero;
            _idlePhaseOffset = Random.value * 100f;
        }

        // Called by WeaponController every Update so LateUpdate has fresh values.
        public void SetSwayInputs(Vector2 lookInput, float horizontalSpeed, bool isGrounded, float adsT)
        {
            _lookInput       = lookInput;
            _horizontalSpeed = horizontalSpeed;
            _isGrounded      = isGrounded;
            _adsT            = adsT;
        }

        // Called by WeaponController on every shot.
        // vertKick and horizKick are the same values passed to PlayerCamera.AddRecoil.
        public void AddKick(float vertKick, float horizKick)
        {
            _kickRotOffset.x -= vertKick  * _kickPitchScale;
            _kickRotOffset.z -= horizKick * _kickRollScale;
            _kickPosOffset.z -= _kickZPushback;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            _kickRotOffset = SpringDamp(_kickRotOffset, Vector3.zero, ref _kickRotVelocity,
                _rotationStiffness, _rotationDamping, dt);
            _kickPosOffset = SpringDamp(_kickPosOffset, Vector3.zero, ref _kickPosVelocity,
                _positionStiffness, _positionDamping, dt);

            ComputeSwayTargets(out Vector3 swayRotTarget, out Vector3 swayPosTarget);

            float swayStiffness = _data != null ? _data.LookSwayRecovery : 10f;
            _swayRotOffset = SpringDamp(_swayRotOffset, swayRotTarget, ref _swayRotVelocity,
                swayStiffness, _swayDamping, dt);
            _swayPosOffset = SpringDamp(_swayPosOffset, swayPosTarget, ref _swayPosVelocity,
                swayStiffness, _swayDamping, dt);

            transform.localRotation = Quaternion.Euler(_kickRotOffset + _swayRotOffset);
            transform.localPosition = _kickPosOffset + _swayPosOffset;
        }

        private void ComputeSwayTargets(out Vector3 rotTarget, out Vector3 posTarget)
        {
            rotTarget = Vector3.zero;
            posTarget = Vector3.zero;
            if (_data == null) return;

            // ADS clamps every kind of sway toward zero (breath hold / stabilization).
            float adsScale = Mathf.Lerp(1f, _data.AdsSwayMultiplier, _adsT);

            // Look sway: weapon lags opposite the aim direction, spring pulls it back
            // once input stops. Vertical look (y) drives pitch; horizontal look (x)
            // drives yaw plus a subtle roll for weight.
            float swayAmount = _data.LookSwayAmount * adsScale;
            rotTarget.x += -_lookInput.y * _lookPitchScale * swayAmount;
            rotTarget.y += -_lookInput.x * _lookYawScale   * swayAmount;
            rotTarget.z += -_lookInput.x * _lookYawScale   * swayAmount * _lookRollFactor;

            // Idle breathing: slow Perlin drift on pitch/yaw, always present.
            float t = Time.time * _data.IdleSwaySpeed + _idlePhaseOffset;
            float idleX = (Mathf.PerlinNoise(t, 0f)      - 0.5f) * 2f;
            float idleY = (Mathf.PerlinNoise(0f, t + 5f) - 0.5f) * 2f;
            float idleAmp = _data.IdleSwayAmount * adsScale;
            rotTarget.x += idleX * idleAmp;
            rotTarget.y += idleY * idleAmp;

            // Move bob: sinusoidal position/rotation while grounded and moving.
            // Vertical bob at 2x horizontal gives the classic figure-8 walk pattern.
            if (_isGrounded && _horizontalSpeed > 0.1f)
            {
                float speedT   = Mathf.Clamp01(_horizontalSpeed / _moveBobMaxSpeed);
                float bobT     = Time.time * _moveBobSpeed * Mathf.Lerp(0.6f, 1f, speedT);
                float strength = _data.MoveSwayAmount * speedT * adsScale;

                posTarget.x += Mathf.Sin(bobT)        * _moveBobAmplitude * strength;
                posTarget.y += Mathf.Sin(bobT * 2f)   * _moveBobAmplitude * strength * 0.5f;
                rotTarget.z += Mathf.Sin(bobT)        * strength;
            }
        }

        private static Vector3 SpringDamp(Vector3 current, Vector3 target, ref Vector3 velocity,
                                          float stiffness, float damping, float dt)
        {
            Vector3 force = (target - current) * stiffness - velocity * damping;
            velocity += force * dt;
            return current + velocity * dt;
        }
    }
}
