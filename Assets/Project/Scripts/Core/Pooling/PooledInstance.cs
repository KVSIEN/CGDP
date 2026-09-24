using UnityEngine;

namespace CGD.Core
{
    // Added by PrefabPool to every instance it creates — never add it by hand. Remembers
    // which prefab the instance belongs to, whether it is currently out of the pool, and
    // the IPoolable callbacks to run, and drives delayed releases.
    [DisallowMultipleComponent]
    public sealed class PooledInstance : MonoBehaviour
    {
        private IPoolable[] _poolables;
        private float       _releaseTimer;

        public GameObject Prefab    { get; private set; }
        public bool       IsSpawned { get; private set; }

        internal void Bind(GameObject prefab)
        {
            Prefab     = prefab;
            _poolables = GetComponentsInChildren<IPoolable>(true);
            enabled    = false;
        }

        internal void MarkSpawned()
        {
            IsSpawned = true;
            foreach (IPoolable poolable in _poolables)
                poolable.OnSpawned();
        }

        internal void MarkDespawned()
        {
            IsSpawned = false;
            enabled   = false;
            foreach (IPoolable poolable in _poolables)
                poolable.OnDespawned();
        }

        // Only ticks while a delayed release is pending.
        internal void ReleaseAfter(float seconds)
        {
            _releaseTimer = seconds;
            enabled       = true;
        }

        private void Update()
        {
            _releaseTimer -= Time.deltaTime;
            if (_releaseTimer <= 0f) PrefabPool.Release(gameObject);
        }

        private void OnDestroy() => PrefabPool.Forget(this);
    }
}
