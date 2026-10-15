using System.Collections.Generic;
using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Enemies;
using CGD.Feedback;

namespace CGD.Director
{
    // The director's wandering threats: while it lets pressure build, a small group of
    // enemies arrives every so often somewhere the player can't see — at a distance, on the
    // NavMesh, with a way to reach them — and comes looking for the player, announced by a
    // sound from where it arrived. During the fade and the calm nothing arrives and the
    // timer waits, so the first group after a calm comes a full interval into the build-up.
    // Higher director Pressure brings bigger groups, sooner.
    public class ReinforcementSpawner : MonoBehaviour
    {
        private const float GroupSpread = 1.5f;

        [SerializeField] private ReinforcementSettings _settings;
        [SerializeField] private GameDirector _director;
        [SerializeField] private Transform    _player;
        [Tooltip("Geometry that hides an arriving group from the player")]
        [SerializeField] private LayerMask    _obstacleMask = 1;

        private readonly List<HealthManager> _alive = new();
        private SpawnSpotFinder _spots;
        private RandomStream    _random;
        private Seed            _seed;
        private CooldownTimer   _nextGroup;
        private int             _spawned;

        private void Awake()
        {
            if (_settings == null || _director == null || _player == null)
            {
                Debug.LogError($"{name}: ReinforcementSpawner needs ReinforcementSettings, the GameDirector and the player.", this);
                enabled = false;
                return;
            }

            _seed   = Seed.Random();
            _random = _seed.Stream();
            _spots  = new SpawnSpotFinder(_random.Fork("spots"), _obstacleMask);
            RestartTimer();
        }

        private void Update()
        {
            if (!_director.AllowsPopulation) return;

            _nextGroup.Tick(Time.deltaTime);
            if (!_nextGroup.IsReady) return;

            RestartTimer();
            SpawnGroup();
        }

        private void RestartTimer() => _nextGroup.Start(_settings.Interval.Evaluate(_random) / _director.Pressure);

        private void SpawnGroup()
        {
            if (_settings.Enemies.Length == 0) return;

            _alive.RemoveAll(IsGone);
            int size  = Mathf.Max(1, Mathf.RoundToInt(_settings.GroupSize.Evaluate(_random) * _director.Pressure));
            int count = Mathf.Min(size, _settings.MaxAlive - _alive.Count);
            if (count <= 0) return;
            if (!_spots.TryFind(_player.position, _settings.Distance, _settings.SpotAttempts, SpotVisibility.Hidden, out Vector3 spot)) return;

            DamageSource lure = DamageSource.Of(_player.gameObject);
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = _random.Pick(_settings.Enemies);
                if (prefab == null) continue;

                GameObject enemy = PrefabPool.Spawn(prefab, _spots.Scatter(spot, GroupSpread), Quaternion.Euler(0f, _random.Range(0, 360), 0f));
                ISeedable.SeedAll(enemy, _seed.Derive("enemies").Derive(_spawned++));
                if (enemy.TryGetComponent(out EnemyAI ai)) ai.AlertTo(_player.position, lure);
                if (enemy.TryGetComponent(out HealthManager health)) _alive.Add(health);
            }
            Announce(spot);
        }

        private void Announce(Vector3 spot)
        {
            if (_settings.ArrivalSound != null && AudioPool.Instance != null)
                AudioPool.Instance.Play(_settings.ArrivalSound, spot, _settings.ArrivalVolume);
            if (!string.IsNullOrEmpty(_settings.ArrivalMessage))
                FeedbackBus.Notify(_settings.ArrivalMessage, NotificationStyle.Warning);
        }

        // Pooled enemies are released (inactive) some time after they die.
        private static bool IsGone(HealthManager enemy) => enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy;
    }
}
