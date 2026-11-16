using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Loot;

namespace CGD.Level
{
    // A structure that keeps sending enemies into its room until it is destroyed. It only
    // runs while the player is near, and never keeps more than its cap alive. Enemies it
    // already sent stay behind when it falls. They drop only a share of their usual loot
    // (none by default): the payout is for destroying the spawner, not for farming it.
    [RequireComponent(typeof(HealthManager))]
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private HealthManager _health;

        private EncounterSpawner _spawner;
        private Transform        _player;
        private SpawnPacing      _pacing;
        private float            _activationRangeSqr;
        private float            _enemyLoot;
        private readonly EnemyGroup _sent = new();

        public event Action Destroyed;

        private void Reset() => _health = GetComponent<HealthManager>();

        private void Awake()
        {
            if (_health == null) _health = GetComponent<HealthManager>();
        }

        public void Initialize(EncounterSpawner spawner, Transform player, EncounterSettings settings)
        {
            _spawner            = spawner;
            _player             = player;
            _pacing             = new SpawnPacing(settings.SpawnerInterval, settings.SpawnerBatch, settings.SpawnerCap);
            _activationRangeSqr = settings.SpawnerActivationRange * settings.SpawnerActivationRange;
            _enemyLoot          = settings.SpawnerEnemyLoot;
            _health.OnDeath    += HandleDeath;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (_pacing == null || _health.IsDead || _player == null) return;
            if ((_player.position - transform.position).sqrMagnitude > _activationRangeSqr) return;

            int count = _pacing.Advance(Time.deltaTime, _sent.Alive);
            if (count > 0) Send(count);
        }

        private void Send(int count)
        {
            List<HealthManager> enemies = _spawner.Spawn(count);
            foreach (HealthManager enemy in enemies)
                if (enemy.TryGetComponent(out LootDropper dropper)) dropper.ScaleDrops(_enemyLoot);
            _sent.AddRange(enemies);
        }

        private void HandleDeath() => Destroyed?.Invoke();
    }
}
