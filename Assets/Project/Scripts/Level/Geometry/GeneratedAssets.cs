using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // The meshes, materials and textures a level build creates at runtime. Unity doesn't free
    // them with the GameObjects that use them, and a generated level is rebuilt on every
    // restart, so the builder that owns the level destroys them when it goes.
    public class GeneratedAssets
    {
        private readonly List<Object> _assets = new();

        public T Track<T>(T asset) where T : Object
        {
            if (asset != null) _assets.Add(asset);
            return asset;
        }

        public void DestroyAll()
        {
            foreach (Object asset in _assets)
                if (asset != null) Object.Destroy(asset);
            _assets.Clear();
        }
    }
}
