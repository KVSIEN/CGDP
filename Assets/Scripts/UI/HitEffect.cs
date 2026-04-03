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
        var vigGo = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
        vigGo.transform.SetParent(self, false);
        _vignetteImage             = vigGo.GetComponent<Image>();
        _vignetteImage.sprite      = CreateVignetteSprite();
        _vignetteImage.color       = new Color(1f, 0f, 0f, 0f);
        _vignetteImage.raycastTarget = false;
        StretchRT(vigGo.GetComponent<RectTransform>());

        // Flash — rendered on top of the vignette
        var flashGo = new GameObject("Flash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        flashGo.transform.SetParent(self, false);
        var flashImage             = flashGo.GetComponent<Image>();
        flashImage.color           = _flashColor;
        flashImage.raycastTarget   = false;
        _flashGroup                = flashGo.GetComponent<CanvasGroup>();
        _flashGroup.alpha          = 0f;
        StretchRT(flashGo.GetComponent<RectTransform>());
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

    private static void StretchRT(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
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
