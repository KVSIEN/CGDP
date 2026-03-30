using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class StatsHUD : HUDElement
{
    [SerializeField] private PlayerStats _playerStats;

    [Header("Colors")]
    [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color _barBgColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
    [SerializeField] private Color _healthColor = new Color(0.22f, 0.85f, 0.22f, 1f);
    [SerializeField] private Color _healthLowColor = new Color(0.9f, 0.15f, 0.1f, 1f);
    [SerializeField] private Color _textColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    [Header("Layout")]
    [SerializeField] private Vector2 _screenPadding = new Vector2(20f, 20f);
    [SerializeField] private Vector2 _innerPadding = new Vector2(12f, 12f);
    [SerializeField] private float _panelWidth = 200f;
    [SerializeField] private float _barHeight = 8f;

    private Image _healthFill;
    private TextMeshProUGUI _healthText;

    private void Awake()
    {
        SetupAnchor();
        Build();

        if (_playerStats != null)
            _playerStats.OnChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (_playerStats != null)
            _playerStats.OnChanged -= Refresh;
    }

    private void SetupAnchor()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = _screenPadding;
    }

    private void Build()
    {
        var self = GetComponent<RectTransform>();

        float ip = _innerPadding.x;
        float contentWidth = _panelWidth - ip * 2f;
        float y = -_innerPadding.y;

        // Background panel (added first so it renders behind everything)
        var bg = MakeImage("Background", self);
        bg.color = _backgroundColor;
        Stretch(bg.rectTransform);

        // Health bar background
        var barBg = MakeImage("HealthBarBg", self);
        barBg.color = _barBgColor;
        Place(barBg.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, _barHeight));

        // Health fill (child of bar background, width driven by anchor)
        _healthFill = MakeImage("HealthFill", barBg.rectTransform);
        _healthFill.color = _healthColor;
        var fillRt = _healthFill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        y -= _barHeight + 6f;

        // Health text
        _healthText = MakeText("HealthText", self);
        _healthText.color = _textColor;
        _healthText.fontSize = 12f;
        _healthText.alignment = TextAlignmentOptions.Left;
        Place(_healthText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 16f));

        y -= 16f;

        float totalHeight = -y + _innerPadding.y;
        self.sizeDelta = new Vector2(_panelWidth, totalHeight);
    }

    public override void Refresh()
    {
        if (_playerStats == null) return;

        float ratio = _playerStats.MaxHealth > 0f ? _playerStats.Health / _playerStats.MaxHealth : 0f;
        _healthFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        _healthFill.color = Color.Lerp(_healthLowColor, _healthColor, ratio);
        _healthText.text = $"HP  {Mathf.CeilToInt(_playerStats.Health)} / {Mathf.CeilToInt(_playerStats.MaxHealth)}";
    }

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

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
