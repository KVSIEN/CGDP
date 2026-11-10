using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Feedback;

namespace CGD.UI
{
    // Feed of short messages at the top of the screen — pickups, kills, quest updates,
    // warnings. Newest on top; each fades out after a while. Repeating the message that
    // is already showing bumps a counter ("×3") instead of stacking copies. Rows are
    // built once and reused. Runs on unscaled time so messages still clear while paused.
    [RequireComponent(typeof(RectTransform))]
    public class NotificationHUD : HUDElement
    {
        [Header("Layout")]
        [SerializeField] private float _topPadding = 90f;
        [SerializeField] private float _rowWidth   = 420f;
        [SerializeField] private float _rowHeight  = 28f;
        [SerializeField] private float _rowGap     = 4f;
        [SerializeField, Min(1)] private int _maxVisible = 5;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float _lifetime = 3f;
        [SerializeField, Min(0f)]   private float _fadeTime = 0.5f;



        private Row[] _rows;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            self.anchorMin = self.anchorMax = self.pivot = new Vector2(0.5f, 1f);
            self.anchoredPosition = new Vector2(0f, -_topPadding);
            self.sizeDelta = new Vector2(_rowWidth, 0f);

            // With a skin, the skin is one row: `Text` (coloured by the message's style); a row
            // brings its own background. Rows are stacked by Row Height and Row Gap, so set
            // those to the skin's height. A skin that turns out unusable is dropped once.
            _rows = new Row[_maxVisible];
            bool skinned = HasSkin;
            for (int i = 0; i < _rows.Length; i++)
            {
                Row row = skinned ? Row.FromSkin(NewSkinItem(self), _rowWidth, _rowHeight) : null;
                if (row == null)
                {
                    skinned = false;
                    row = new Row(self, i, _rowWidth, _rowHeight, Theme.Scrim);
                }
                _rows[i] = row;
            }
        }

        public void Show(string message, NotificationStyle style)
        {
            if (_rows == null) return;

            if (_rows[0].IsActive && _rows[0].Message == message)
            {
                _rows[0].Repeat(_lifetime);
                return;
            }

            // Shift everything down one slot; the oldest drops off the end.
            Row recycled = _rows[_rows.Length - 1];
            for (int i = _rows.Length - 1; i > 0; i--)
                _rows[i] = _rows[i - 1];
            _rows[0] = recycled;

            recycled.Start(message, ColorOf(style), _lifetime);
            Layout();
        }

        public override void Refresh() { }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (Row row in _rows)
                row.Tick(dt, _fadeTime);
        }

        private void Layout()
        {
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].SetSlot(i, _rowHeight, _rowGap);
        }

        private Color ColorOf(NotificationStyle style) => style switch
        {
            NotificationStyle.Success => Theme.Positive,
            NotificationStyle.Reward  => Theme.Reward,
            NotificationStyle.Warning => Theme.Warning,
            NotificationStyle.Danger  => Theme.Danger,
            _                         => Theme.TextPrimary,
        };

        private sealed class Row
        {
            private readonly RectTransform   _rect;
            private readonly TextMeshProUGUI _text;
            private readonly CanvasGroup     _group;
            private float _remaining;
            private int   _count;

            private Row(RectTransform rect, TextMeshProUGUI text)
            {
                _rect = rect;
                _text = text;
                _group = rect.gameObject.TryGetComponent(out CanvasGroup existing)
                    ? existing
                    : rect.gameObject.AddComponent<CanvasGroup>();
                _group.alpha = 0f;
            }

            // Null when the skin lacks a `Text`; the skin is discarded then.
            public static Row FromSkin(SkinParts skin, float width, float height)
            {
                TextMeshProUGUI text = skin.GetText("Text");
                if (!skin.Accept()) return null;

                RectTransform rt = skin.Root;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = skin.SizeOr(new Vector2(width, height));
                return new Row(rt, text);
            }

            public Row(RectTransform parent, int index, float width, float height, Color background)
            {
                Image image = UIFactory.MakeImage($"Notification{index}", parent);
                image.color = background;
                _rect = image.rectTransform;
                _group = image.gameObject.AddComponent<CanvasGroup>();
                _group.alpha = 0f;

                RectTransform rt = _rect;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(width, height);

                _text = UIFactory.MakeText("Text", rt);
                _text.fontSize  = 15f;
                _text.alignment = TextAlignmentOptions.Center;
                UIFactory.Stretch(_text.rectTransform);
            }

            public string Message  { get; private set; }
            public bool   IsActive => _remaining > 0f;

            public void Start(string message, Color color, float lifetime)
            {
                Message     = message;
                _count      = 1;
                _remaining  = lifetime;
                _text.text  = message;
                _text.color = color;
                _group.alpha = 1f;
            }

            public void Repeat(float lifetime)
            {
                _count++;
                _remaining   = lifetime;
                _text.text   = $"{Message}  ×{_count}";
                _group.alpha = 1f;
            }

            public void SetSlot(int slot, float height, float gap) =>
                _rect.anchoredPosition = new Vector2(0f, -slot * (height + gap));

            public void Tick(float deltaTime, float fadeTime)
            {
                if (_remaining <= 0f) return;

                _remaining -= deltaTime;
                _group.alpha = fadeTime > 0f ? Mathf.Clamp01(_remaining / fadeTime) : (_remaining > 0f ? 1f : 0f);
            }
        }
    }
}
