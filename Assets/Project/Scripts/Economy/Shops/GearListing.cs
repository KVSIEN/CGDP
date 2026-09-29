using CGD.Items;

namespace CGD.Economy
{
    // One rolled piece of non-weapon gear (armor) into the pack, ready to equip.
    public class GearListing : ShopListing
    {
        public GearListing(ItemInstance item, int price) : base(price, 1) => Item = item;

        public ItemInstance Item { get; }

        public override string   Name    => Item.DisplayName;
        public override ItemTier Tier    => Item.Tier;
        public override string   Details => $"Q{Item.Quality}";

        public override ItemInstance Deliver(ShopCustomer customer)
        {
            customer.Inventory.Add(Item);
            return null;
        }
    }
}
