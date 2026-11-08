using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // Brings every enemy under this object back to life a few seconds after it dies, in its
    // own territory (TerritorySpawn: near where it was placed and along its patrol route) at
    // the spot safest from its rivals — so a test scene's factions keep fighting each other
    // indefinitely without coming back inside each other's ground. The enemies must not also
    // despawn on death (DespawnOnDeath).
    public class EnemyRespawner : MonoBehaviour
    {
        [Tooltip("Seconds between an enemy dying and coming back")]
        [SerializeField, Min(0f)] private float _delay = 3f;
        [Tooltip("Metres around its placement and each of its waypoints it may come back within")]
        [SerializeField, Min(1f)] private float _territoryRadius = 6f;
        [Tooltip("Characters outside this object a respawn keeps away from and out of sight of, e.g. the Player. Enemies under it always count")]
        [SerializeField] private HealthManager[] _alsoAvoid;
        [Tooltip("Geometry that blocks line of sight")]
        [SerializeField] private LayerMask _obstacleMask = 1;

        private EnemyHealth[]       _enemies;
        private System.Action[]     _deathHandlers;
        private List<HealthManager> _everyone;

        private void Awake()
        {
            _enemies       = GetComponentsInChildren<EnemyHealth>(true);
            _deathHandlers = new System.Action[_enemies.Length];
            _everyone      = new List<HealthManager>(_enemies);
            if (_alsoAvoid != null)
                foreach (HealthManager other in _alsoAvoid)
                    if (other != null) _everyone.Add(other);

            for (int i = 0; i < _enemies.Length; i++)
            {
                EnemyHealth enemy = _enemies[i];
                var territory = new TerritorySpawn(enemy, _territoryRadius, _obstacleMask);
                _deathHandlers[i] = () => StartCoroutine(Respawn(enemy, territory));
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < _enemies.Length; i++) _enemies[i].OnDeath += _deathHandlers[i];
        }

        private void OnDisable()
        {
            for (int i = 0; i < _enemies.Length; i++)
                if (_enemies[i] != null) _enemies[i].OnDeath -= _deathHandlers[i];
        }

        // The spot is picked when it comes back, not when it fell: by then its rivals have moved.
        private IEnumerator Respawn(EnemyHealth enemy, TerritorySpawn territory)
        {
            yield return new WaitForSeconds(_delay);
            // Brought back meanwhile (a test scenario), or switched off: not ours to revive.
            if (enemy == null || !enemy.IsDead || !enemy.gameObject.activeInHierarchy) yield break;

            EnemyRestart.At(enemy.gameObject, territory.Pick(_everyone), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        }
    }
}
