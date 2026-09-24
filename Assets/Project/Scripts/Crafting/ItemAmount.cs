using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Crafting
{
    // An item and how many — a recipe ingredient or its output.
    [Serializable]
    public struct ItemAmount
    {
        public ItemDefinition Item;
        [Min(1)] public int   Count;

        public ItemAmount(ItemDefinition item, int count)
        {
            Item  = item;
            Count = count;
        }

        public int SafeCount => Mathf.Max(1, Count);
    }
}
