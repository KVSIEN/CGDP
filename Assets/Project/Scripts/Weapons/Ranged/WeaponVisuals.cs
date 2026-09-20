using System;
using UnityEngine;

namespace CGD.Weapons
{
    /// <summary>
    /// Weapon-pivot visual behaviour (child of the camera). Combines independent motions on
    /// the same transform, all in local space so the rest pose is preserved:
    ///   * Hip kick  - spring pushed by AddKick; the gun itself rears up and rolls, because
    ///                 from the hip the camera barely moves.
    ///   * ADS kick  - timed pulse pushed by AddKick; while aiming the camera carries the climb
    ///                 and bullets go to the crosshair, so the pulse is guaranteed back at zero
    ///                 before the next round can fire — the sights are centred on the crosshair
    ///                 whenever a shot leaves.
    ///   * Sway      - pulled by look input, movement and idle breathing (amounts from the
    ///                 equipped WeaponData). Fades out while aiming for the same reason.
    /// </summary>
    public class WeaponVisuals : MonoBehaviour
    {
        // Kick amounts are per degree of the weapon's own per-shot kick (pushback is per shot).
        [Serializable]
        private struct KickProfile
        {
            [Tooltip("Degrees of muzzle rise")]                                  public float Pitch;
            [Tooltip("Degrees of sideways swing per degree of horizontal kick")] public float Yaw;
            [Tooltip("Degrees of roll per degree of horizontal kick")]           public float Roll;
            [Tooltip("Metres of upward hop")]                                    public float Rise;
            [Tooltip("Metres pushed back per shot")]                             public float Pushback;

            public void Apply(float vertKick, float horizKick, float weight, ref Vector3 rot, ref Vector3 pos)
            {
                rot.x -= vertKick  * Pitch * weight;  // negative X = muzzle rise in local space
                rot.y += horizKick * Yaw   * weight;
                rot.z -= horizKick * Roll  * weight;
                pos.y += vertKick  * Rise  * weight;
                pos.z -= Pushback * weight;
            }
        }

        [Header("Hip — spring kick")]
        [SerializeField] private KickProfile _hip = new() { Pitch = 1.5f, Roll = 1.5f, Pushback = 0.03f };
        [SerializeField] private float _rotationStiffness = 150f;
        [SerializeField] private float _rotationDamping   = 20f;
        [SerializeField] private float _positionStiffness = 200f;
        [SerializeField] private float _positionDamping   = 24f;

        [Header("ADS — timed kick")]
        [SerializeField] private KickProfile _ads = new() { Pitch = 0.35f, Yaw = 0.2f, Roll = 0.2f, Rise = 0.002f, Pushback = 0.02f };
        [Tooltip("Longest an aimed kick takes to return to centre; faster weapons return within their own shot interval")]
        [SerializeField] private float _adsMaxRecoverTime = 0.15f;

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

        // Stiff springs are unstable with large frame steps, so integrate in small sub-steps.
        private const float MaxSpringStep = 1f / 240f;

        // Recover slightly inside the shot interval so frame timing can't leave a residue.
        private const float IntervalMargin = 0.9f;

        private WeaponData _data;

        // Hip kick spring state — local offsets relative to rest
        private Vector3 _kickRot;
        private Vector3 _kickRotVelocity;
        private Vector3 _kickPos;
        private Vector3 _kickPosVelocity;

        // ADS pulse state — offsets at the moment of the last kick, eased back to zero
        private Vector3 _pulseRot;
        private Vector3 _pulsePos;
        private float   _pulseStart;
        private float   _pulseDuration;

        // Sway spring state
        private Vector3 _swayRot;
        private Vector3 _swayRotVelocity;
        private Vector3 _swayPos;
        private Vector3 _swayPosVelocity;

        // Live inputs pushed by WeaponController
        private Vector2 _lookInput;
        private float   _horizontalSpeed;
        private bool    _isGrounded = true;
        private float   _adsT;
        private float   _idlePhaseOffset;

        /// <summary>Called by WeaponController when the equipped weapon changes.</summary>
        public void Configure(WeaponData data)
        {
            _data = data;
            _kickRot  = _kickRotVelocity = _kickPos = _kickPosVelocity = Vector3.zero;
            _swayRot  = _swayRotVelocity = _swayPos = _swayPosVelocity = Vector3.zero;
            _pulseRot = _pulsePos = Vector3.zero;
            _pulseDuration   = 0f;
            _idlePhaseOffset = UnityEngine.Random.value * 100f;
        }

        /// <summary>Called by WeaponController every Update so LateUpdate has fresh values.</summary>
        public void SetSwayInputs(Vector2 lookInput, float horizontalSpeed, bool isGrounded, float adsT)
        {
            _lookInput       = lookInput;
            _horizontalSpeed = horizontalSpeed;
            _isGrounded      = isGrounded;
            _adsT            = adsT;
        }

