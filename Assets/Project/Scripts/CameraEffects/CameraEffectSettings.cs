using UnityEngine;

namespace CGD.CameraEffects
{
    [CreateAssetMenu(fileName = "CameraEffectSettings", menuName = "CGD/Camera/Camera Effect Settings")]
    public class CameraEffectSettings : ScriptableObject
    {
        [Header("Shake (trauma)")]
        [Tooltip("Rotation at full trauma, degrees: pitch, yaw, roll")]
        [SerializeField] private Vector3 _maxShakeAngles = new(3f, 3f, 5f);
        [Tooltip("Position offset at full trauma, metres")]
        [SerializeField] private Vector3 _maxShakeOffset = new(0.05f, 0.05f, 0.02f);
        [Tooltip("Noise speed — higher is more violent")]
        [SerializeField, Min(0f)] private float _shakeFrequency = 18f;
        [Tooltip("Trauma lost per second")]
        [SerializeField, Min(0f)] private float _traumaDecay = 1.2f;
        [Tooltip("Shake = trauma^exponent: 2 keeps small hits subtle while big ones still punch")]
        [SerializeField, Range(1f, 3f)] private float _traumaExponent = 2f;

        [Header("Kicks (spring)")]
        [Tooltip("How hard a kicked camera is pulled back to rest")]
        [SerializeField, Min(0f)] private float _springStiffness = 220f;
        [Tooltip("How quickly the spring's wobble dies out")]
        [SerializeField, Min(0f)] private float _springDamping = 22f;
        [Tooltip("Extra view kick per degree of weapon aim recoil (visual only)")]
        [SerializeField, Min(0f)] private float _recoilViewKick = 12f;

        [Header("FOV kick")]
        [Tooltip("How fast a FOV kick returns to normal (per second, exponential)")]
        [SerializeField, Min(0f)] private float _fovRecovery = 6f;

        [Header("Lag")]
        [Tooltip("Seconds the camera takes to catch up with its target position. 0 = off (recommended for first person)")]
        [SerializeField, Min(0f)] private float _positionLag;
        [Tooltip("The camera never trails further than this, so teleports don't leave it behind")]
        [SerializeField, Min(0f)] private float _maxLagDistance = 0.5f;

        [Header("Damage")]
        [Tooltip("Trauma added per point of damage the watched character takes")]
        [SerializeField, Min(0f)] private float _traumaPerDamage = 0.012f;
        [SerializeField, Range(0f, 1f)] private float _maxDamageTrauma = 0.6f;

        public Vector3 MaxShakeAngles   => _maxShakeAngles;
        public Vector3 MaxShakeOffset   => _maxShakeOffset;
        public float   ShakeFrequency   => _shakeFrequency;
        public float   TraumaDecay      => _traumaDecay;
        public float   TraumaExponent   => _traumaExponent;
        public float   SpringStiffness  => _springStiffness;
        public float   SpringDamping    => _springDamping;
        public float   RecoilViewKick   => _recoilViewKick;
        public float   FovRecovery      => _fovRecovery;
        public float   PositionLag      => _positionLag;
        public float   MaxLagDistance   => _maxLagDistance;
        public float   TraumaPerDamage  => _traumaPerDamage;
        public float   MaxDamageTrauma  => _maxDamageTrauma;
    }
}
