using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Economy
{
    // A fixed list of goods: stackables sold in bundles (30 rounds, 2 bandages), and gear
    // definitions rolled fresh on every restock.
    [CreateAssetMenu(fileName = "NewItemStock", menuName = "CGD/Economy/Stock/Item Stock")]
    public class ItemStock : ShopStockSource
    {
        [Serializable]
        public struct Entry
        {
            public ItemDefinition Item;
            [Tooltip("Units per purchase (stackables only)")]
            [Min(1)] public int BundleSize;
            [Tooltip("Price per purchase. 0 = from the shop's PriceTable")]
            [Min(0)] public int PriceOverride;
            [Tooltip("Purchases available per restock. 0 = unlimited (stackables); gear always rolls at least one")]
            [Min(0)] public int Stock;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public override void Stock(List<ShopListing> listings, RandomStream random, PriceTable prices)
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Item is GearDefinition gear) StockGear(listings, entry, gear, random, prices);
                else if (entry.Item != null)            StockStack(listings, entry, prices);
            }
        }

        private static void StockStack(List<ShopListing> listings, in Entry entry, PriceTable prices)
        {
            int count = Mathf.Max(1, entry.BundleSize);
            int price = entry.PriceOverride > 0 ? entry.PriceOverride : prices.BuyPrice(entry.Item, count);
            if (price <= 0) return;

            int stock = entry.Stock > 0 ? entry.Stock : ShopListing.Unlimited;
            listings.Add(new StackListing(entry.Item, count, price, stock));
        }

        private static void StockGear(List<ShopListing> listings, in Entry entry, GearDefinition gear, RandomStream random, PriceTable prices)
        {
            for (int i = 0; i < Mathf.Max(1, entry.Stock); i++)
            {
                ItemInstance item = gear.CreateInstance(gear.Roll(gear.Tier, random.NextSeed()));
                int price = entry.PriceOverride > 0 ? entry.PriceOverride : prices.BuyPrice(item);
                if (price <= 0) continue;

                // Weapon categories roll weapons, which belong in the loadout rather than the pack.
                listings.Add(item is WeaponItem weapon ? new WeaponListing(weapon, price) : new GearListing(item, price));
            }
        }
    }
}
