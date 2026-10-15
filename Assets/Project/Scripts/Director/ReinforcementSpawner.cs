using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;
using CGD.Enemies;

namespace CGD.Director
{
    // The director's wandering threats: while it lets pressure build, a small group of
    // enemies arrives every so often somewhere the player can't see — at a distance, on the
    // NavMesh, with a way to reach them — and comes looking for the player. During the fade
    // and the calm nothing arrives and the timer waits, so the first group after a calm
    // comes a full interval into the build-up.
    public class ReinforcementSpawner : MonoBehaviour
    {
        private const float EyeHeight      = 1.6f;
        private const float NavMeshSearch  = 4f;
        private const float GroupSpread    = 1.5f;

        [SerializeField] private ReinforcementSettings _settings;
        [SerializeField] private GameDirector _director;
        [SerializeField] private Transform    _player;
        [Tooltip("Geometry that hides an arriving group from the player")]
        [SerializeField] private LayerMask    _obstacleMask = 1;

        private readonly List<HealthManager> _alive = new();
        private NavMeshPath   _path;
        private RandomStream  _random;
        private Seed          _seed;
        private CooldownTimer _nextGroup;
        private int           _spawned;

        private void Awake()
        {
            if (_settings == null || _director == null || _player == null)
            {
                Debug.LogError($"{name}: ReinforcementSpawner needs ReinforcementSettings, the GameDirector and the player.", this);
                enabled = false;
                return;
            }

            _path   = new NavMeshPath();
            _seed   = Seed.Random();
            _random = _seed.Stream();
            _nextGroup.Start(_settings.Interval.Evaluate(_random));
        }

        private void Update()
        {
            if (!_director.AllowsPopulation) return;

            _nextGroup.Tick(Time.deltaTime);
            if (!_nextGroup.IsReady) return;

            _nextGroup.Start(_settings.Interval.Evaluate(_random));
            SpawnGroup();
        }

        private void SpawnGroup()
        {
            if (_settings.Enemies.Length == 0) return;

            _alive.RemoveAll(IsGone);
            int count = Mathf.Min(_settings.GroupSize.Evaluate(_random), _settings.MaxAlive - _alive.Count);
            if (count <= 0 || !TryFindSpot(out Vector3 spot)) return;

            DamageSource lure = DamageSource.Of(_player.gameObject);
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = _random.Pick(_settings.Enemies);
                if (prefab == null) continue;

                GameObject enemy = PrefabPool.Spawn(prefab, Spread(spot), Quaternion.Euler(0f, _random.Range(0, 360), 0f));
                ISeedable.SeedAll(enemy, _seed.Derive("enemies").Derive(_spawned++));
                if (enemy.TryGetComponent(out EnemyAI ai)) ai.AlertTo(_player.position, lure);
                if (enemy.TryGetComponent(out HealthManager health)) _alive.Add(health);
            }
        }

        // Pooled enemies are released (inactive) some time after they die.
        private static bool IsGone(HealthManager enemy) => enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy;

        private bool TryFindSpot(out Vector3 spot)
        {
            spot = default;
            if (!NavMesh.SamplePosition(_player.position, out NavMeshHit playerHit, NavMeshSearch, NavMesh.AllAreas)) return false;

            for (int i = 0; i < _settings.SpotAttempts; i++)
            {
                Vector2 direction = _random.InsideUnitCircle().normalized;
                if (direction == Vector2.zero) continue;

                Vector3 candidate = _player.position + new Vector3(direction.x, 0f, direction.y) * _settings.Distance.Evaluate(_random);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSearch, NavMesh.AllAreas)) continue;
                if (IsVisible(hit.position)) continue;
                if (!NavMesh.CalculatePath(hit.position, playerHit.position, NavMesh.AllAreas, _path) || _path.status != NavMeshPathStatus.PathComplete) continue;

                spot = hit.position;
                return true;
            }
            return false;
        }

        private bool IsVisible(Vector3 position) =>
            !Physics.Linecast(_player.position + Vector3.up * EyeHeight, position + Vector3.up * EyeHeight,
                              _obstacleMask, QueryTriggerInteraction.Ignore);

        private Vector3 Spread(Vector3 spot)
        {
            Vector2 offset = _random.InsideUnitCircle() * GroupSpread;
            Vector3 position = spot + new Vector3(offset.x, 0f, offset.y);
            return NavMesh.SamplePosition(position, out NavMeshHit hit, GroupSpread, NavMesh.AllAreas) ? hit.position : spot;
        }
    }
}
