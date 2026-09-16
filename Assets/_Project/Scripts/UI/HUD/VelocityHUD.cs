using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class VelocityHUD : HUDElement
{
    [SerializeField] private PlayerMovement _movement;

    [Header("Colors")]
    [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color _textColor        = new Color(0.92f, 0.92f, 0.92f, 1f);

    [Header("Layout")]
    [SerializeField] private Vector2 _screenPadding = new Vector2(20f, 20f);
    [SerializeField] private float   _panelWidth    = 120f;
    [SerializeField] private float   _panelHeight   = 28f;

    private TextMeshProUGUI _text;
    private int             _lastSpeed = -1;

    private void Awake()
    {
        SetupAnchor();
        Build();
    }

    private void SetupAnchor()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-_screenPadding.x, -_screenPadding.y);
        rt.sizeDelta = new Vector2(_panelWidth, _panelHeight);
    }

    private void Build()
    {
        var self = GetComponent<RectTransform>();

        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(self, false);
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color         = _backgroundColor;
        bgImg.raycastTarget = false;
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var textGo = new GameObject("SpeedText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(self, false);
        _text               = textGo.GetComponent<TextMeshProUGUI>();
        _text.color         = _textColor;
        _text.fontSize      = 12f;
        _text.alignment     = TextAlignmentOptions.Center;
        _text.raycastTarget = false;
        var textRt = _text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
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
