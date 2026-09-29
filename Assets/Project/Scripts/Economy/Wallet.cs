using CGD.Items;

namespace CGD.Economy
{
    // Currency operations over an Inventory. All-or-nothing, like Inventory itself.
    public static class Wallet
    {
        public static int Balance(Inventory inventory, CurrencyDefinition currency) =>
            inventory != null && currency != null ? inventory.CountOf(currency) : 0;

        public static bool CanAfford(Inventory inventory, CurrencyDefinition currency, int amount) =>
            amount <= 0 || Balance(inventory, currency) >= amount;

        public static bool TryPay(Inventory inventory, CurrencyDefinition currency, int amount)
        {
            if (amount <= 0) return true;
            return inventory != null && currency != null && inventory.Remove(currency, amount);
        }

        public static void Earn(Inventory inventory, CurrencyDefinition currency, int amount)
        {
            if (amount > 0 && inventory != null && currency != null) inventory.Add(currency, amount);
        }
    }
}
