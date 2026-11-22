using UnityEngine;

namespace CGD.Core
{
    // Returns a spawned effect (particles, decals, one-shot VFX) to its pool on its own.
    // With a lifetime of 0 it waits until every ParticleSystem in the hierarchy has
    // finished; otherwise it releases after the fixed lifetime.
    public class PooledLifetime : MonoBehaviour, IPoolable
    {
        [Tooltip("Seconds before release. 0 = release once all particles have finished.")]
        [SerializeField, Min(0f)] private float _lifetime;

        // Seconds it lasts before release (0 = until its particles finish), for effects that fade out with it.
        public float Lifetime => _lifetime;

        private ParticleSystem _particles;
        private float          _age;

        private void Awake() => _particles = GetComponentInChildren<ParticleSystem>();

        public void OnSpawned()
        {
            _age = 0f;
            if (_particles != null) _particles.Play(true);
        }

        public void OnDespawned()
        {
            if (_particles != null) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (IsFinished()) PrefabPool.Release(gameObject);
        }

        private bool IsFinished()
        {
            if (_lifetime > 0f) return _age >= _lifetime;
            return _particles == null || !_particles.IsAlive(true);
        }
    }
}
