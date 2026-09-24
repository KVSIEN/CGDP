using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Core;
using CGD.Input;

namespace CGD.UI
{
    // Base for mouse-driven windows (character, crafting, dev console): a dimmed backdrop,
    // a centred window with a title and close button, and while open the player's input
    // is locked and the cursor freed. Only one modal panel is open at a time.
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public abstract class ModalPanel : HUDElement
    {
        private const float TitleHeight = 36f;

        [SerializeField] protected PlayerInputHandler _input;

        private CanvasGroup _group;
        private TextMeshProUGUI _title;
        private int _openedFrame = -1;

        public static ModalPanel Open { get; private set; }

        public override bool ShowWithHud => false;

        protected abstract string  Title      { get; }
        protected abstract Vector2 WindowSize { get; }

        // True on the frame the panel opened, so the key that opened it doesn't also close it.
        protected bool OpenedThisFrame => _openedFrame == Time.frameCount;

        // Another modal panel is in the way.
        protected static bool IsBlocked(ModalPanel self) => Open != null && Open != self;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Open = null;

        protected virtual void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            var self = GetComponent<RectTransform>();
            UIFactory.Stretch(self);

            Image dim = UIFactory.MakeImage("Dim", self);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;
            UIFactory.Stretch(dim.rectTransform);

            Image window = UIFactory.MakeImage("Window", self);
            window.color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
            RectTransform windowRt = window.rectTransform;
            windowRt.anchorMin = windowRt.anchorMax = windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = WindowSize;

            _title = UIFactory.MakeText("Title", windowRt);
            _title.text      = Title;
            _title.fontSize  = 18f;
            _title.fontStyle = FontStyles.Bold;
            _title.alignment = TextAlignmentOptions.MidlineLeft;
            UIFactory.Place(_title.rectTransform, new Vector2(14f, 0f), new Vector2(WindowSize.x - 60f, TitleHeight));

            Button close = UIFactory.MakeButton("Close", windowRt, "X", out _);
            UIFactory.Place(close.GetComponent<RectTransform>(), new Vector2(WindowSize.x - 40f, -6f), new Vector2(30f, 24f));
            close.onClick.AddListener(Hide);

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(windowRt, false);
            UIFactory.Stretch(content);
            content.offsetMin = new Vector2(12f, 12f);
            content.offsetMax = new Vector2(-12f, -TitleHeight - 4f);

            Build(content);
            Apply(false);
        }

        protected virtual void OnDisable()
        {
            if (IsVisible) Hide();
        }

        protected abstract void Build(RectTransform content);

        protected void SetTitle(string title) => _title.text = title;
        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }

        public override void Show()
        {
            if (IsVisible || IsBlocked(this)) return;
            Apply(true);
            OnOpened();
        }

        public override void Hide()
        {
            if (!IsVisible) return;
            Apply(false);
            OnClosed();
        }

        public override void Toggle()
        {
            if (IsVisible) Hide();
            else           Show();
        }

        private void Apply(bool open)
        {
            IsVisible = open;
            _group.alpha          = open ? 1f : 0f;
            _group.blocksRaycasts = open;
            _group.interactable   = open;

            if (open)
            {
                Open = this;
                _openedFrame = Time.frameCount;
            }
            else if (Open == this)
            {
                Open = null;
            }

            if (_input != null) _input.InputEnabled = !open;
            CursorLock.Set(!open);
        }

        // Two columns side by side inside the content area.
        protected static (RectTransform left, RectTransform right) Columns(RectTransform content, float gap = 16f)
        {
            RectTransform Make(string name, float from, float to)
            {
                var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rt.SetParent(content, false);
                rt.anchorMin = new Vector2(from, 0f);
                rt.anchorMax = new Vector2(to, 1f);
                rt.offsetMin = new Vector2(from > 0f ? gap * 0.5f : 0f, 0f);
                rt.offsetMax = new Vector2(to < 1f ? -gap * 0.5f : 0f, 0f);
                return rt;
            }

            return (Make("Left", 0f, 0.5f), Make("Right", 0.5f, 1f));
        }
    }
}
