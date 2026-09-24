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

        [Header("Colors")]
        [SerializeField] private Color _info    = new(0.92f, 0.92f, 0.92f);
        [SerializeField] private Color _success = new(0.55f, 0.95f, 0.55f);
        [SerializeField] private Color _reward  = new(1f, 0.85f, 0.4f);
        [SerializeField] private Color _warning = new(1f, 0.65f, 0.25f);
        [SerializeField] private Color _danger  = new(1f, 0.35f, 0.35f);

        private static readonly Color RowBg = new(0f, 0f, 0f, 0.55f);

        private Row[] _rows;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            self.anchorMin = self.anchorMax = self.pivot = new Vector2(0.5f, 1f);
            self.anchoredPosition = new Vector2(0f, -_topPadding);
            self.sizeDelta = new Vector2(_rowWidth, 0f);

            _rows = new Row[_maxVisible];
            for (int i = 0; i < _rows.Length; i++)
                _rows[i] = new Row(self, i, _rowWidth, _rowHeight);
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
            NotificationStyle.Success => _success,
            NotificationStyle.Reward  => _reward,
            NotificationStyle.Warning => _warning,
            NotificationStyle.Danger  => _danger,
            _                         => _info,
        };

        private sealed class Row
        {
            private readonly Image           _background;
            private readonly TextMeshProUGUI _text;
            private readonly CanvasGroup     _group;
            private float _remaining;
            private int   _count;

            public Row(RectTransform parent, int index, float width, float height)
            {
                _background = UIFactory.MakeImage($"Notification{index}", parent);
                _background.color = RowBg;
                _group = _background.gameObject.AddComponent<CanvasGroup>();
                _group.alpha = 0f;

                RectTransform rt = _background.rectTransform;
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
                _background.rectTransform.anchoredPosition = new Vector2(0f, -slot * (height + gap));

            public void Tick(float deltaTime, float fadeTime)
            {
                if (_remaining <= 0f) return;

                _remaining -= deltaTime;
                _group.alpha = fadeTime > 0f ? Mathf.Clamp01(_remaining / fadeTime) : (_remaining > 0f ? 1f : 0f);
            }
        }
    }
}
