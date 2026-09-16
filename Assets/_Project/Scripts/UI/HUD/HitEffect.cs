using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class HitEffect : HUDElement
{
    [SerializeField] private PlayerStats _playerStats;

    [Header("Flash")]
    [SerializeField] private Color _flashColor     = new Color(1f, 0.1f, 0.1f, 0.35f);
    [SerializeField] private float _flashFadeSpeed = 6f;

    [Header("Vignette")]
    [SerializeField] private float _vignetteMaxAlpha        = 0.75f;
    [SerializeField] private float _vignetteHealthThreshold = 0.5f;

    private Image       _vignetteImage;
    private CanvasGroup _flashGroup;

    private void Awake()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Build();

        if (_playerStats != null)
        {
            _playerStats.OnDamaged += OnPlayerDamaged;
            _playerStats.OnChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (_playerStats != null)
        {
            _playerStats.OnDamaged -= OnPlayerDamaged;
            _playerStats.OnChanged -= Refresh;
        }
    }

    private void Build()
    {
        var self = GetComponent<RectTransform>();

        // Vignette — rendered first so it sits behind the flash
        _vignetteImage             = HudUIFactory.MakeImage("Vignette", self);
        _vignetteImage.sprite      = CreateVignetteSprite();
        _vignetteImage.color       = new Color(1f, 0f, 0f, 0f);
        HudUIFactory.Stretch(_vignetteImage.rectTransform);

        // Flash — rendered on top of the vignette
        var flashImage    = HudUIFactory.MakeImage("Flash", self);
        flashImage.color  = _flashColor;
        _flashGroup        = flashImage.gameObject.AddComponent<CanvasGroup>();
        _flashGroup.alpha  = 0f;
        HudUIFactory.Stretch(flashImage.rectTransform);
    }

    private void OnPlayerDamaged(float amount)
    {
        _flashGroup.alpha = 1f;
    }

    public override void Refresh()
    {
        if (_playerStats == null || _vignetteImage == null) return;
        float ratio    = _playerStats.MaxHealth > 0f ? _playerStats.Health / _playerStats.MaxHealth : 0f;
        float vigAlpha = Mathf.Clamp01(1f - ratio / _vignetteHealthThreshold) * _vignetteMaxAlpha;
        _vignetteImage.color = new Color(1f, 0f, 0f, vigAlpha);
    }

    private void Update()
    {
        if (_flashGroup.alpha <= 0f) return;
        _flashGroup.alpha = Mathf.MoveTowards(_flashGroup.alpha, 0f, _flashFadeSpeed * Time.deltaTime);
    }

    private static Sprite CreateVignetteSprite()
    {
        const int Size = 128;
        var tex    = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var pixels = new Color32[Size * Size];
        float cx = Size * 0.5f, cy = Size * 0.5f;

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float nx   = (x - cx) / cx;
            float ny   = (y - cy) / cy;
            float dist = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny));
            byte  a    = (byte)(Mathf.Pow(dist, 1.8f) * 255f);
            pixels[y * Size + x] = new Color32(255, 0, 0, a);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
    }
}
