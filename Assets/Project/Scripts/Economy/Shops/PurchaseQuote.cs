using CGD.Items;

namespace CGD.Economy
{
    // What buying a listing would cost this customer right now, trade-in included, and
    // whether they can. The shop panel shows it; Shop.TryBuy acts on it.
    public readonly struct PurchaseQuote
    {
        public PurchaseQuote(PurchaseStatus status, int price, ItemInstance tradeIn, int tradeInCredit, string reason = null)
        {
            Status        = status;
            Price         = price;
            TradeIn       = tradeIn;
            TradeInCredit = tradeInCredit;
            Reason        = reason;
        }

        public PurchaseStatus Status        { get; }
        public int            Price         { get; }
        // What the purchase pushes out of the customer's hands, and what the shop pays for it.
        public ItemInstance   TradeIn       { get; }
        public int            TradeInCredit { get; }
        // Only for CantTake.
        public string         Reason        { get; }

        public bool IsOk     => Status == PurchaseStatus.Ok;
        // What leaves the wallet; negative when the trade-in is worth more than the purchase.
        public int  NetCost  => Price - TradeInCredit;
    }
}
