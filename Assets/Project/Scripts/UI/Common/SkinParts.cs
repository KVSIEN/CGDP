using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CGD.UI
{
    // A designer-made layout standing in for an element's built-in one. The element asks for the
    // named parts it needs to drive (a fill, a label) and leaves everything else about them to the
    // prefab: size, sprite, font, anchors, extra decoration. A part the skin lacks makes the
    // element drop the skin and build its own layout, so a half-finished skin never breaks the HUD.
    public sealed class SkinParts
    {
        private readonly Dictionary<string, Transform> _byName = new();
        private readonly string _owner;
        private List<string> _missing;

        public RectTransform Root { get; }

        // The size the skin was designed at (its root's width and height).
        private readonly Vector2 _designSize;

        private SkinParts(RectTransform root, Vector2 designSize, string owner)
        {
            Root        = root;
            _designSize = designSize;
            _owner      = owner;

            // First one wins when a name is used twice.
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                _byName.TryAdd(child.name, child);
        }

        // Instantiates `prefab` under `parent`. A skin for a whole element is stretched to fill
        // it; one item of a repeated element (a slot, a row) is left for the element to place.
        public static SkinParts Create(RectTransform prefab, RectTransform parent, string owner, bool stretch = true)
        {
            Vector2 designSize = prefab.sizeDelta;
            RectTransform root = Object.Instantiate(prefab, parent, false);
            root.name = prefab.name;
            if (stretch) UIFactory.Stretch(root);
            return new SkinParts(root, designSize, owner);
        }

        // An optional part may be left out of the skin: it comes back null, and the element
        // simply doesn't drive it.
        public Image           GetImage(string part, bool optional = false) => Get<Image>(part, optional);
        public TextMeshProUGUI GetText(string part, bool optional = false)  => Get<TextMeshProUGUI>(part, optional);

        // The skin's size for the element to adopt, or `fallback` when the root has none (a
        // root that stretches to its parent has a size of 0).
        public Vector2 SizeOr(Vector2 fallback) =>
            _designSize.x > 0f && _designSize.y > 0f ? _designSize : fallback;

        // True when every part asked for so far was there. Otherwise warns, discards the skin and
        // returns false so the element falls back to its built-in layout.
        public bool Accept()
        {
            if (_missing == null) return true;

            Debug.LogWarning($"{_owner}: skin '{Root.name}' is missing {string.Join(", ", _missing)}; using the built-in layout.", Root);
            if (Application.isPlaying) Object.Destroy(Root.gameObject);
            else                       Object.DestroyImmediate(Root.gameObject);
            return false;
        }

        private T Get<T>(string part, bool optional) where T : Component
        {
            if (_byName.TryGetValue(part, out Transform found) && found.TryGetComponent(out T component))
                return component;

            if (!optional) (_missing ??= new List<string>()).Add($"{part} ({typeof(T).Name})");
            return null;
        }
    }
}
