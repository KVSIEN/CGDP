using UnityEngine;
using CGD.Items;

namespace CGD.Economy
{
    // Money is an item: it lives in the player's Inventory as a stack, so loot tables,
    // pickups, quest rewards and the dev console's /give all hand it out without knowing
    // it's special. Several currencies can coexist (credits, faction scrip); each shop
    // names the one it trades in.
    [CreateAssetMenu(fileName = "NewCurrency", menuName = "CGD/Economy/Currency")]
    public class CurrencyDefinition : ItemDefinition
    {
        [Header("Currency")]
        [Tooltip("Short suffix after amounts, e.g. \"cr\"")]
        [SerializeField] private string _symbol = "cr";

        public string Symbol => _symbol;

        // Large enough that a wallet is always a single stack.
        public override int MaxStack => 1_000_000_000;

        public string Format(int amount) => $"{amount:N0} {_symbol}";
    }
}
