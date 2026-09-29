using CGD.Items;

namespace CGD.Economy
{
    // A bundle of a stackable item (ammo, bandages, crafting materials) into the pack.
    public class StackListing : ShopListing
    {
        public StackListing(ItemDefinition item, int count, int price, int stock) : base(price, stock)
        {
            Item  = item;
            Count = count;
        }

        public ItemDefinition Item  { get; }
        public int            Count { get; }

        public override string   Name => Count > 1 ? $"{Item.DisplayName} ×{Count}" : Item.DisplayName;
        public override ItemTier Tier => Item.Tier;

        public override ItemInstance Deliver(ShopCustomer customer)
        {
            customer.Inventory.Add(Item, Count);
            return null;
        }
    }
}