        /// <summary>
        /// Called by WeaponController on every shot. vertKick/horizKick are the weapon's own
        /// kick in degrees (before the hip/ADS camera share); adsT is 0 at hip, 1 fully aimed;
        /// shotInterval is the time until the weapon can fire again.
        /// </summary>
        public void AddKick(float vertKick, float horizKick, float adsT, float shotInterval)
        {
            _hip.Apply(vertKick, horizKick, 1f - adsT, ref _kickRot, ref _kickPos);

            // Start the new pulse from whatever is left of the previous one so it never pops.
            float remaining = PulseRemaining();
            _pulseRot *= remaining;
            _pulsePos *= remaining;
            _ads.Apply(vertKick, horizKick, adsT, ref _pulseRot, ref _pulsePos);

            _pulseStart    = Time.time;
            _pulseDuration = Mathf.Min(shotInterval * IntervalMargin, _adsMaxRecoverTime);
        }

        private void LateUpdate()
        {
            ComputeSwayTargets(out Vector3 swayRotTarget, out Vector3 swayPosTarget);
            float swayStiffness = _data != null ? _data.LookSwayRecovery : 10f;

            float dt    = Time.deltaTime;
            int   steps = Mathf.Max(1, Mathf.CeilToInt(dt / MaxSpringStep));
            float h     = dt / steps;

            for (int i = 0; i < steps; i++)
            {
                _kickRot = SpringDamp(_kickRot, Vector3.zero,  ref _kickRotVelocity, _rotationStiffness, _rotationDamping, h);
                _kickPos = SpringDamp(_kickPos, Vector3.zero,  ref _kickPosVelocity, _positionStiffness, _positionDamping, h);
                _swayRot = SpringDamp(_swayRot, swayRotTarget, ref _swayRotVelocity, swayStiffness,      _swayDamping,     h);
                _swayPos = SpringDamp(_swayPos, swayPosTarget, ref _swayPosVelocity, swayStiffness,      _swayDamping,     h);
            }

            float remaining = PulseRemaining();
            transform.localRotation = Quaternion.Euler(_kickRot + _swayRot + _pulseRot * remaining);
            transform.localPosition = _kickPos + _swayPos + _pulsePos * remaining;
        }

        // 1 at the moment of the kick, easing to exactly 0 once the pulse duration has passed.
        private float PulseRemaining()
        {
            if (_pulseDuration <= 0f) return 0f;
            float u = Mathf.Clamp01((Time.time - _pulseStart) / _pulseDuration);
            return 1f - Mathf.SmoothStep(0f, 1f, u);
        }

        private void ComputeSwayTargets(out Vector3 rotTarget, out Vector3 posTarget)
        {
            rotTarget = Vector3.zero;
            posTarget = Vector3.zero;
            if (_data == null) return;

            // All sway fades out while aiming: bullets go to the crosshair, so any sway would
            // push the sights off the point the shot actually goes to.
            float swayScale = 1f - _adsT;
            if (swayScale <= 0f) return;

            // Look sway: weapon lags opposite the aim direction, spring pulls it back once
            // input stops. Vertical look (y) drives pitch; horizontal look (x) drives yaw plus
            // a subtle roll for weight.
            float lookAmount = _data.LookSwayAmount * swayScale;
            rotTarget.x += -_lookInput.y * _lookPitchScale * lookAmount;
            rotTarget.y += -_lookInput.x * _lookYawScale   * lookAmount;
            rotTarget.z += -_lookInput.x * _lookYawScale   * lookAmount * _lookRollFactor;

            // Idle breathing: slow Perlin drift on pitch/yaw.
            float t     = Time.time * _data.IdleSwaySpeed + _idlePhaseOffset;
            float idleX = (Mathf.PerlinNoise(t, 0f)      - 0.5f) * 2f;
            float idleY = (Mathf.PerlinNoise(0f, t + 5f) - 0.5f) * 2f;
            float idleAmp = _data.IdleSwayAmount * swayScale;
            rotTarget.x += idleX * idleAmp;
            rotTarget.y += idleY * idleAmp;

            // Move bob: sinusoidal position/rotation while grounded and moving. Vertical bob
            // at 2x horizontal gives the classic figure-8 walk pattern.
            if (_isGrounded && _horizontalSpeed > 0.1f)
            {
                float speedT   = Mathf.Clamp01(_horizontalSpeed / _moveBobMaxSpeed);
                float bobT     = Time.time * _moveBobSpeed * Mathf.Lerp(0.6f, 1f, speedT);
                float strength = _data.MoveSwayAmount * speedT * swayScale;

                posTarget.x += Mathf.Sin(bobT)      * _moveBobAmplitude * strength;
                posTarget.y += Mathf.Sin(bobT * 2f) * _moveBobAmplitude * strength * 0.5f;
                rotTarget.z += Mathf.Sin(bobT)      * strength;
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
