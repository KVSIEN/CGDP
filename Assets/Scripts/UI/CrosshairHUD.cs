using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CrosshairHUD : HUDElement
{
    [SerializeField] private CrosshairSettings _settings;
    [SerializeField] private PlayerCamera _playerCamera;

    [Header("Spread")]
    [Tooltip("Scale multiplier on top of the FOV-derived px/deg conversion. 1 = physically accurate.")]
    [SerializeField] private float _spreadScale = 1f;

    [Header("Obstruction Dot")]
    [SerializeField] private float _obstructionDotSize = 6f;
    [SerializeField] private Color _obstructionDotColor = new Color(1f, 0.15f, 0.15f, 0.9f);

    private readonly Image[] _lines = new Image[4];
    private readonly Image[] _lineOutlines = new Image[4];
    private Image _dot;
    private Image _dotOutline;
    private Image _obstructionDot;
    private float _dynamicSpreadPixels;

    private void Awake()
    {
        for (int i = 0; i < 4; i++)
        {
            _lineOutlines[i] = CreateImage("LineOutline_" + i);
            _lines[i] = CreateImage("Line_" + i);
        }
        _dotOutline = CreateImage("DotOutline");
        _dot = CreateImage("Dot");
        _obstructionDot = CreateImage("ObstructionDot");
        _obstructionDot.color = Color.clear;

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

    private void Update()
    {
        if (_playerCamera == null || _obstructionDot == null) return;

        if (_playerCamera.IsAimObstructed)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                _playerCamera.Camera, _playerCamera.ObstructionPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, screenPoint, null, out Vector2 localPos);
            _obstructionDot.rectTransform.anchoredPosition = localPos;
            _obstructionDot.rectTransform.sizeDelta = Vector2.one * _obstructionDotSize;
            _obstructionDot.color = _obstructionDotColor;
        }
        else
        {
            _obstructionDot.color = Color.clear;
        }
    }

    /// <summary>
    /// Called by WeaponController every frame with the current spread in degrees.
    /// Pass 0 when no weapon is equipped.
    /// </summary>
    public void SetDynamicSpread(float spreadDegrees)
    {
        // Derive px/deg from the live camera FOV so the crosshair scales correctly
        // at all FOVs (hip, ADS, sprint) instead of using a hardcoded constant.
        if (_playerCamera == null || _playerCamera.Camera == null) return;
        float canvasHeight = ((RectTransform)transform.root).rect.height;
        float pixPerDeg    = canvasHeight / _playerCamera.Camera.fieldOfView;
        _dynamicSpreadPixels = spreadDegrees * pixPerDeg * _spreadScale;
        ApplyLinePositions();
    }

    public override void Refresh()
    {
        if (_settings == null) return;
        ApplyColors();
        ApplyLinePositions();
        ApplyDot();
    }

    private void ApplyColors()
    {
        float outlineSize  = _settings.showOutline ? _settings.outlineThickness : 0f;
        Color fill         = new Color(_settings.color.r, _settings.color.g, _settings.color.b, _settings.opacity);
        Color outlineColor = new Color(_settings.outlineColor.r, _settings.outlineColor.g, _settings.outlineColor.b, _settings.opacity);
        Color outlineFill  = outlineSize > 0f ? outlineColor : Color.clear;

        Vector2[] sizes =
        {
            new Vector2(_settings.lineThickness, _settings.lineLength),
            new Vector2(_settings.lineThickness, _settings.lineLength),
            new Vector2(_settings.lineLength,    _settings.lineThickness),
            new Vector2(_settings.lineLength,    _settings.lineThickness)
        };

        for (int i = 0; i < 4; i++)
        {
            _lineOutlines[i].color = outlineFill;
            _lineOutlines[i].rectTransform.sizeDelta = sizes[i] + Vector2.one * (outlineSize * 2f);
            _lines[i].color = fill;
            _lines[i].rectTransform.sizeDelta = sizes[i];
        }
    }

    private void ApplyLinePositions()
    {
        if (_settings == null || _lines[0] == null) return;

        float outlineSize = _settings.showOutline ? _settings.outlineThickness : 0f;
        float halfGapLen  = _settings.centerGap + _settings.lineLength * 0.5f + _dynamicSpreadPixels;

        Vector2[] positions =
        {
            new Vector2(0f,           halfGapLen),
            new Vector2(0f,          -halfGapLen),
            new Vector2(-halfGapLen,  0f),
            new Vector2( halfGapLen,  0f)
        };

        for (int i = 0; i < 4; i++)
        {
            _lines[i].rectTransform.anchoredPosition        = positions[i];
            _lineOutlines[i].rectTransform.anchoredPosition = positions[i];
        }
    }

    private void ApplyDot()
    {
        if (_settings == null) return;
        float outlineSize = _settings.showOutline ? _settings.outlineThickness : 0f;
        Color fill        = new Color(_settings.color.r, _settings.color.g, _settings.color.b, _settings.opacity);
        Color outlineColor = new Color(_settings.outlineColor.r, _settings.outlineColor.g, _settings.outlineColor.b, _settings.opacity);
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
