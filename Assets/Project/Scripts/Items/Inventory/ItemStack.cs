namespace CGD.Items
{
    // A quantity of one stackable definition. Immutable: changing a count produces a
    // new stack, so a stack can never be mutated behind the inventory's back.
    public readonly struct ItemStack
    {
        public readonly ItemDefinition Definition;
        public readonly int            Count;

        public ItemStack(ItemDefinition definition, int count)
        {
            Definition = definition;
            Count      = count;
        }

        public bool IsEmpty => Definition == null || Count <= 0;

        public int SpaceRemaining => Definition != null ? Definition.MaxStack - Count : 0;

        public ItemStack WithCount(int count) => new(Definition, count);
    }
}
