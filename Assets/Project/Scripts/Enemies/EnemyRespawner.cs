using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using CGD.Core;

namespace CGD.Enemies
{
    // Brings every enemy under this object back to life a few seconds after it dies, at a
    // random walkable spot anywhere on the NavMesh — so a test scene's enemies keep fighting
    // each other indefinitely. The enemies must not also despawn on death (DespawnOnDeath).
    public class EnemyRespawner : MonoBehaviour
    {
        private const int SpotAttempts = 16;

        [Tooltip("Seconds between an enemy dying and coming back")]
        [SerializeField, Min(0f)] private float _delay = 3f;

        private EnemyHealth[]   _enemies;
        private System.Action[] _deathHandlers;
        private float[]         _cumulativeAreas;
        private NavMeshTriangulation _navMesh;
        private NavMeshPath     _path;

        private void Awake()
        {
            _path          = new NavMeshPath();
            _enemies       = GetComponentsInChildren<EnemyHealth>(true);
            _deathHandlers = new System.Action[_enemies.Length];

            for (int i = 0; i < _enemies.Length; i++)
            {
                EnemyHealth enemy = _enemies[i];
                Vector3     home  = enemy.transform.position;
                _deathHandlers[i] = () => StartCoroutine(Respawn(enemy, home));
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

        private IEnumerator Respawn(EnemyHealth enemy, Vector3 home)
        {
            yield return new WaitForSeconds(_delay);
            if (enemy == null || !enemy.IsDead) yield break;

            enemy.transform.SetPositionAndRotation(RandomSpot(home), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            // Coming back is the same reset a pooled enemy gets when it's reused.
            foreach (IPoolable poolable in enemy.GetComponentsInChildren<IPoolable>(true))
                poolable.OnSpawned();
        }

        // A random point on the NavMesh (weighted by area, so every square metre is equally
        // likely) that can be walked to from where the enemy was placed — never a crate top
        // or other island it would be stuck on. Falls back to that original spot.
        private Vector3 RandomSpot(Vector3 home)
        {
            if (_cumulativeAreas == null) CacheNavMesh();
            if (_cumulativeAreas.Length == 0) return home;

            for (int attempt = 0; attempt < SpotAttempts; attempt++)
            {
                Vector3 spot = RandomPointOnNavMesh();
                if (NavMesh.CalculatePath(home, spot, NavMesh.AllAreas, _path) && _path.status == NavMeshPathStatus.PathComplete)
                    return spot;
            }
            return home;
        }

        private Vector3 RandomPointOnNavMesh()
        {
            float pick = Random.value * _cumulativeAreas[^1];
            int triangle = System.Array.BinarySearch(_cumulativeAreas, pick);
            if (triangle < 0) triangle = ~triangle;
            triangle = Mathf.Min(triangle, _cumulativeAreas.Length - 1);

            Vector3 a = _navMesh.vertices[_navMesh.indices[triangle * 3]];
            Vector3 b = _navMesh.vertices[_navMesh.indices[triangle * 3 + 1]];
            Vector3 c = _navMesh.vertices[_navMesh.indices[triangle * 3 + 2]];

            float u = Random.value, v = Random.value;
            if (u + v > 1f) { u = 1f - u; v = 1f - v; }
            return a + (b - a) * u + (c - a) * v;
        }

        // Read once, on the first respawn: the NavMesh is built at runtime when the scene loads.
        private void CacheNavMesh()
        {
            _navMesh = NavMesh.CalculateTriangulation();
            int triangles = _navMesh.indices.Length / 3;
            _cumulativeAreas = new float[triangles];

            float total = 0f;
            for (int i = 0; i < triangles; i++)
            {
                Vector3 a = _navMesh.vertices[_navMesh.indices[i * 3]];
                Vector3 b = _navMesh.vertices[_navMesh.indices[i * 3 + 1]];
                Vector3 c = _navMesh.vertices[_navMesh.indices[i * 3 + 2]];
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                _cumulativeAreas[i] = total;
            }
        }
    }
}
