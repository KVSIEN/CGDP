using System;
using UnityEngine;
using CGD.Core;
using CGD.Items;

namespace CGD.Loot
{
    // One line of a LootTable. Kind picks which reference is used; the others are ignored.
    [Serializable]
    public class LootEntry
    {
        public LootEntryKind Kind = LootEntryKind.Item;
        public ItemDefinition Item;
        public LootTable      Table;
        public GameObject     Prefab;

        [Tooltip("How many: stack size for stackables, pieces for gear, rolls for tables, copies for prefabs")]
        public IntRange Count = new(1, 1);

        [Tooltip("Relative chance in the weighted pool (ignored for guaranteed drops)")]
        [Min(0f)] public float Weight = 1f;

        public bool IsValid => Kind switch
        {
            LootEntryKind.Item   => Item != null,
            LootEntryKind.Table  => Table != null,
            LootEntryKind.Prefab => Prefab != null,
            _                    => false,
        };

        // At least one: an entry that was picked should drop something.
        public int RollCount() => Mathf.Max(1, Count.Evaluate());
    }
}
