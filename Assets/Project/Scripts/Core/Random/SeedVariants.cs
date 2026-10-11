using System;
using System.Collections.Generic;
using System.Globalization;
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

        public SeedVariants() { }

        public SeedVariants(SeedVariants other)
        {
            if (other != null) _entries.AddRange(other._entries);
        }

        // True when every layer is on variant 0, i.e. the plain seed.
        public bool IsEmpty => _entries.TrueForAll(e => e.Variant == 0);

        public int VariantOf(string layer)
        {
            int index = IndexOf(layer);
            return index >= 0 ? _entries[index].Variant : 0;
        }

        public Seed Resolve(Seed parent, string layer) => parent.Derive(layer, VariantOf(layer));

        // A layer's stream; null variants mean every layer is on variant 0.
        public static RandomStream StreamFor(Seed seed, SeedVariants variants, string layer) =>
            (variants != null ? variants.Resolve(seed, layer) : seed.Derive(layer)).Stream();

        public void Reroll(string layer) => Set(layer, VariantOf(layer) + 1);

        public void Set(string layer, int variant)
        {
            int index = IndexOf(layer);
            if (index < 0) _entries.Add(new Entry { Layer = layer, Variant = variant });
            else           _entries[index] = new Entry { Layer = layer, Variant = variant };
        }

        public void Clear() => _entries.Clear();

        // "stats:2 perks:1" — the rerolled layers, in the form TryParse reads back.
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (Entry entry in _entries)
                if (entry.Variant != 0) parts.Add($"{entry.Layer}:{entry.Variant.ToString(CultureInfo.InvariantCulture)}");
            return string.Join(" ", parts);
        }

        // Reads ToString's "layer:variant" tokens, separated by spaces.
        public static bool TryParse(string text, out SeedVariants variants)
        {
            variants = new SeedVariants();
            foreach (string token in (text ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = token.LastIndexOf(':');
                if (colon <= 0) return false;
                if (!int.TryParse(token.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int variant))
                    return false;
                variants.Set(token.Substring(0, colon), variant);
            }
            return true;
        }

        private int IndexOf(string layer) => _entries.FindIndex(e => e.Layer == layer);
    }
}
