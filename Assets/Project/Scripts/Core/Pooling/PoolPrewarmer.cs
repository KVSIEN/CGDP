using System;
using UnityEngine;

namespace CGD.Core
{
    // Fills PrefabPool with idle instances when the scene starts, so the first shots,
    // explosions or enemy waves don't pay for Instantiate mid-fight. Optional — pools
    // grow on demand without it.
    public class PoolPrewarmer : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public GameObject Prefab;
            [Min(1)] public int Count;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private void Start()
        {
            foreach (Entry entry in _entries)
                PrefabPool.Prewarm(entry.Prefab, entry.Count);
        }
    }
}
