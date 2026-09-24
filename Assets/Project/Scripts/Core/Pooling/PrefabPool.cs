using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CGD.Core
{
    // Reuses instances of frequently spawned prefabs (projectiles, grenades, VFX, pickups,
    // enemies) instead of instantiating and destroying them. One pool per prefab, created
    // on first use; idle instances live inactive under a DontDestroyOnLoad root.
    //
    // Components that hold per-use state implement IPoolable to reset themselves. Anything
    // still spawned when the active scene changes is recalled, so a round in flight never
    // survives into the next level.
    public static class PrefabPool
    {
        private static readonly Dictionary<GameObject, Stack<PooledInstance>> _available = new();
        private static readonly HashSet<PooledInstance> _spawned      = new();
        private static readonly List<PooledInstance>    _releaseBuffer = new();
        private static Transform _root;
        private static bool      _listeningForScenes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _available.Clear();
            _spawned.Clear();
            _releaseBuffer.Clear();
            _root = null;

            if (_listeningForScenes) SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            _listeningForScenes = false;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            PooledInstance instance = Take(prefab) ?? Create(prefab, position, rotation);

            Transform t = instance.transform;
            if (parent != null) t.SetParent(parent, false);
            t.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);

            _spawned.Add(instance);
            instance.MarkSpawned();
            return instance.gameObject;
        }

        // Typed convenience for prefabs referenced by their main component.
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component =>
            Spawn(prefab.gameObject, position, rotation, parent).GetComponent<T>();

        // Returns a spawned instance to its pool. Objects that didn't come from the pool are
        // destroyed, so callers can release without knowing how the object was created.
        // Releasing an instance that is already back in the pool does nothing.
        public static void Release(GameObject instance)
        {
            if (instance == null) return;

            if (!instance.TryGetComponent(out PooledInstance pooled))
            {
                Object.Destroy(instance);
                return;
            }

            if (!pooled.IsSpawned) return;

            _spawned.Remove(pooled);
            pooled.MarkDespawned();
            instance.SetActive(false);
            if (instance.transform.parent != Root) instance.transform.SetParent(Root, false);

            _available[pooled.Prefab].Push(pooled);
        }

        // Releases after a delay (e.g. a spent effect or a corpse). Non-pooled objects are
        // destroyed after the same delay.
        public static void Release(GameObject instance, float delay)
        {
            if (instance == null) return;

            if (instance.TryGetComponent(out PooledInstance pooled))
                pooled.ReleaseAfter(delay);
            else
                Object.Destroy(instance, delay);
        }

        // Creates idle instances up front so the first burst of spawns doesn't hitch.
        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;

            Stack<PooledInstance> stack = StackFor(prefab);
            for (int i = stack.Count; i < count; i++)
            {
                PooledInstance instance = Create(prefab, Vector3.zero, Quaternion.identity);
                instance.gameObject.SetActive(false);
                stack.Push(instance);
            }
        }

        // Recalls every spawned instance to its pool.
        public static void ReleaseAll()
        {
            _releaseBuffer.Clear();
            _releaseBuffer.AddRange(_spawned);
            foreach (PooledInstance instance in _releaseBuffer)
                Release(instance.gameObject);
            _releaseBuffer.Clear();
        }

        // Called when a pooled instance is destroyed by something other than the pool
        // (e.g. it was parented into a scene that unloaded).
        internal static void Forget(PooledInstance instance) => _spawned.Remove(instance);

        private static PooledInstance Take(GameObject prefab)
        {
            Stack<PooledInstance> stack = StackFor(prefab);
            while (stack.Count > 0)
            {
                PooledInstance pooled = stack.Pop();
                if (pooled != null) return pooled;
            }
            return null;
        }

        private static PooledInstance Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            ListenForSceneChanges();

            GameObject go = Object.Instantiate(prefab, position, rotation, Root);
            var instance  = go.AddComponent<PooledInstance>();
            instance.Bind(prefab);
            return instance;
        }

        private static Stack<PooledInstance> StackFor(GameObject prefab)
        {
            if (_available.TryGetValue(prefab, out Stack<PooledInstance> stack)) return stack;

            stack = new Stack<PooledInstance>();
            _available[prefab] = stack;
            return stack;
        }

        private static void ListenForSceneChanges()
        {
            if (_listeningForScenes) return;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            _listeningForScenes = true;
        }

        private static void OnActiveSceneChanged(Scene previous, Scene next) => ReleaseAll();

        private static Transform Root
        {
            get
            {
                if (_root != null) return _root;

                var go = new GameObject("PrefabPool");
                Object.DontDestroyOnLoad(go);
                _root = go.transform;
                return _root;
            }
        }
    }
}
