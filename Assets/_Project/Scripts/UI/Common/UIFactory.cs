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

    // Button with a background Image (also its targetGraphic) and a centered text label.
    public static Button MakeButton(string name, RectTransform parent, string label, out TextMeshProUGUI text, int layer = -1)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        if (layer >= 0) go.layer = layer;
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.16f, 0.16f, 0.22f, 0.95f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        text = MakeText(name + "_Label", go.GetComponent<RectTransform>(), layer);
        text.text = label;
        text.fontSize = 13f;
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform);

        return btn;
    }

    // Minimal functional Slider (Background / Fill Area+Fill / Handle Slide Area+Handle).
    public static Slider MakeSlider(string name, RectTransform parent, int layer = -1)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        if (layer >= 0) go.layer = layer;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();

        var bg = MakeImage("Background", rt, layer);
        bg.color = new Color(1f, 1f, 1f, 0.15f);
        bg.rectTransform.anchorMin = new Vector2(0f, 0.3f);
        bg.rectTransform.anchorMax = new Vector2(1f, 0.7f);
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(rt, false);
        var fillAreaRt = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.3f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.7f);
        fillAreaRt.offsetMin = new Vector2(4f, 0f);
        fillAreaRt.offsetMax = new Vector2(-4f, 0f);

        var fill = MakeImage("Fill", fillAreaRt, layer);
        fill.color = new Color(0.3f, 0.55f, 1f, 0.9f);
        fill.rectTransform.anchorMin = new Vector2(0f, 0f);
        fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.sizeDelta = new Vector2(8f, 0f);

        var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(rt, false);
        var handleAreaRt = handleAreaGO.GetComponent<RectTransform>();
        Stretch(handleAreaRt);
        handleAreaRt.offsetMin = new Vector2(8f, 0f);
        handleAreaRt.offsetMax = new Vector2(-8f, 0f);

        var handle = MakeImage("Handle", handleAreaRt, layer);
        handle.color = Color.white;
        handle.rectTransform.anchorMin = new Vector2(0f, 0f);
        handle.rectTransform.anchorMax = new Vector2(0f, 1f);
        handle.rectTransform.sizeDelta = new Vector2(14f, 0f);

        var slider = go.GetComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    // Single-line TMP_InputField with a background Image and a masked text viewport.
    public static TMP_InputField MakeInputField(string name, RectTransform parent, int layer = -1)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        if (layer >= 0) go.layer = layer;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();

        var bg = go.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.08f);

        var viewportGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(rt, false);
        var viewportRt = viewportGO.GetComponent<RectTransform>();
        Stretch(viewportRt);
        viewportRt.offsetMin = new Vector2(6f, 2f);
        viewportRt.offsetMax = new Vector2(-6f, -2f);

        var text = MakeText("Text", viewportRt, layer);
        text.fontSize = 13f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        Stretch(text.rectTransform);

        var inputField = go.GetComponent<TMP_InputField>();
        inputField.targetGraphic = bg;
        inputField.textViewport = viewportRt;
        inputField.textComponent = text;

        return inputField;
    }
}
