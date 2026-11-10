using System.Collections.Generic;
using UnityEngine;

namespace CGD.Core
{
    // A static cache keyed by scene objects outlives the scene: once a level unloads, its keys
    // only compare equal to null and are never looked up again, so without pruning the cache
    // grows with every level load. Pooled objects survive loads and keep their entries.
    public static class DestroyedKeys
    {
        public static void Remove<TKey, TValue>(Dictionary<TKey, TValue> cache) where TKey : Object
        {
            List<TKey> destroyed = null;
            foreach (TKey key in cache.Keys)
                if (key == null) (destroyed ??= new List<TKey>()).Add(key);

            if (destroyed == null) return;
            foreach (TKey key in destroyed) cache.Remove(key);
        }
    }
}
