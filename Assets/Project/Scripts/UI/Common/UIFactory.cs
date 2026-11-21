using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CGD.UI
{
    // Shared element construction for code-built HUD panels — avoids re-implementing
    // the same GameObject/RectTransform setup in every HUD script.
    public static class UIFactory
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

        // A text with its words and size set up front; alignment is left as TextMeshPro's default
        // unless one is given.
        public static TextMeshProUGUI MakeLabel(string name, RectTransform parent, string text, float size,
                                                TextAlignmentOptions? alignment = null)
        {
            TextMeshProUGUI label = MakeText(name, parent);
            label.text     = text;
            label.fontSize = size;
            if (alignment.HasValue) label.alignment = alignment.Value;
            return label;
        }

        // A vertically scrolling list placed like Place (top-left anchored); returns the content
        // to add rows to, `contentWidth` × `contentHeight`, top-left anchored too.
        public static RectTransform MakeScrollList(string name, RectTransform parent, Vector2 position, Vector2 size,
                                                   float contentWidth, float contentHeight, float scrollSensitivity = 1f)
        {
            var view = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            view.transform.SetParent(parent, false);
            var viewRect = (RectTransform)view.transform;
            Place(viewRect, position, size);
            view.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
            view.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewRect, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);

            var scroll = view.GetComponent<ScrollRect>();
            scroll.content           = contentRect;
            scroll.viewport          = viewRect;
            scroll.horizontal        = false;
            scroll.vertical          = true;
            scroll.movementType      = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = scrollSensitivity;
            return contentRect;
        }

        // Stretched to fill its parent — map layers, render-texture views.
        // A world-space canvas under `parent`, `size` canvas units at `scale` metres per unit.
        public static Canvas MakeWorldCanvas(string name, Transform parent, Vector2 size, Vector3 localPosition, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Placed only after switching to WorldSpace: a new Canvas starts in overlay mode,
            // which drives (and overwrites) its RectTransform.
            var rt = (RectTransform)canvas.transform;
            rt.sizeDelta     = size;
            rt.localPosition = localPosition;
            rt.localScale    = Vector3.one * scale;
            return canvas;
        }

        public static RawImage MakeRawImage(string name, RectTransform parent, int layer = -1)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            if (layer >= 0) go.layer = layer;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            Stretch(image.rectTransform);
            return image;
        }

        // Shrinks the text to fit its box instead of overflowing it, down to `minSize`; it
        // never grows past the size it already has. For labels whose length depends on a
        // setting, such as a rebindable key name.
        public static void ShrinkToFit(TMP_Text text, float minSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax      = text.fontSize;
            text.fontSizeMin      = minSize;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // Pins a HUD panel to a screen corner ((0,0) bottom-left … (1,1) top-right), with
        // padding measured inward from that corner.
        public static void AnchorToCorner(RectTransform rt, Vector2 corner, Vector2 padding)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.anchoredPosition = new Vector2(
                corner.x > 0.5f ? -padding.x : padding.x,
                corner.y > 0.5f ? -padding.y : padding.y);
        }

        // Top-left anchored and pivoted: the layout most built panels lay their parts out in.
        public static void Place(RectTransform rt, Vector2 pos, Vector2 size) => Pin(rt, new Vector2(0f, 1f), pos, size);

        // Anchored and pivoted on the same point of the parent ((0.5, 0.5) its centre, (0.5, 0)
        // its bottom edge's middle…), offset by `pos` from there.
        public static void Pin(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // A row hanging from `y` below the parent's top, the parent's full width less `inset`
        // on each side.
        public static void PlaceRow(RectTransform rt, float y, float height, float inset = 0f)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(-inset * 2f, height);
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
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            if (layer >= 0) go.layer = layer;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            // Invisible hit area over the whole slider: the visible track is too thin to grab,
            // and MakeImage's graphics don't receive raycasts.
            var hitArea = go.GetComponent<Image>();
            hitArea.color = Color.clear;

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
            // Same inset as the handle area so the fill ends at the handle's centre.
            fillAreaRt.offsetMin = new Vector2(8f, 0f);
            fillAreaRt.offsetMax = new Vector2(-8f, 0f);

            // The Slider only drives the fill's anchors, so its offsets must be exactly zero.
            var fill = MakeImage("Fill", fillAreaRt, layer);
            fill.color = new Color(0.3f, 0.55f, 1f, 0.9f);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

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
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(text.rectTransform);

            var inputField = go.GetComponent<TMP_InputField>();
            inputField.targetGraphic = bg;
            inputField.textViewport = viewportRt;
            inputField.textComponent = text;

            return inputField;
        }
    }
}
