using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WeaponHUD : HUDElement
{
    [SerializeField] private WeaponController _weapon;

    [Header("Colors")]
    [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color _textColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color _dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color _reloadColor = new Color(1f, 0.75f, 0.1f, 1f);
    [SerializeField] private Color _emptyColor = new Color(0.68f, 0.68f, 0.68f, 1f);
    [SerializeField] private Color _noWeaponColor = new Color(0.68f, 0.68f, 0.68f, 1f);

    [Header("Layout")]
    [SerializeField] private Vector2 _screenPadding = new Vector2(20f, 20f);
    [SerializeField] private Vector2 _innerPadding = new Vector2(12f, 10f);
    [SerializeField] private float _panelWidth = 180f;

    private TextMeshProUGUI _weaponNameText;
    private TextMeshProUGUI _magText;
    private TextMeshProUGUI _reserveText;
    private TextMeshProUGUI _reloadText;

    private int _magazine;
    private int _reserve;
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
        _magazine = magazine;
        _reserve = reserve;
        _isReloading = isReloading;
        Refresh();
    }

    public override void Refresh()
    {
        if (_magText == null) return;

        bool hasWeapon = _weapon != null && _weapon.Data != null;

        _weaponNameText.text  = hasWeapon ? _weapon.Data.WeaponName : string.Empty;
        _weaponNameText.color = _dimColor;

        if (!hasWeapon)
        {
            _reloadText.gameObject.SetActive(false);
            _magText.text = "-";
            _magText.color = _noWeaponColor;
            _reserveText.text = "|";
            _reserveText.color = _noWeaponColor;
            return;
        }

        if (_isReloading)
        {
            _magText.text = "-";
            _magText.color = _reloadColor;
            _reloadText.gameObject.SetActive(true);
            _reserveText.text = _reserve.ToString();
            _reserveText.color = _dimColor;
            return;
        }

        _reloadText.gameObject.SetActive(false);

        bool empty = _magazine == 0;
        _magText.text = _magazine.ToString();
        _magText.color = empty ? _emptyColor : _textColor;

        _reserveText.text = _reserve.ToString();
        _reserveText.color = _reserve == 0 ? _emptyColor : _dimColor;
    }

    private void SetupAnchor()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-_screenPadding.x, _screenPadding.y);
    }

    private void Build()
    {
        var self = GetComponent<RectTransform>();
        float ip = _innerPadding.x;
        float contentWidth = _panelWidth - ip * 2f;

        float totalHeight = _innerPadding.y + 18f + 44f + 20f + 18f + _innerPadding.y;
        self.sizeDelta = new Vector2(_panelWidth, totalHeight);

        float y = -_innerPadding.y;

        var bg = HudUIFactory.MakeImage("Background", self);
        bg.color = _backgroundColor;
        HudUIFactory.Stretch(bg.rectTransform);

        _weaponNameText = HudUIFactory.MakeText("WeaponName", self);
        _weaponNameText.color = _dimColor;
        _weaponNameText.fontSize = 12f;
        _weaponNameText.alignment = TextAlignmentOptions.Right;
        HudUIFactory.Place(_weaponNameText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 18f));
        y -= 18f;

        _magText = HudUIFactory.MakeText("MagCount", self);
        _magText.color = _textColor;
        _magText.fontSize = 36f;
        _magText.fontStyle = FontStyles.Bold;
        _magText.alignment = TextAlignmentOptions.Right;
        HudUIFactory.Place(_magText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 44f));
        y -= 44f;

        _reserveText = HudUIFactory.MakeText("Reserve", self);
        _reserveText.color = _dimColor;
        _reserveText.fontSize = 16f;
        _reserveText.alignment = TextAlignmentOptions.Right;
        HudUIFactory.Place(_reserveText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 20f));
        y -= 20f;

        _reloadText = HudUIFactory.MakeText("ReloadLabel", self);
        _reloadText.color = _reloadColor;
        _reloadText.fontSize = 13f;
        _reloadText.fontStyle = FontStyles.Bold;
        _reloadText.alignment = TextAlignmentOptions.Right;
        _reloadText.text = "RELOADING";
        HudUIFactory.Place(_reloadText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 18f));
        _reloadText.gameObject.SetActive(false);
    }
}
