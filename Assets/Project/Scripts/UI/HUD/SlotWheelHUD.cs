using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CGD.UI
{
    // The selection wheel opened by holding a weapon or item key: the choices on a ring
    // around the screen centre with the highlighted one lit, and the slot's name in the
    // middle. Driven by SlotKeyWheel; hidden until then.
    [RequireComponent(typeof(RectTransform))]
    public class SlotWheelHUD : HUDElement
    {
        private const int MaxOptions = 12;

        [SerializeField] private float   _radius   = 150f;
        [SerializeField] private Vector2 _tileSize = new(150f, 40f);

        private static readonly Color TileColor      = new(0f, 0f, 0f, 0.6f);
        private static readonly Color HighlightColor = new(0.25f, 0.55f, 1f, 0.85f);

        private readonly List<Image> _tiles = new();
        private readonly List<TextMeshProUGUI> _labels = new();
        private TextMeshProUGUI _title;
        private int _highlighted = -1;

        public override bool ShowWithHud => false;

        public void Open(string title, IReadOnlyList<string> options)
        {
            EnsureBuilt();
            _title.text = title;

            int count = Mathf.Min(options.Count, MaxOptions);
            for (int i = 0; i < _tiles.Count; i++)
            {
                bool used = i < count;
                _tiles[i].gameObject.SetActive(used);
                if (!used) continue;

                _labels[i].text = options[i];
                float angle = i * Mathf.PI * 2f / count;   // 0 = top, clockwise
                _tiles[i].rectTransform.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * _radius;
            }

            _highlighted = -1;
            Highlight(-1);
            Show();
        }

        public void Highlight(int index)
        {
            if (index == _highlighted && _tiles.Count > 0) return;
            _highlighted = index;
            for (int i = 0; i < _tiles.Count; i++)
                _tiles[i].color = i == index ? HighlightColor : TileColor;
        }

        public override void Refresh() { }

        private void EnsureBuilt()
        {
            if (_title != null) return;

            var self = GetComponent<RectTransform>();
            self.anchorMin = self.anchorMax = new Vector2(0.5f, 0.5f);
            self.sizeDelta = Vector2.one * (_radius * 2f + _tileSize.x);
            self.anchoredPosition = Vector2.zero;

            _title = UIFactory.MakeText("Title", self);
            _title.fontSize  = 16f;
            _title.alignment = TextAlignmentOptions.Center;
            UIFactory.Place(_title.rectTransform, Vector2.zero, new Vector2(_tileSize.x, 30f));
            CentreAnchor(_title.rectTransform);

            for (int i = 0; i < MaxOptions; i++)
            {
                Image tile = UIFactory.MakeImage($"Option{i}", self);
                CentreAnchor(tile.rectTransform);
                tile.rectTransform.sizeDelta = _tileSize;

                TextMeshProUGUI label = UIFactory.MakeText("Label", tile.rectTransform);
                label.fontSize  = 13f;
                label.alignment = TextAlignmentOptions.Center;
                UIFactory.Stretch(label.rectTransform);

                _tiles.Add(tile);
                _labels.Add(label);
            }
        }

        private static void CentreAnchor(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
