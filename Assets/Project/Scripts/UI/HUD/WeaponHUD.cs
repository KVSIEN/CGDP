using TMPro;
using UnityEngine;
using CGD.Weapons;

namespace CGD.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class WeaponHUD : HUDElement
    {
        [SerializeField] private WeaponController _weapon;


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
            UIFactory.AnchorToCorner(GetComponent<RectTransform>(), new Vector2(1f, 0f), _screenPadding);
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
            _weaponNameText.color = Theme.TextMuted;

            if (!hasWeapon)
            {
                _reloadText.gameObject.SetActive(false);
                _magText.text = "-";
                _magText.color = Theme.TextSecondary;
                _reserveText.text = "|";
                _reserveText.color = Theme.TextSecondary;
                return;
            }

            if (_isReloading)
            {
                _magText.text = "-";
                _magText.color = Theme.Warning;
                _reloadText.gameObject.SetActive(true);
                _reserveText.text = _reserve.ToString();
                _reserveText.color = Theme.TextMuted;
                return;
            }

            _reloadText.gameObject.SetActive(false);

            bool empty = _magazine == 0;
            _magText.text = _magazine.ToString();
            _magText.color = empty ? Theme.Danger : Theme.TextPrimary;

            _reserveText.text = _reserve.ToString();
            _reserveText.color = _reserve == 0 ? Theme.Danger : Theme.TextMuted;
        }

        private void Build()
        {
            var self = GetComponent<RectTransform>();
            float ip = _innerPadding.x;
            float contentWidth = _panelWidth - ip * 2f;

            float totalHeight = _innerPadding.y + 18f + 44f + 20f + 18f + _innerPadding.y;
            self.sizeDelta = new Vector2(_panelWidth, totalHeight);

            float y = -_innerPadding.y;

            var bg = UIFactory.MakeImage("Background", self);
            bg.color = Theme.Panel;
            UIFactory.Stretch(bg.rectTransform);

            _weaponNameText = UIFactory.MakeText("WeaponName", self);
            _weaponNameText.color = Theme.TextMuted;
            _weaponNameText.fontSize = 12f;
            _weaponNameText.alignment = TextAlignmentOptions.Right;
            UIFactory.Place(_weaponNameText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 18f));
            y -= 18f;

            _magText = UIFactory.MakeText("MagCount", self);
            _magText.color = Theme.TextPrimary;
            _magText.fontSize = 36f;
            _magText.fontStyle = FontStyles.Bold;
            _magText.alignment = TextAlignmentOptions.Right;
            UIFactory.Place(_magText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 44f));
            y -= 44f;

            _reserveText = UIFactory.MakeText("Reserve", self);
            _reserveText.color = Theme.TextMuted;
            _reserveText.fontSize = 16f;
            _reserveText.alignment = TextAlignmentOptions.Right;
            UIFactory.Place(_reserveText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 20f));
            y -= 20f;

            _reloadText = UIFactory.MakeText("ReloadLabel", self);
            _reloadText.color = Theme.Warning;
            _reloadText.fontSize = 13f;
            _reloadText.fontStyle = FontStyles.Bold;
            _reloadText.alignment = TextAlignmentOptions.Right;
            _reloadText.text = "RELOADING";
            UIFactory.Place(_reloadText.rectTransform, new Vector2(ip, y), new Vector2(contentWidth, 18f));
            _reloadText.gameObject.SetActive(false);
        }
    }
}
