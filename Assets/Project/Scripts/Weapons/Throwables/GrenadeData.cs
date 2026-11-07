using UnityEngine;
using UnityEngine.Serialization;
using CGD.Audio;
using CGD.Combat;

namespace CGD.Weapons
{
    // Everything that makes one kind of grenade: how far and how it is thrown, what sets it
    // off, how big and how hard the blast is, and how it looks and sounds. The Grenade
    // component on the prefab only runs these numbers.
    [CreateAssetMenu(fileName = "NewGrenade", menuName = "CGD/Weapons/Grenade Data")]
    public class GrenadeData : ScriptableObject
    {
        private const float MinBounceSpeed = 1.5f; // m/s; slower rebounds read as a dead stop

        [Header("Identity")]
        public string GrenadeName = "Frag Grenade";
        public GameObject GrenadePrefab;
        [Tooltip("Tint of the grenade body")]
        public Color BodyColor = new(0.28f, 0.34f, 0.2f);

        [Header("Throw")]
        [Tooltip("Charged: holding longer throws further. Fixed: every throw goes at Max Throw Speed, however long it's held")]
        public ThrowRangeMode RangeMode = ThrowRangeMode.Charged;
        [Tooltip("Charged only: launch speed of a quick tap (m/s). Range is about speed² ÷ 9.8 on flat ground")]
        [Min(0f)] public float MinThrowSpeed = 8f;
        [Tooltip("Launch speed once fully charged, and of every Fixed throw (m/s)")]
        [FormerlySerializedAs("ThrowForce")]
        [Min(0f)] public float MaxThrowSpeed = 18f;
        [Tooltip("Charged only: seconds of holding the throw button to reach full speed (0 = always full speed)")]
        [Min(0f)] public float ChargeTime = 0.8f;
        [Tooltip("Degrees the throw is lifted above where you look, so a throw at a target lands near it")]
        [Range(0f, 45f)] public float ThrowAngle = 6f;
        [Tooltip("How much of the thrower's own velocity the grenade takes along")]
        [Range(0f, 1f)] public float InheritVelocity = 0.5f;

        [Tooltip("Multiplies world gravity while it flies. Above 1 the throw is quicker and lands with more weight than " +
                 "real physics; pair it with a higher throw speed for the same range. The preview and enemy aim use it too")]
        [Min(0.1f)] public float GravityScale = 2f;

        [Header("Landing")]
        [Tooltip("Share of its speed into a surface the grenade bounces back with (0 = no bounce). " +
                 "The same on every surface, so bounces can be read and planned")]
        [Range(0f, 1f)] public float Bounciness = 0.3f;
        [Tooltip("Share of its speed along a surface the grenade keeps each time it hits one (0 = stops dead, 1 = slides on)")]
        [Range(0f, 1f)] public float ImpactSpeedKept = 0.45f;
        [Tooltip("How quickly it slows while resting on the ground (m/s per second)")]
        [Min(0f)] public float GroundDrag = 12f;

        [Header("Fuse")]
        public FuseMode Fuse = FuseMode.Timed;
        [Tooltip("Seconds after the throw before it goes off (for On Impact, the longest it may fly)")]
        [Min(0.1f)] public float FuseTime = 2.5f;
        [Tooltip("On Impact only: seconds after the throw before a hit can set it off, so it clears the thrower")]
        [Min(0f)] public float ArmTime = 0.15f;
        public Color FuseBlinkColor = new(1f, 0.15f, 0.05f);

        [Header("Explosion")]
        [Min(0f)] public float ExplosionRadius = 5f;
        [Min(0f)] public float ExplosionDamage = 80f;
        [Tooltip("Share of the damage still dealt at the very edge of the blast (1 = no falloff)")]
        [Range(0f, 1f)] public float EdgeDamage = 0f;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        public DamageType DamageType = DamageType.Physical;
        public LayerMask HitMask = ~0;
        [Tooltip("Walls and cover protect what is behind them")]
        public bool NeedsLineOfSight = true;
        [Tooltip("What blocks the blast when Needs Line Of Sight is on")]
        public LayerMask BlockerMask = 1;
        [Tooltip("Push applied to loose physics objects in the blast (0 = none)")]
        [Min(0f)] public float PushForce = 400f;
        [Tooltip("How far away enemies hear it (0 = silent)")]
        public float NoiseRadius = 50f;

        [Header("Aftermath")]
        [Tooltip("Optional — a pooled prefab left where it goes off: a smoke cloud (ConcealmentZone + PooledLifetime) or a FireZone")]
        [FormerlySerializedAs("SmokeCloudPrefab")]
        public GameObject LingeringZone;
        [Tooltip("Optional — a pooled prefab with an ExplosionFlash, spawned at the blast")]
        public GameObject ExplosionEffect;
        public Color FlashColor = new(1f, 0.6f, 0.15f, 0.8f);

        [Header("Camera")]
        [Tooltip("Shake at the blast centre (0..1), fading to nothing at Shake Radius")]
        [Range(0f, 1f)] public float CameraTrauma = 0.8f;
        public float ShakeRadius = 25f;

        [Header("On Hit")]
        [Tooltip("Status effects each hit may apply")]
        public StatusEffectApplication[] OnHitEffects;

        [Header("Audio")]
        public SoundBank ThrowSound;
        public SoundBank ExplosionSound;

        [Header("Trajectory Preview")]
        public int   TrajectorySteps    = 60;
        public float TrajectoryStepTime = 0.05f;
        [FormerlySerializedAs("DebugColor")]
        public Color PreviewColor = Color.yellow;

        // 0..1 for how long the throw button has been held; always full for a fixed-range throw.
        public float Charge(float heldSeconds) =>
            RangeMode == ThrowRangeMode.Fixed || ChargeTime <= 0f ? 1f : Mathf.Clamp01(heldSeconds / ChargeTime);

        // Where a throw at this charge sends the grenade: along the aim, lifted by the throw
        // angle, plus a share of the thrower's own velocity.
        public Vector3 LaunchVelocity(Vector3 aimForward, Vector3 aimRight, float charge, Vector3 throwerVelocity)
        {
            float speed = Mathf.Lerp(MinThrowSpeed, MaxThrowSpeed, Mathf.Clamp01(charge));
            Vector3 direction = Quaternion.AngleAxis(-ThrowAngle, aimRight) * aimForward;
            return direction * speed + throwerVelocity * InheritVelocity;
        }

        // The pull on the grenade in flight.
        public Vector3 Gravity => Physics.gravity * GravityScale;

        // What hitting a surface does to the grenade, decided here rather than by the physics
        // engine so it is the same everywhere and the preview can draw it: it rebounds with a share
        // of the speed it hit with and keeps a share of its speed along the surface. A rebound too
        // weak to see is dropped so it settles at once instead of buzzing on the ground.
        public Vector3 BounceVelocity(Vector3 incoming, Vector3 normal)
        {
            float into = Vector3.Dot(incoming, normal);
            if (into >= 0f) return incoming;

            Vector3 along = incoming - normal * into;
            float rebound = -into * Bounciness;
            if (rebound < MinBounceSpeed) rebound = 0f;
            return along * ImpactSpeedKept + normal * rebound;
        }

        // A bounce whose rebound is dropped: the grenade has come to rest on that surface.
        public bool Settles(Vector3 incoming, Vector3 normal) =>
            -Vector3.Dot(incoming, normal) * Bounciness < MinBounceSpeed;
    }
}
