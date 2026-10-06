using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // Rolls this enemy's random affixes each time it spawns from the pool, as many as its
    // tier allows, and runs it on a copy of its EnemyData with them applied. Back in the
    // pool it returns to its plain data, so an enemy spawned some other way never keeps
    // a previous life's affixes.
    [RequireComponent(typeof(EnemyHealth), typeof(EnemyAI))]
    public class EnemyAffixes : MonoBehaviour, IPoolable
    {
        [SerializeField] private EnemyAffixPool _pool;

        private readonly List<EnemyAffix> _rolled = new();
        private EnemyHealth _health;
        private EnemyAI     _ai;
        private EnemyData   _base;
        private EnemyData   _runtime;

        public IReadOnlyList<EnemyAffix> Rolled => _rolled;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _ai     = GetComponent<EnemyAI>();
            _base   = _health.Data;
        }

        private void OnDestroy()
        {
            if (_runtime != null) Destroy(_runtime);
        }

        public void OnSpawned()
        {
            int count = _pool != null && _base != null ? _pool.CountFor(_base.Tier) : 0;
            if (count == 0) return;

            _pool.Pick(count, _rolled);
            if (_runtime != null) Destroy(_runtime);
            _runtime = Instantiate(_base);
            foreach (EnemyAffix affix in _rolled) affix.ApplyTo(_runtime);
            Use(_runtime);
        }

        public void OnDespawned()
        {
            if (_rolled.Count == 0) return;
            _rolled.Clear();
            Use(_base);
        }

        private void Use(EnemyData data)
        {
            _ai.UseData(data);
            _health.UseData(data);
        }
    }
}
