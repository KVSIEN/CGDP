using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Core
{
    // Per-layer reroll counters on top of a seed. A layer's seed is
    // parent → layer name → variant, so bumping one layer's variant gives that layer
    // fresh numbers while every other layer keeps exactly the numbers it had.
    [Serializable]
    public class SeedVariants
    {
        [Serializable]
        private struct Entry
        {
            public string Layer;
            public int    Variant;
        }

        [SerializeField] private List<Entry> _entries = new();

        public int VariantOf(string layer)
        {
            int index = IndexOf(layer);
            return index >= 0 ? _entries[index].Variant : 0;
        }

        public Seed Resolve(Seed parent, string layer) => parent.Derive(layer, VariantOf(layer));

        public void Reroll(string layer) => Set(layer, VariantOf(layer) + 1);

        public void Set(string layer, int variant)
        {
            int index = IndexOf(layer);
            if (index < 0) _entries.Add(new Entry { Layer = layer, Variant = variant });
            else           _entries[index] = new Entry { Layer = layer, Variant = variant };
        }

        public void Clear() => _entries.Clear();

        private int IndexOf(string layer) => _entries.FindIndex(e => e.Layer == layer);
    }
}
