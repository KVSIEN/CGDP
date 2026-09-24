using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace CGD.UI
{
    // A column of text rows, each optionally clickable, rebuilt in place: Begin, Add rows,
    // End. Rows are created once and reused, so rebuilding on every inventory change is
    // cheap. Rows without an action render as section headings.
    public class UIButtonList
    {
        private static readonly Color RowColor     = new(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color HeadingColor = new(0f, 0f, 0f, 0f);
        private static readonly Color DisabledText = new(1f, 1f, 1f, 0.35f);

        private readonly RectTransform _root;
        private readonly float _rowHeight;
        private readonly List<Row> _rows = new();
        private int _used;

        public UIButtonList(RectTransform root, float rowHeight = 26f)
        {
            _root      = root;
            _rowHeight = rowHeight;
        }

        public void Begin() => _used = 0;

        public void Heading(string text) => Next().Set(text, null, false, true, HeadingColor, _rowHeight);

        public void Label(string text) => Next().Set(text, null, false, false, HeadingColor, _rowHeight);

        public void Add(string text, UnityAction onClick, bool interactable = true) =>
            Next().Set(text, onClick, interactable, false, RowColor, _rowHeight);

        public void End()
        {
            for (int i = _used; i < _rows.Count; i++)
                _rows[i].Hide();
        }

        private Row Next()
        {
            if (_used == _rows.Count) _rows.Add(new Row(_root, _rows.Count));

            Row row = _rows[_used];
            row.Place(_used, _rowHeight);
            _used++;
            return row;
        }

        private sealed class Row
        {
            private readonly Button          _button;
            private readonly Image           _background;
            private readonly TextMeshProUGUI _text;

            public Row(RectTransform parent, int index)
            {
                _button     = UIFactory.MakeButton($"Row{index}", parent, string.Empty, out _text);
                _background = (Image)_button.targetGraphic;
                _text.alignment = TextAlignmentOptions.MidlineLeft;
                _text.rectTransform.offsetMin = new Vector2(8f, 0f);

                RectTransform rt = _button.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
            }

            public void Place(int index, float height)
            {
                RectTransform rt = (RectTransform)_button.transform;
                rt.anchoredPosition = new Vector2(0f, -index * (height + 2f));
                rt.sizeDelta        = new Vector2(0f, height);
                _button.gameObject.SetActive(true);
            }

            public void Set(string text, UnityAction onClick, bool interactable, bool bold, Color background, float height)
            {
                _text.text      = text;
                _text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
                _text.color     = onClick != null && !interactable ? DisabledText : Color.white;
                _background.color = background;

                _button.onClick.RemoveAllListeners();
                if (onClick != null) _button.onClick.AddListener(onClick);
                _button.interactable = onClick != null && interactable;
            }

            public void Hide() => _button.gameObject.SetActive(false);
        }
    }
}
