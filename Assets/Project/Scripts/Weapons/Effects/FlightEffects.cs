using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    // What a shot or a thrown grenade leaves behind as it flies: a fading trail, a stream of
    // small glowing sparks dropped along its path, and — for a Projectile — a burst where it
    // lands. Optionally turns a body child to point the way it flies (a mortar shell on a
    // grenade, whose rotation is frozen). Built at runtime from one transparent unlit material tinted per effect (the
    // object's own renderer stays as it is), and reset on every reuse from the pool.
    public class FlightEffects : MonoBehaviour, IPoolable
    {
        [Tooltip("Transparent unlit material, tinted per effect (e.g. Materials/ExplosionFlash)")]
        [SerializeField] private Material _material;
        [Tooltip("Mesh each spark is drawn with (the built-in Sphere)")]
        [SerializeField] private Mesh _sparkMesh;

        [Header("Trail")]
        [SerializeField] private Color _trailColor = new(1f, 0.8f, 0.2f, 0.6f);
        [Tooltip("Metres wide where it leaves the object; it tapers to nothing")]
        [SerializeField, Min(0f)] private float _trailWidth = 0.15f;
        [Tooltip("Seconds a point of the trail lasts. 0 = no trail")]
        [SerializeField, Min(0f)] private float _trailTime = 0.25f;

        [Header("Sparks")]
        [SerializeField] private Color _sparkColor = new(1f, 0.9f, 0.4f, 0.9f);
        [Tooltip("Sparks per metre flown. 0 = none")]
        [SerializeField, Min(0f)] private float _sparksPerMetre = 4f;
        [Tooltip("Metres across a spark starts at; it shrinks away")]
        [SerializeField, Min(0.005f)] private float _sparkSize = 0.07f;
        [Tooltip("Seconds a spark lasts")]
        [SerializeField, Min(0.05f)] private float _sparkLifetime = 0.35f;
        [Tooltip("Metres per second sparks scatter at")]
        [SerializeField, Min(0f)] private float _sparkScatter = 0.6f;
        [Tooltip("Positive drifts sparks down, negative floats them up")]
        [SerializeField] private float _sparkGravity = 0.3f;

        [Header("Body")]
        [Tooltip("Turned so its long (Y) axis points along the flight, as a built-in Capsule's: a shell or rocket body on an object that doesn't turn itself (a grenade's rotation is frozen). Empty = left as it is")]
        [SerializeField] private Transform _faceFlight;

        [Header("Impact (shots only)")]
        [Tooltip("Pooled prefab with an ExplosionFlash (Prefabs/VFX/ExplosionFlash). Empty = none")]
        [SerializeField] private GameObject _impactFlash;
        [SerializeField] private Color _impactColor = new(1f, 0.85f, 0.3f, 0.8f);
        [SerializeField, Min(0f)] private float _impactRadius = 0.5f;

        // Lays a capsule's long Y axis along the forward Z that LookRotation aims.
        private static readonly Quaternion LongAxisForward = Quaternion.Euler(90f, 0f, 0f);

        private TrailRenderer  _trail;
        private ParticleSystem _sparks;
        private Projectile     _projectile;
        private Vector3        _lastPosition;

        private void Awake()
        {
            if (_trailTime > 0f)      _trail  = CreateTrail();
            if (_sparksPerMetre > 0f) _sparks = CreateSparks();
            TryGetComponent(out _projectile);
        }

        private void OnEnable()
        {
            if (_projectile != null) _projectile.Impacted += OnImpacted;
        }

        private void OnDisable()
        {
            if (_projectile != null) _projectile.Impacted -= OnImpacted;
        }

        // A reused object would otherwise streak or spark from wherever its last flight ended.
        public void OnSpawned()
        {
            _lastPosition = transform.position;
            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = true;
            }
            if (_sparks == null) return;
            _sparks.Clear(true);
            _sparks.Play(true);
        }

        public void OnDespawned()
        {
            if (_trail != null) _trail.emitting = false;
            if (_sparks != null) _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void LateUpdate()
        {
            if (_faceFlight == null) return;

            Vector3 position = transform.position;
            Vector3 moved    = position - _lastPosition;
            _lastPosition    = position;
            // Resting or barely moving: keep the last heading rather than spin on jitter.
            if (moved.sqrMagnitude > 0.0001f) _faceFlight.rotation = Quaternion.LookRotation(moved) * LongAxisForward;
        }

        private void OnImpacted(Vector3 point, Vector3 normal)
        {
            if (_impactRadius <= 0f) return;
            ExplosionFlash.Spawn(_impactFlash, point + normal * 0.05f, _impactRadius, _impactColor);
        }

        // A child of its own: an object can carry only one renderer, and the shot has its mesh.
        private TrailRenderer CreateTrail()
        {
            TrailRenderer trail = EffectParts.Trail("Trail", transform, Vector3.zero, _material, _trailTime, _trailWidth, 0.05f);
            trail.numCapVertices = 2;
            trail.emitting       = true;
            trail.SetTint(_trailColor);
            return trail;
        }

        private ParticleSystem CreateSparks()
        {
            var sparks = Child("Sparks").AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // it starts on its defaults

            ParticleSystem.MainModule main = sparks.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(_sparkLifetime * 0.6f, _sparkLifetime);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0f, _sparkScatter);
            main.startSize       = new ParticleSystem.MinMaxCurve(_sparkSize * 0.6f, _sparkSize);
            main.gravityModifier = _sparkGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;   // left behind, not carried along
            main.maxParticles    = 128;

            ParticleSystem.EmissionModule emission = sparks.emission;
            emission.rateOverTime     = 0f;
            emission.rateOverDistance = _sparksPerMetre;

            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.04f;

            ParticleSystem.SizeOverLifetimeModule shrink = sparks.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode        = ParticleSystemRenderMode.Mesh;
            renderer.mesh              = _sparkMesh;
            renderer.sharedMaterial    = _material;
            EffectParts.NoShadows(renderer);
            renderer.SetTint(_sparkColor);

            sparks.Play(true);
            return sparks;
        }

        private GameObject Child(string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            return child;
        }
    }
}
