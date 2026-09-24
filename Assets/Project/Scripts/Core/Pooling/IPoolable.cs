namespace CGD.Core
{
    // Implemented by components on a pooled prefab that hold per-use state. PrefabPool
    // calls these on every IPoolable in the instance's hierarchy, so a reused object
    // starts clean without relying on OnEnable ordering. Implement only what you need.
    public interface IPoolable
    {
        // After the instance is positioned and activated.
        void OnSpawned() { }

        // Before the instance is deactivated and returned to its pool.
        void OnDespawned() { }
    }
}
