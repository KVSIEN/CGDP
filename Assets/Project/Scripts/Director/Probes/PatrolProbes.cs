using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Core;
using CGD.Enemies;

namespace CGD.Director
{
    // Pressure from enemies already in the level, not only new arrivals: during a build-up the
    // director now and then sends one calm enemy some way off to have a look round a rough
    // area near the player — never the player's position, so it may well walk past them. It
    // goes as a mildly curious enemy (an investigation, not a hunt), and only enemies with
    // nothing else on their mind are picked.
    public class PatrolProbes : MonoBehaviour
    {
        private const float NavMeshSnap = 3f;

        [SerializeField] private PatrolProbeSettings _settings;
        [SerializeField] private GameDirector _director;
        [SerializeField] private Transform    _player;
        [Tooltip("Layers enemies' colliders are on")]
        [SerializeField] private LayerMask    _enemyMask = ~0;

        private static readonly Collider[] Nearby = new Collider[64];
        private readonly HashSet<EnemyAI> _seen = new();
        private RandomStream  _random;
        private CooldownTimer _nextProbe;
        private int           _sentThisBuildUp;

        private void Awake()
        {
            if (_settings == null || _director == null || _player == null)
            {
                Debug.LogError($"{name}: PatrolProbes needs PatrolProbeSettings, the GameDirector and the player.", this);
                enabled = false;
                return;
            }

            _random = Seed.Random().Stream();
            RestartTimer();
        }

        private void OnEnable()
        {
            if (_director != null) _director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_director != null) _director.PhaseChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            if (_director.Phase != PacingPhase.BuildUp || !_director.AllowsPopulation) return;
            if (_sentThisBuildUp >= _settings.PerBuildUp) return;

            _nextProbe.Tick(Time.deltaTime);
            if (!_nextProbe.IsReady) return;

            RestartTimer();
            if (TrySendProbe()) _sentThisBuildUp++;
        }

        private void OnPhaseChanged(PacingPhase previous, PacingPhase next)
        {
            if (next != PacingPhase.BuildUp) return;
            _sentThisBuildUp = 0;
            RestartTimer();
        }

        private void RestartTimer() => _nextProbe.Start(_settings.Interval.Evaluate(_random) / _director.Pressure);

        private bool TrySendProbe()
        {
            EnemyAI enemy = PickCalmEnemy();
            return enemy != null && TryPickArea(out Vector3 area) && enemy.CheckArea(area);
        }

        // A random calm enemy within the distance band (each one counted once, however many colliders it has).
        private EnemyAI PickCalmEnemy()
        {
            Vector3 player = _player.position;
            FloatRange band = _settings.EnemyDistance;
            int count = Physics.OverlapSphereNonAlloc(player, band.Max, Nearby, _enemyMask, QueryTriggerInteraction.Ignore);

            _seen.Clear();
            EnemyAI picked = null;
            int candidates = 0;
            for (int i = 0; i < count; i++)
            {
                EnemyAI enemy = Nearby[i].GetComponentInParent<EnemyAI>();
                if (enemy == null || !_seen.Add(enemy) || !enemy.isActiveAndEnabled) continue;
                if (enemy.Awareness != AwarenessLevel.Unaware) continue;
                if ((enemy.transform.position - player).sqrMagnitude < band.Min * band.Min) continue;

                // Reservoir pick: every candidate equally likely, without a list.
                if (_random.Range(0, ++candidates) == 0) picked = enemy;
            }
            return picked;
        }

        private bool TryPickArea(out Vector3 area)
        {
            float angle  = _random.Range(0f, 360f);
            Vector3 spot = _player.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * _settings.AreaOffset.Evaluate(_random);
            bool found   = NavMesh.SamplePosition(spot, out NavMeshHit hit, NavMeshSnap, NavMesh.AllAreas);
            area = found ? hit.position : default;
            return found;
        }
    }
}
