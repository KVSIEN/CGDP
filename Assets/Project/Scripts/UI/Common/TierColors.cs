using UnityEngine;
using CGD.Items;

namespace CGD.UI
{
    // The colour each item tier is shown in, everywhere items are listed.
    public static class TierColors
    {
        private static readonly Color Common    = new(0.75f, 0.75f, 0.75f, 1f);
        private static readonly Color Uncommon  = new(0.30f, 0.85f, 0.30f, 1f);
        private static readonly Color Rare      = new(0.30f, 0.60f, 0.90f, 1f);
        private static readonly Color Epic      = new(0.70f, 0.35f, 0.90f, 1f);
        private static readonly Color Legendary = new(0.90f, 0.70f, 0.30f, 1f);

        private static readonly string[] Hex =
        {
            ColorUtility.ToHtmlStringRGB(Common), ColorUtility.ToHtmlStringRGB(Uncommon), ColorUtility.ToHtmlStringRGB(Rare),
            ColorUtility.ToHtmlStringRGB(Epic),   ColorUtility.ToHtmlStringRGB(Legendary),
        };

        public static Color Of(ItemTier tier) => tier switch
        {
            ItemTier.Uncommon  => Uncommon,
            ItemTier.Rare      => Rare,
            ItemTier.Epic      => Epic,
            ItemTier.Legendary => Legendary,
            _                  => Common,
        };

        // For TextMeshPro rich text: <color=#RRGGBB>.
        public static string RichHex(ItemTier tier) => "#" + Hex[Mathf.Clamp((int)tier, 0, Hex.Length - 1)];
    }
}
