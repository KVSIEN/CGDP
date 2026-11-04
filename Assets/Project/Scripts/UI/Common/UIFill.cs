using UnityEngine;
using UnityEngine.UI;

namespace CGD.UI
{
    // Shows a share of a bar or overlay. A skin can build its bars either way: a Filled image
    // (fillAmount, which suits sprites and radial or vertical fills) or a plain image that is a
    // stretched rectangle (the built-in layouts), whose far edge is pulled in.
    public static class UIFill
    {
        // `amount` (0..1) of the width, from the left.
        public static void Horizontal(Image image, float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (image.type == Image.Type.Filled)
            {
                if (!Mathf.Approximately(image.fillAmount, amount)) image.fillAmount = amount;
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.anchorMax = new Vector2(amount, rect.anchorMax.y);
        }

        // `amount` (0..1) of the height, from the bottom.
        public static void Vertical(Image image, float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (image.type == Image.Type.Filled)
            {
                if (!Mathf.Approximately(image.fillAmount, amount)) image.fillAmount = amount;
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.anchorMax = new Vector2(rect.anchorMax.x, amount);
        }
    }
}
