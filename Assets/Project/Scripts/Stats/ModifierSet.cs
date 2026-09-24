using System;
using System.Collections.Generic;

namespace CGD.Stats
{
    // Modifiers for many stats, keyed by whatever names the stats (ItemStat for gear and
    // characters). Holds no base values: callers pass the base from wherever it lives
    // (WeaponData, EnemyData…) and get it back with every modifier applied.
    public class ModifierSet<TKey>
    {
        private readonly Dictionary<TKey, ModifierStack> _stacks = new();

        // The stat whose modifiers changed.
        public event Action<TKey> Changed;

        public float Apply(TKey stat, float baseValue) =>
            _stacks.TryGetValue(stat, out ModifierStack stack) ? stack.Apply(baseValue) : baseValue;

        public void Add(TKey stat, Modifier modifier)
        {
            if (!_stacks.TryGetValue(stat, out ModifierStack stack))
                _stacks[stat] = stack = new ModifierStack();

            stack.Add(modifier);
            Changed?.Invoke(stat);
        }

        // Removes every modifier the source added, across all stats.
        public int RemoveFrom(object source)
        {
            int removed = 0;
            foreach (KeyValuePair<TKey, ModifierStack> entry in _stacks)
            {
                int count = entry.Value.RemoveFrom(source);
                if (count == 0) continue;

                removed += count;
                Changed?.Invoke(entry.Key);
            }
            return removed;
        }

        public void Clear()
        {
            foreach (KeyValuePair<TKey, ModifierStack> entry in _stacks)
            {
                if (entry.Value.Count == 0) continue;

                entry.Value.Clear();
                Changed?.Invoke(entry.Key);
            }
        }

        public IReadOnlyList<Modifier> ModifiersOf(TKey stat) =>
            _stacks.TryGetValue(stat, out ModifierStack stack) ? stack.Modifiers : Array.Empty<Modifier>();
    }
}
