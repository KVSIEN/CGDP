using UnityEngine;
using TMPro;
using CGD.Player;

namespace CGD.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class VelocityHUD : HUDElement
    {
        [SerializeField] private PlayerMovement _movement;


        [Header("Layout")]
        [SerializeField] private Vector2 _screenPadding = new Vector2(20f, 20f);
        [SerializeField] private float   _panelWidth    = 120f;
        [SerializeField] private float   _panelHeight   = 28f;

        private TextMeshProUGUI _text;
        private int             _lastSpeed = -1;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            UIFactory.AnchorToCorner(self, new Vector2(1f, 1f), _screenPadding);

            SkinParts skin = UseSkin();
            if (skin != null)
            {
                _text = skin.GetText("SpeedText");
                if (skin.Accept())
                {
                    self.sizeDelta = skin.SizeOr(new Vector2(_panelWidth, _panelHeight));
                    return;
                }
            }

            self.sizeDelta = new Vector2(_panelWidth, _panelHeight);

            var background = UIFactory.MakeImage("Background", self);
            background.color = Theme.Panel;
            UIFactory.Stretch(background.rectTransform);

            _text           = UIFactory.MakeText("SpeedText", self);
            _text.color     = Theme.TextPrimary;
            _text.fontSize  = 12f;
            _text.alignment = TextAlignmentOptions.Center;
            UIFactory.Stretch(_text.rectTransform);
        }

        public override void Refresh() { }

        private void Update()
        {
            if (_movement == null) return;
            int speed = Mathf.RoundToInt(_movement.Velocity.magnitude);
            if (speed == _lastSpeed) return;
            _lastSpeed = speed;
            _text.text = $"SPD  {speed}";
        }
    }
}
