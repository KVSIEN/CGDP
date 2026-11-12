using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // Starts an enemy over somewhere: alive at full health, calm, knowing nothing, its tree
    // from the top — the same reset a pooled enemy gets when it is reused (every IPoolable on
    // it). Used for respawns (EnemyRespawner) and for setting up test scenarios.
    public static class EnemyRestart
    {
        public static void At(GameObject enemy, Vector3 position, Quaternion rotation)
        {
            enemy.transform.SetPositionAndRotation(position, rotation);
            foreach (IPoolable poolable in enemy.GetComponentsInChildren<IPoolable>(true))
                poolable.OnSpawned();
        }
    }
}
