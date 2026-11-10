using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // What makes a Void echo wrong (EchoAbility). Its tree holds it still while its target looks
    // at it and lets it creep closer while not; this adds the rest:
    //   stared at   held squarely in view long enough, or walked right up to, it breaks apart
    //   mimicry     every few seconds while unwatched it repeats the last hostile noise it heard
    //               (gunfire, a reload, footsteps) from where it stands: rivals hear Void there
    //   shot        any hit breaks it (it never dies, so nothing mourns it as a fallen ally),
    //               and tells the Void that left it: shooting at phantoms is the flinch Void punishes
    // The tells: no health bar, no hit sound, and it never moves while you watch it.
    [RequireComponent(typeof(EnemyAI))]
    public class EchoBody : MonoBehaviour, ISummoned, IPoolable, IDamageInterceptor
    {
        [Tooltip("Seconds of being held squarely in view that break it")]
        [SerializeField, Min(0.1f)] private float _breakAfterStare = 1.5f;
        [Tooltip("Metres: a target this close finds nothing there")]
        [SerializeField, Min(0f)] private float _breakWithin = 2.5f;
        [Tooltip("Metres within which it picks up hostile noises to repeat")]
        [SerializeField, Min(0f)] private float _mimicRange = 30f;
        [Tooltip("Seconds between repeats while unwatched (random within the range)")]
        [SerializeField] private FloatRange _mimicEvery = new(3f, 5f);
        [Tooltip("Optional: what the player hears when it repeats a noise")]
        [SerializeField] private SoundBank _mimicSound;
        [Tooltip("Optional: the sting as it breaks apart")]
        [SerializeField] private SoundBank _breakSound;
        [Tooltip("Optional: Prefabs/VFX/ExplosionFlash")]
        [SerializeField] private GameObject _breakFlash;
        [SerializeField] private Color _breakColor = new(0.55f, 0.25f, 1f, 0.8f);

        private EnemyAI _ai;
        private HealthManager _health;
        private EnemyAI _summoner;
        private float _heardRadius;
        private float _nextMimic;
        private bool  _broken;

        private void Awake()
        {
            _ai     = GetComponent<EnemyAI>();
            _health = GetComponent<HealthManager>();
        }

        private void OnEnable()  => Noise.Emitted += OnNoise;
        private void OnDisable() => Noise.Emitted -= OnNoise;

        void ISummoned.SummonedBy(EnemyAI summoner) => _summoner = summoner;

        void IPoolable.OnSpawned()
        {
            _broken      = false;
            _heardRadius = 0f;
            _nextMimic   = Time.time + _mimicEvery.EvaluateClamped();
        }

        void IPoolable.OnDespawned() => _summoner = null;

        private void Update()
        {
            if (_broken)
            {
                // Released a frame after it broke, so a hit's other listeners finish first.
                PrefabPool.Release(gameObject);
                return;
            }
            if (_health.IsDead) return;

            if (_ai.Readings.Watching >= _breakAfterStare || TargetWithin(_breakWithin)) Break();
            else if (Time.time >= _nextMimic && _ai.Readings.Watching <= 0f) Mimic();
        }

        // Only hostile noises are worth repeating: the target's own sounds, coming from the wrong place.
        private void OnNoise(NoiseEvent noise)
        {
            if (noise.Source.Owner == gameObject || !_health.CanBeDamagedBy(noise.Source)) return;
            if ((noise.Position - transform.position).sqrMagnitude > _mimicRange * _mimicRange) return;

            _heardRadius = noise.Radius;
        }

        private void Mimic()
        {
            _nextMimic = Time.time + _mimicEvery.EvaluateClamped();
            if (_heardRadius <= 0f) return;

            Noise.Emit(transform.position, _heardRadius, DamageSource.Of(gameObject));
            if (_mimicSound != null) _mimicSound.Play(transform.position);
        }

        public int Order => int.MinValue;

        // Every hit is stopped: the echo breaks instead of being hurt.
        public float Intercept(in DamageInfo info, float amount, Vector3 point)
        {
            if (_broken) return 0f;

            GameObject shooter = info.Source.Owner;
            if (shooter != null && _summoner != null && _summoner.isActiveAndEnabled)
                _summoner.Readings.NoteDecoyShot(shooter.GetComponentInParent<HealthManager>());
            Break();
            return 0f;
        }

        private void Break()
        {
            if (_broken) return;
            _broken = true;

            if (_breakSound != null) _breakSound.Play(transform.position);
            if (_breakFlash == null) return;
            GameObject flash = PrefabPool.Spawn(_breakFlash, transform.position + Vector3.up, Quaternion.identity);
            if (flash.TryGetComponent(out ExplosionFlash burst)) burst.Play(1.2f, _breakColor);
        }

        private bool TargetWithin(float distance)
        {
            EnemyPerception perception = _ai.Perception;
            return perception.Target != null && perception.IsSensing &&
                   (perception.TargetPosition - transform.position).sqrMagnitude <= distance * distance;
        }
    }
}
