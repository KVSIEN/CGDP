using UnityEngine;
using CGD.Items;

namespace CGD.Economy
{
    // What goods are worth. Stackables are worth their BaseValue per unit; rolled gear
    // is worth its BaseValue scaled by quality, so a Legendary sells for several times a
    // Common of the same kind. Shops buy at a markup and sell back at a fraction, and
    // different vendors can use different tables (a fence pays more, a trader charges more).
    [CreateAssetMenu(fileName = "PriceTable", menuName = "CGD/Economy/Price Table")]
    public class PriceTable : ScriptableObject
    {
        [Tooltip("Value multiplier by quality (0 = quality 1, 1 = quality 100)")]
        [SerializeField] private AnimationCurve _qualityMultiplier = AnimationCurve.EaseInOut(0f, 1f, 1f, 6f);
        [Tooltip("Buy price = value × markup")]
        [SerializeField, Min(0f)] private float _buyMarkup = 1f;
        [Tooltip("Sell price = value × this (also what a trade-in is credited)")]
        [SerializeField, Range(0f, 1f)] private float _sellRatio = 0.4f;

        // Buying rounds up and selling rounds down, so nothing can be bought and sold back
        // for a profit, however cheap it is.
        public int BuyPrice(ItemDefinition item, int count)  => Mathf.CeilToInt(StackValue(item, count) * _buyMarkup);
        public int BuyPrice(ItemInstance item)               => Mathf.CeilToInt(InstanceValue(item) * _buyMarkup);
        public int SellPrice(ItemDefinition item, int count) => Mathf.FloorToInt(StackValue(item, count) * _sellRatio);
        public int SellPrice(ItemInstance item)              => Mathf.FloorToInt(InstanceValue(item) * _sellRatio);

        // Fewest units that sell for at least 1 (three rounds of ammo, one rifle); 0 = worthless.
        public int SellBundle(ItemDefinition item)
        {
            float unit = StackValue(item, 1) * _sellRatio;
            return unit > 0f ? Mathf.CeilToInt(1f / unit - 0.0001f) : 0;
        }

        private static float StackValue(ItemDefinition item, int count) => item != null ? item.BaseValue * (float)count : 0f;

        // Hand-authored weapons have no definition behind them, so they're worth nothing.
        private float InstanceValue(ItemInstance item)
        {
            if (item?.Definition == null) return 0f;
            return item.Definition.BaseValue * _qualityMultiplier.Evaluate(ItemTiers.Normalize(item.Quality));
        }
    }
}
