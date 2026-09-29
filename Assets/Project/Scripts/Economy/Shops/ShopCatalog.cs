using System;
using UnityEngine;

namespace CGD.Economy
{
    // Everything that defines one kind of vendor: the currency it trades in, how it prices
    // goods, what it stocks and whether it buys the player's things. Vendors sharing a
    // catalog sell the same kind of goods; their seeds keep their shelves different.
    [CreateAssetMenu(fileName = "NewShopCatalog", menuName = "CGD/Economy/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        [SerializeField] private CurrencyDefinition _currency;
        [SerializeField] private PriceTable _prices;
        [Tooltip("Combined in order to fill the shelf on every restock")]
        [SerializeField] private ShopStockSource[] _stock = Array.Empty<ShopStockSource>();

        [Header("Buying from the player")]
        [Tooltip("Lets the player sell items from their pack here")]
        [SerializeField] private bool _buysItems = true;
        [Tooltip("When a bought weapon replaces one in a full loadout, the old one is taken in trade")]
        [SerializeField] private bool _acceptsTradeIns = true;

        public CurrencyDefinition Currency => _currency;
        public PriceTable         Prices   => _prices;
        public ShopStockSource[]  Stock    => _stock;
        public bool BuysItems       => _buysItems;
        public bool AcceptsTradeIns => _acceptsTradeIns;
    }
}
