using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Economy
{
    // Fills part of a vendor's shelf each time it restocks. Each kind of goods is its own
    // source asset (fixed items, rolled weapons…), and a ShopCatalog combines several, so
    // new kinds of stock are a new subclass rather than a change to the shop.
    public abstract class ShopStockSource : ScriptableObject
    {
        // Adds this restock's listings. Everything random comes from `random`, so the same
        // seed restocks the same shelf.
        public abstract void Stock(List<ShopListing> listings, RandomStream random, PriceTable prices);
    }
}
