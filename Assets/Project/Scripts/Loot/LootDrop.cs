using UnityEngine;
using CGD.Items;

namespace CGD.Loot
{
    // One concrete result of rolling a LootTable — what actually dropped. Plain data, so
    // the same result can become world pickups, go straight into an inventory, or fill
    // a reward screen.
    public readonly struct LootDrop
    {
        public readonly LootDropKind   Kind;
        public readonly ItemDefinition Definition;
        public readonly int            Count;
        public readonly ItemInstance   Instance;
        public readonly GameObject     Prefab;

        private LootDrop(LootDropKind kind, ItemDefinition definition, int count, ItemInstance instance, GameObject prefab)
        {
            Kind       = kind;
            Definition = definition;
            Count      = count;
            Instance   = instance;
            Prefab     = prefab;
        }

        public static LootDrop Stack(ItemDefinition definition, int count) =>
            new(LootDropKind.Stack, definition, count, null, null);

        public static LootDrop Gear(ItemInstance instance) =>
            new(LootDropKind.Instance, instance.Definition, 1, instance, null);

        public static LootDrop Spawn(GameObject prefab) =>
            new(LootDropKind.Prefab, null, 1, null, prefab);

        public override string ToString() => Kind switch
        {
            LootDropKind.Stack    => $"{Definition.DisplayName} ×{Count}",
            LootDropKind.Instance => Instance.ToString(),
            _                     => Prefab != null ? Prefab.name : "Nothing",
        };
    }
}
