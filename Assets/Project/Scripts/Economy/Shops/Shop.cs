using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Items;

namespace CGD.Economy
{
    // One vendor's trading rules and current shelf, independent of Unity scenes and UI:
    // quotes, purchases (with trade-ins for goods that displace something) and buying items
    // back from the customer. Every change to the shelf raises Changed.
    public class Shop
    {
        private readonly ShopCatalog _catalog;
        private readonly PriceTable _prices;
        private readonly List<ShopListing> _listings = new();

        public Shop(ShopCatalog catalog, PriceTable fallbackPrices)
        {
            _catalog = catalog;
            _prices  = catalog.Prices != null ? catalog.Prices : fallbackPrices;
        }

        public CurrencyDefinition        Currency  => _catalog.Currency;
        public IReadOnlyList<ShopListing> Listings => _listings;
        public bool                      BuysItems => _catalog.BuysItems;

        public event Action Changed;

        // Replaces the whole shelf. The same seed gives the same shelf.
        public void Restock(Seed seed)
        {
            _listings.Clear();
            ShopStockSource[] sources = _catalog.Stock;
            for (int i = 0; i < sources.Length; i++)
                if (sources[i] != null) sources[i].Stock(_listings, seed.Derive(i).Stream(), _prices);

            Changed?.Invoke();
        }

        // --- Buying -----------------------------------------------------------------------

        public PurchaseQuote Quote(ShopListing listing, ShopCustomer customer)
        {
            if (!listing.InStock) return new PurchaseQuote(PurchaseStatus.SoldOut, listing.Price, null, 0);

            string reason = listing.CannotDeliverReason(customer);
            if (reason != null) return new PurchaseQuote(PurchaseStatus.CantTake, listing.Price, null, 0, reason);

            ItemInstance tradeIn = listing.WouldDisplace(customer);
            if (tradeIn != null && !_catalog.AcceptsTradeIns)
                return new PurchaseQuote(PurchaseStatus.CantTake, listing.Price, null, 0, "No free slot");

            int credit = tradeIn != null ? _prices.SellPrice(tradeIn) : 0;
            var quote = new PurchaseQuote(PurchaseStatus.Ok, listing.Price, tradeIn, credit);

            return Wallet.CanAfford(customer.Inventory, Currency, quote.NetCost)
                ? quote
                : new PurchaseQuote(PurchaseStatus.CantAfford, listing.Price, tradeIn, credit);
        }

        // Pays, hands the goods over and credits any trade-in. Nothing changes unless the
        // quote is Ok.
        public bool TryBuy(ShopListing listing, ShopCustomer customer, out PurchaseQuote quote)
        {
            quote = Quote(listing, customer);
            if (!quote.IsOk || !Wallet.TryPay(customer.Inventory, Currency, quote.NetCost)) return false;

            listing.TakeOne();
            listing.Deliver(customer);
            if (quote.NetCost < 0) Wallet.Earn(customer.Inventory, Currency, -quote.NetCost);
            if (!listing.InStock) _listings.Remove(listing);

            Changed?.Invoke();
            return true;
        }

        // --- Selling ----------------------------------------------------------------------

        // 0 = the shop won't take it. Money itself is never for sale.
        public int SellPrice(ItemDefinition item, int count) =>
            CanBuyFromCustomer(item) ? _prices.SellPrice(item, count) : 0;

        // How many units of a stackable to sell at once so the sale is worth at least 1.
        public int SellBundle(ItemDefinition item) => CanBuyFromCustomer(item) ? _prices.SellBundle(item) : 0;

        public int SellPrice(ItemInstance item) =>
            item != null && CanBuyFromCustomer(item.Definition) ? _prices.SellPrice(item) : 0;

        public bool TrySell(ItemDefinition item, int count, ShopCustomer customer)
        {
            int price = SellPrice(item, count);
            if (price <= 0 || !customer.Inventory.Remove(item, count)) return false;

            Wallet.Earn(customer.Inventory, Currency, price);
            return true;
        }

        public bool TrySell(ItemInstance item, ShopCustomer customer)
        {
            int price = SellPrice(item);
            if (price <= 0 || !customer.Inventory.Remove(item)) return false;

            Wallet.Earn(customer.Inventory, Currency, price);
            return true;
        }

        private bool CanBuyFromCustomer(ItemDefinition item) =>
            _catalog.BuysItems && item != null && item is not CurrencyDefinition;
    }
}
