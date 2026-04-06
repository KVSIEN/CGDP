using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays weapon magazine, reserve ammo, and a reload indicator.
/// Anchor: bottom-right corner.
/// Wire WeaponController in the Inspector.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AmmoHUD : HUDElement
{
    [SerializeField] private WeaponController _weapon;

    [Header("Colors")]
    [SerializeField] private Color _backgroundColor  = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color _textColor        = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color _dimColor         = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color _reloadColor      = new Color(1f, 0.75f, 0.1f, 1f);
    [SerializeField] private Color _emptyColor       = new Color(0.68f, 0.68f, 0.68f, 1f);

    [Header("Layout")]
    [SerializeField] private Vector2 _screenPadding = new Vector2(20f, 20f);
    [SerializeField] private Vector2 _innerPadding  = new Vector2(12f, 10f);
    [SerializeField] private float   _panelWidth    = 180f;

    private TextMeshProUGUI _magText;
    private TextMeshProUGUI _reserveText;
    private TextMeshProUGUI _reloadText;

    private int  _magazine;
    private int  _reserve;
    private bool _isReloading;

    private void Awake()
    {
        SetupAnchor();
        Build();
    }

    private void Start()
    {
        if (_weapon != null)
        {
            _weapon.OnAmmoChanged += OnAmmoChanged;
            // Read weapon state after all Awake calls have run
            OnAmmoChanged(_weapon.Magazine, _weapon.Reserve, _weapon.IsReloading);
        }
    }

    private void OnDestroy()
    {
        if (_weapon != null)
            _weapon.OnAmmoChanged -= OnAmmoChanged;
    }

    private void OnAmmoChanged(int magazine, int reserve, bool isReloading)
    {
        _magazine    = magazine;
        _reserve     = reserve;
        _isReloading = isReloading;
        Refresh();
    }

    public override void Refresh()
    {
        if (_magText == null) return;

        bool hasWeapon = _weapon != null && _weapon.Data != null;

        if (!hasWeapon)
        {
            _reloadText.gameObject.SetActive(false);
            _magText.text      = "—";
            _magText.color     = _emptyColor;
            _reserveText.text  = "│";
            _reserveText.color = _emptyColor;
            return;
        }

        if (_isReloading)
        {
            _magText.text     = "—";
            _magText.color    = _reloadColor;
            _reloadText.gameObject.SetActive(true);
            _reserveText.text  = _reserve.ToString();
            _reserveText.color = _dimColor;
            return;
        }

        _reloadText.gameObject.SetActive(false);

        bool empty = _magazine == 0;
        _magText.text  = _magazine.ToString();
        _magText.color = empty ? _emptyColor : _textColor;

        _reserveText.text  = _reserve > 0 ? _reserve.ToString() : "—";
        _reserveText.color = _reserve == 0 ? _emptyColor : _dimColor;
    }

    // ── Build ─────────────────────────────────────────────────────────────
    private void SetupAnchor()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-_screenPadding.x, _screenPadding.y);
    }

    private void Build()
    {
        var self = GetComponent<RectTransform>();
        float ip           = _innerPadding.x;
        float contentWidth = _panelWidth - ip * 2f;

        // Calculate total height first and set sizeDelta so children are positioned correctly
        float totalHeight = _innerPadding.y + 44f + 20f + 18f + _innerPadding.y;
        self.sizeDelta = new Vector2(_panelWidth, totalHeight);

        float y = -_innerPadding.y;

        var bg = MakeImage("Background", self);
        bg.color = _backgroundColor;
        Stretch(bg.rectTransform);

        // Large magazine count — right-aligned, prominent
        _magText = MakeText("MagCount", self);
        _magText.color     = _textColor;
        _magText.fontSize  = 36f;
        _magText.fontStyle = FontStyles.Bold;
        _magText.alignment = TextAlignmentOptions.Right;
        Place(_magText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 44f));
        y -= 44f;

        // Reserve — smaller, dimmed
        _reserveText = MakeText("Reserve", self);
        _reserveText.color     = _dimColor;
        _reserveText.fontSize  = 16f;
        _reserveText.alignment = TextAlignmentOptions.Right;
        Place(_reserveText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 20f));
        y -= 20f;

        // Reload label — hidden by default
        _reloadText = MakeText("ReloadLabel", self);
        _reloadText.color     = _reloadColor;
        _reloadText.fontSize  = 13f;
        _reloadText.fontStyle = FontStyles.Bold;
        _reloadText.alignment = TextAlignmentOptions.Right;
        _reloadText.text      = "RELOADING";
        Place(_reloadText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 18f));
        _reloadText.gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static Image MakeImage(string n, RectTransform parent)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string n, RectTransform parent)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        return t;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
