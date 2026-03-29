using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CrosshairHUD : HUDElement
{
    [SerializeField] private CrosshairSettings _settings;

    private readonly Image[] _lines = new Image[4];
    private readonly Image[] _lineOutlines = new Image[4];
    private Image _dot;
    private Image _dotOutline;

    private void Awake()
    {
        for (int i = 0; i < 4; i++)
        {
            _lineOutlines[i] = CreateImage("LineOutline_" + i);
            _lines[i] = CreateImage("Line_" + i);
        }
        _dotOutline = CreateImage("DotOutline");
        _dot = CreateImage("Dot");

        Refresh();
    }

    private Image CreateImage(string elementName)
    {
        var go = new GameObject(elementName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return go.GetComponent<Image>();
    }

    public override void Refresh()
    {
        if (_settings == null) return;

        float outlineSize = _settings.showOutline ? _settings.outlineThickness : 0f;
        Color fill        = new Color(_settings.color.r,        _settings.color.g,        _settings.color.b,        _settings.opacity);
        Color outlineColor = new Color(_settings.outlineColor.r, _settings.outlineColor.g, _settings.outlineColor.b, _settings.opacity);

        // Distance from center to the near edge of each line
        float halfGapLen = _settings.centerGap + _settings.lineLength * 0.5f;

        // top, bottom, left, right
        Vector2[] positions =
        {
            new Vector2(0f,          halfGapLen),
            new Vector2(0f,         -halfGapLen),
            new Vector2(-halfGapLen, 0f),
            new Vector2( halfGapLen, 0f)
        };
        Vector2[] sizes =
        {
            new Vector2(_settings.lineThickness, _settings.lineLength),
            new Vector2(_settings.lineThickness, _settings.lineLength),
            new Vector2(_settings.lineLength,    _settings.lineThickness),
            new Vector2(_settings.lineLength,    _settings.lineThickness)
        };

        // Outlines are just a slightly larger rect drawn behind each line
        Color outlineFill = outlineSize > 0f ? outlineColor : Color.clear;
        for (int i = 0; i < 4; i++)
        {
            Set(_lineOutlines[i], positions[i], sizes[i] + Vector2.one * (outlineSize * 2f), outlineFill);
            Set(_lines[i], positions[i], sizes[i], fill);
        }

        bool dotVisible = _settings.showCenterDot;
        Set(_dot, Vector2.zero, Vector2.one * _settings.centerDotSize, dotVisible ? fill : Color.clear);
        Set(_dotOutline, Vector2.zero, Vector2.one * (_settings.centerDotSize + outlineSize * 2f),
            dotVisible && outlineSize > 0f ? outlineColor : Color.clear);
    }

    private static void Set(Image img, Vector2 pos, Vector2 size, Color color)
    {
        img.color = color;
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = size;
    }
}
