using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class DodgeHUD : HUDElement
{
    [SerializeField] private PlayerDodge _dodge;

    [Header("Layout")]
    [SerializeField] private float _slotSize            = 56f;
    [SerializeField] private float _screenPaddingBottom = 24f;
    [SerializeField] private float _offsetFromCenter    = 160f;

    private static readonly Color ReadyColor    = new Color(0.25f, 0.55f, 1f,  0.85f);
    private static readonly Color CooldownColor = new Color(0.1f,  0.1f,  0.1f, 0.85f);
    private static readonly Color StepColor     = new Color(1f,    0.75f, 0.1f, 0.85f);
    private static readonly Color RollColor     = new Color(0.15f, 0.9f,  0.4f, 0.85f);
    private static readonly Color OverlayColor  = new Color(0f,    0f,    0f,   0.65f);

    private Image          _bg;
    private Image          _overlay;
    private TextMeshProUGUI _nameLabel;

    private void Awake()
    {
        var rt              = GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(_offsetFromCenter, _screenPaddingBottom);
        rt.sizeDelta        = Vector2.one * _slotSize;

        _bg          = MakeImage("DodgeBg", rt);
        _bg.color    = ReadyColor;
        Stretch(_bg.rectTransform);

        _overlay       = MakeImage("DodgeOverlay", _bg.rectTransform);
        _overlay.color = OverlayColor;
        Stretch(_overlay.rectTransform);

        var keyLabel         = MakeText("DodgeKey", _bg.rectTransform);
        keyLabel.text        = "Q";
        keyLabel.fontSize    = 11f;
        keyLabel.color       = new Color(1f, 1f, 1f, 0.7f);
        keyLabel.alignment   = TextAlignmentOptions.TopLeft;
        Stretch(keyLabel.rectTransform);
        keyLabel.rectTransform.offsetMin = new Vector2(4f, 0f);
        keyLabel.rectTransform.offsetMax = new Vector2(0f, -3f);

        _nameLabel           = MakeText("DodgeName", _bg.rectTransform);
        _nameLabel.text      = "DODGE";
        _nameLabel.fontSize  = 9f;
        _nameLabel.color     = new Color(1f, 1f, 1f, 0.85f);
        _nameLabel.alignment = TextAlignmentOptions.Center;
        Stretch(_nameLabel.rectTransform);
    }

    private void Update()
    {
        if (_dodge == null) return;

        var phase = _dodge.CurrentDodgePhase;

        if (phase == PlayerDodge.DodgePhase.Sidestep)
        {
            _bg.color = StepColor;
            _overlay.rectTransform.anchorMax = new Vector2(1f, 0f);
            _nameLabel.text = "STEP";
        }
        else if (phase == PlayerDodge.DodgePhase.Roll)
        {
            _bg.color = RollColor;
            _overlay.rectTransform.anchorMax = new Vector2(1f, 0f);
            _nameLabel.text = "ROLL";
        }
        else
        {
            float ratio = _dodge.DodgeReadyRatio;
            _bg.color = Color.Lerp(CooldownColor, ReadyColor, ratio);
            _overlay.rectTransform.anchorMax = new Vector2(1f, 1f - ratio);
            _nameLabel.text = "DODGE";
        }
    }

    public override void Refresh() { }

    private static Image MakeImage(string elementName, RectTransform parent)
    {
        var go = new GameObject(elementName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string elementName, RectTransform parent)
    {
        var go = new GameObject(elementName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
