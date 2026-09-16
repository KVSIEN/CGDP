using System.Collections.Generic;
using UnityEngine;

namespace CGD.Core
{
    // Reuses instances of frequently spawned prefabs (projectiles, grenades) instead of
    // instantiating and destroying them. Instances live under a DontDestroyOnLoad root;
    // any that were destroyed externally are skipped. Spawned objects must reset their
    // own state when reused (OnEnable or an init method).
    public static class PrefabPool
    {
        private static readonly Dictionary<GameObject, Stack<GameObject>> _available = new();
        private static readonly Dictionary<GameObject, GameObject> _prefabOf = new(); // instance → prefab
        private static Transform _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _available.Clear();
            _prefabOf.Clear();
            _root = null;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_available.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                _available[prefab] = stack;
            }

            while (stack.Count > 0)
            {
                GameObject pooled = stack.Pop();
                if (pooled == null) continue;

                pooled.transform.SetPositionAndRotation(position, rotation);
                pooled.SetActive(true);
                return pooled;
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation, Root);
            _prefabOf[instance] = prefab;
            return instance;
        }

        // Returns a spawned instance to its pool; objects that didn't come from the pool are destroyed.
        public static void Release(GameObject instance)
        {
            if (!_prefabOf.TryGetValue(instance, out GameObject prefab))
            {
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            _available[prefab].Push(instance);
        }

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
