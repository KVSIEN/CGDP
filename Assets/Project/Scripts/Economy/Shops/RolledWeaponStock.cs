using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Economy
{
    // A handful of freshly generated weapons per restock: a random category and tier each,
    // priced by quality through the shop's PriceTable (the category's BaseValue is a
    // Common's worth).
    [CreateAssetMenu(fileName = "NewRolledWeaponStock", menuName = "CGD/Economy/Stock/Rolled Weapon Stock")]
    public class RolledWeaponStock : ShopStockSource
    {
        [SerializeField] private WeaponCategoryData[] _categories = Array.Empty<WeaponCategoryData>();
        [Tooltip("Weapons on the shelf after a restock")]
        [SerializeField] private IntRange _count = new(3, 5);
        [SerializeField] private ItemTier _minTier = ItemTier.Common;
        [SerializeField] private ItemTier _maxTier = ItemTier.Rare;

        public override void Stock(List<ShopListing> listings, RandomStream random, PriceTable prices)
        {
            if (_categories.Length == 0) return;

            int count = _count.Evaluate(random);
            for (int i = 0; i < count; i++)
            {
                WeaponCategoryData category = random.Pick(_categories);
                if (category == null) continue;

                var tier = (ItemTier)random.Range((int)_minTier, (int)_maxTier + 1);
                WeaponInstance weapon = WeaponGenerator.Generate(category, category.Roll(tier, random.NextSeed()));

                int price = prices.BuyPrice(weapon);
                if (price > 0) listings.Add(new WeaponListing(weapon, price));
            }
        }
    }
}
