using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using CGD.Economy;
using CGD.Feedback;
using CGD.Items;

namespace CGD.UI
{
    // Opens when a Vendor is opened for the player. Left: the vendor's shelf, each row
    // clickable when the player can buy it (a trade-in shows its credit). Right: what the
    // player can sell from their pack — stackables go in the smallest bundle worth a coin,
    // or the whole stack with Shift held. The balance sits in the title.
    public class ShopPanel : ModalPanel
    {
        private readonly StringBuilder _line = new();

        private UIButtonList _buyList;
        private UIButtonList _sellList;
        private Vendor       _vendor;
        private ShopCustomer _customer;

        protected override string  Title      => "Shop";
        protected override Vector2 WindowSize => new(860f, 520f);

        private Shop Shop => _vendor.Shop;

        protected override void Build(RectTransform content)
        {
            (RectTransform left, RectTransform right) = Columns(content);
            _buyList  = new UIButtonList(left, 28f);
            _sellList = new UIButtonList(right, 28f);
        }

        private void OnEnable() => Vendor.Opened += OnVendorOpened;

        protected override void OnDisable()
        {
            Vendor.Opened -= OnVendorOpened;
            base.OnDisable();
        }

        private void OnVendorOpened(Vendor vendor, ShopCustomer customer)
        {
            if (IsBlocked(this)) return;

            _vendor   = vendor;
            _customer = customer;
            Show();
        }

        protected override void OnOpened()
        {
            Shop.Changed               += Refresh;
            _customer.Inventory.Changed += Refresh;
            Refresh();
        }

        protected override void OnClosed()
        {
            if (_vendor != null)   Shop.Changed               -= Refresh;
            if (_customer != null) _customer.Inventory.Changed -= Refresh;
            _vendor   = null;
            _customer = null;
        }

        public override void Refresh()
        {
            if (!IsVisible || _vendor == null) return;

            CurrencyDefinition currency = Shop.Currency;
            int balance = Wallet.Balance(_customer.Inventory, currency);
            SetTitle($"{_vendor.DisplayName}   <color=#AAAAAA>·</color>   {Format(currency, balance)}");

            RefreshBuyList(currency);
            RefreshSellList(currency);
        }

        // --- Buying -----------------------------------------------------------------------

        private void RefreshBuyList(CurrencyDefinition currency)
        {
            _buyList.Begin();
            _buyList.Heading("For sale");
            if (Shop.Listings.Count == 0) _buyList.Label("Sold out.");

            foreach (ShopListing listing in Shop.Listings)
            {
                PurchaseQuote quote = Shop.Quote(listing, _customer);
                _buyList.Add(Describe(listing, quote, currency), () => Buy(listing), quote.IsOk);
            }
            _buyList.End();
        }

        private void Buy(ShopListing listing)
        {
            if (!Shop.TryBuy(listing, _customer, out PurchaseQuote quote)) return;

            string message = quote.TradeIn != null
                ? $"Bought {listing.Name} (traded in {quote.TradeIn.DisplayName})"
                : $"Bought {listing.Name}";
            FeedbackBus.Notify(message, NotificationStyle.Reward);
        }

        // "<tier colour>Name</colour>  details   120 cr   (trades in Rifle +30)"
        private string Describe(ShopListing listing, in PurchaseQuote quote, CurrencyDefinition currency)
        {
            _line.Clear();
            _line.Append("<color=").Append(TierColors.RichHex(listing.Tier)).Append('>').Append(listing.Name).Append("</color>");
            if (!string.IsNullOrEmpty(listing.Details)) _line.Append("  <color=#AAAAAA>").Append(listing.Details).Append("</color>");
            _line.Append("   <b>").Append(Format(currency, listing.Price)).Append("</b>");
            if (listing.Remaining > 1) _line.Append("  <color=#AAAAAA>×").Append(listing.Remaining).Append("</color>");

            switch (quote.Status)
            {
                case PurchaseStatus.CantTake:
                    _line.Append("  <color=#FF8866>").Append(quote.Reason).Append("</color>");
                    break;
                case PurchaseStatus.CantAfford:
                    _line.Append("  <color=#FF8866>can't afford</color>");
                    break;
            }

            if (quote.TradeIn != null)
                _line.Append("  <color=#AAAAAA>(trades in ").Append(quote.TradeIn.DisplayName)
                     .Append(" +").Append(quote.TradeInCredit).Append(")</color>");

            return _line.ToString();
        }

        // --- Selling ----------------------------------------------------------------------

        private void RefreshSellList(CurrencyDefinition currency)
        {
            _sellList.Begin();
            _sellList.Heading("Your pack");

            if (!Shop.BuysItems)
            {
                _sellList.Label("Doesn't buy anything.");
                _sellList.End();
                return;
            }

            int shown = 0;
            foreach (ItemStack stack in _customer.Inventory.Stacks)
            {
                int bundle = Shop.SellBundle(stack.Definition);
                if (bundle <= 0 || stack.Count < bundle) continue;

                ItemDefinition item = stack.Definition;
                int price = Shop.SellPrice(item, bundle);
                string text = bundle > 1
                    ? $"{item.DisplayName} ×{stack.Count}   sell ×{bundle} for {Format(currency, price)}"
                    : $"{item.DisplayName} ×{stack.Count}   sell for {Format(currency, price)}";
                _sellList.Add(text, () => SellStack(item, bundle));
                shown++;
            }

            foreach (ItemInstance item in _customer.Inventory.Items)
            {
                int price = Shop.SellPrice(item);
                if (price <= 0) continue;

                _sellList.Add($"<color={TierColors.RichHex(item.Tier)}>{item.DisplayName}</color>   sell for {Format(currency, price)}",
                    () => SellItem(item));
                shown++;
            }

            if (shown == 0) _sellList.Label("Nothing they'll buy.");
            _sellList.End();
        }

        // Shift sells every whole bundle in the pack at once.
        private void SellStack(ItemDefinition item, int bundle)
        {
            bool all  = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            int count = all ? _customer.Inventory.CountOf(item) / bundle * bundle : bundle;
            if (count > 0) Shop.TrySell(item, count, _customer);
        }

        private void SellItem(ItemInstance item)
        {
            if (Shop.TrySell(item, _customer))
                FeedbackBus.Notify($"Sold {item.DisplayName}", NotificationStyle.Info);
        }

        private static string Format(CurrencyDefinition currency, int amount) =>
            currency != null ? currency.Format(amount) : amount.ToString("N0");
    }
}
