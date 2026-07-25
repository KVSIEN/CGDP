using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shared element construction for code-built HUD panels — avoids re-implementing
// the same GameObject/RectTransform setup in every HUD script.
public static class HudUIFactory
{
    public static Image MakeImage(string name, RectTransform parent, int layer = -1)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (layer >= 0) go.layer = layer;
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI MakeText(string name, RectTransform parent, int layer = -1)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (layer >= 0) go.layer = layer;
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
