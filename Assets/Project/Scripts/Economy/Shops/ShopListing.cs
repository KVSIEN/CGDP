using CGD.Items;

namespace CGD.Economy
{
    // One thing on a vendor's shelf: what it is, what it costs and how many are left.
    // Subclasses decide how the goods reach the customer (into the pack, into a weapon
    // slot…), so new kinds of goods only need a new listing and a stock source to make it.
    public abstract class ShopListing
    {
        public const int Unlimited = -1;

        protected ShopListing(int price, int stock)
        {
            Price     = price;
            Remaining = stock;
        }

        public abstract string   Name { get; }
        public abstract ItemTier Tier { get; }
        // Short extra line (tier, quality, bundle size); empty for none.
        public virtual string Details => string.Empty;

        public int  Price     { get; }
        public int  Remaining { get; private set; }
        public bool InStock   => Remaining != 0;

        // Why this customer can't take it (no weapon slots to put a gun in), or null.
        public virtual string CannotDeliverReason(ShopCustomer customer) => null;

        // What delivering would push out of the customer's hands to make room — a weapon
        // replaced in a full loadout. The shop buys it back. Null when nothing moves.
        public virtual ItemInstance WouldDisplace(ShopCustomer customer) => null;

        // Hands the goods over and returns whatever got displaced.
        public abstract ItemInstance Deliver(ShopCustomer customer);

        internal void TakeOne()
        {
            if (Remaining > 0) Remaining--;
        }
    }
}
