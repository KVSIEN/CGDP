using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CGD.UI
{
    // The health (and shield) bar floating over an enemy. By default it appears when the enemy
    // is hit and fades out after a moment; "always visible" keeps it up with its numbers, for
    // targets whose state is the thing being looked at (the firing range's dummies).
    public class EnemyHealthBar : MonoBehaviour
    {
        private const float BarScale  = 0.005f; // world metres per canvas unit
        private const float PunchTime = 0.15f;

        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private float   _fadeDelay   = 2.5f;
        [SerializeField] private float   _fadeSpeed   = 2f;
        [SerializeField] private Color   _fillColor   = new Color(0.2f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color   _bgColor     = new Color(0.85f, 0.15f, 0.1f, 1f);
        [SerializeField] private Color   _shieldColor = new Color(0.35f, 0.65f, 1f, 1f);

        [Header("Damage trail")]
        [Tooltip("Colour of the band that shows what a hit just took off")]
        [SerializeField] private Color _trailColor = Color.white;
        [Tooltip("Seconds the band waits before shrinking onto the remaining health")]
        [SerializeField, Min(0f)] private float _trailHold = 0.35f;
        [Tooltip("Share of the bar the band shrinks per second")]
        [SerializeField, Min(0.1f)] private float _trailSpeed = 1.5f;
        [Tooltip("How much the bar swells on a hit, fading back")]
        [SerializeField, Min(0f)] private float _punch = 0.12f;

        [Header("Always-on display")]
        [Tooltip("Stay visible instead of fading out after a hit")]
        [SerializeField] private bool _alwaysVisible;
        [Tooltip("Write current health and shield under the bar")]
        [SerializeField] private bool _showNumbers;

        private Canvas          _canvas;
        private TrailingFill    _health;
        private TrailingFill    _shield;
        private RectTransform   _shieldBar;
        private TextMeshProUGUI _numbers;
        private CanvasGroup     _group;
        private Camera          _cam;
        private float           _fadeTimer;
        private float           _punchTimer;
        private bool            _active;

        private void Awake()
        {
            BuildCanvas();
            if (_alwaysVisible) Reveal();
            else _canvas.gameObject.SetActive(false);
        }

        private void BuildCanvas()
        {
            var go = new GameObject("HealthBarCanvas");
            go.transform.SetParent(transform, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            // Placed only after switching to WorldSpace: a new Canvas starts in overlay
            // mode, which drives (and overwrites) its RectTransform.
            var rt = (RectTransform)_canvas.transform;
            rt.sizeDelta     = new Vector2(200f, 20f);
            rt.localPosition = _worldOffset;
            rt.localScale    = Vector3.one * BarScale;

            _group = go.AddComponent<CanvasGroup>();

            _health = BuildFill(rt, _bgColor, _fillColor);

            BuildShieldBar(rt);
            if (_showNumbers) BuildNumbers(rt);
        }

        // A thinner strip on top of the health bar; only shown for enemies that have a shield.
        private void BuildShieldBar(RectTransform parent)
        {
            _shieldBar = new GameObject("Shield", typeof(RectTransform)).GetComponent<RectTransform>();
            _shieldBar.SetParent(parent, false);
            _shieldBar.anchorMin = new Vector2(0f, 1f);
            _shieldBar.anchorMax = new Vector2(1f, 1f);
            _shieldBar.pivot     = new Vector2(0.5f, 0f);
            _shieldBar.anchoredPosition = new Vector2(0f, 2f);
            _shieldBar.sizeDelta = new Vector2(0f, 8f);

            _shield = BuildFill(_shieldBar, new Color(0f, 0f, 0f, 0.6f), _shieldColor);
            _shieldBar.gameObject.SetActive(false);
        }

        // Background, then the damage trail, then the fill on top.
        private TrailingFill BuildFill(RectTransform parent, Color background, Color fill)
        {
            CreateStretchImage(parent, "BG", background);
            Image trail = CreateStretchImage(parent, "Trail", _trailColor);
            Image front = CreateStretchImage(parent, "Fill", fill);
            return new TrailingFill(front.rectTransform, trail.rectTransform, _trailHold, _trailSpeed);
        }

        private void BuildNumbers(RectTransform parent)
        {
            _numbers = UIFactory.MakeText("Numbers", parent);
            _numbers.rectTransform.anchorMin = new Vector2(0f, 0f);
            _numbers.rectTransform.anchorMax = new Vector2(1f, 0f);
            _numbers.rectTransform.pivot     = new Vector2(0.5f, 1f);
            _numbers.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            _numbers.rectTransform.sizeDelta = new Vector2(0f, 24f);
            _numbers.fontSize  = 20f;
            _numbers.alignment = TextAlignmentOptions.Center;
            _numbers.outlineWidth = 0.25f;
            _numbers.outlineColor = Color.black;
        }

        private static Image CreateStretchImage(Transform parent, string name, Color color)
        {
            var img = UIFactory.MakeImage(name, (RectTransform)parent);
            img.color = color;
            UIFactory.Stretch(img.rectTransform);
            return img;
        }

        // Brings the bar up (and starts its fade-out timer) after a hit.
        public void ShowDamage(float health, float maxHealth, float shield, float maxShield)
        {
            Refresh(health, maxHealth, shield, maxShield);
            _punchTimer = PunchTime;
            Reveal();
        }

        // Updates the fills without bringing the bar up, e.g. as a shield regenerates.
        public void Refresh(float health, float maxHealth, float shield, float maxShield)
        {
            if (_health == null) return;

            _health.Set(maxHealth > 0f ? health / maxHealth : 0f);
            _shieldBar.gameObject.SetActive(maxShield > 0f);
            _shield.Set(maxShield > 0f ? shield / maxShield : 0f);

            if (_numbers != null)
                _numbers.text = maxShield > 0f
                    ? $"{health:0} / {maxHealth:0}   <color=#{ColorUtility.ToHtmlStringRGB(_shieldColor)}>{shield:0} / {maxShield:0}</color>"
                    : $"{health:0} / {maxHealth:0}";
        }

        private void Reveal()
        {
            _group.alpha = 1f;
            _fadeTimer   = _fadeDelay;
            _active      = true;
            _canvas.gameObject.SetActive(true);
        }

        // The bar swells on a hit and eases back.
        private void Punch()
        {
            if (_punchTimer <= 0f) return;

            _punchTimer = Mathf.Max(0f, _punchTimer - Time.deltaTime);
            _canvas.transform.localScale = Vector3.one * (BarScale * (1f + _punch * _punchTimer / PunchTime));
        }

        private void LateUpdate()
        {
            if (!_active) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
                _canvas.transform.rotation = _cam.transform.rotation;

            _health.Tick(Time.deltaTime);
            _shield.Tick(Time.deltaTime);
            Punch();

            if (_alwaysVisible) return;

            _fadeTimer -= Time.deltaTime;
            if (_fadeTimer >= 0f) return;

            _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, _fadeSpeed * Time.deltaTime);
            if (_group.alpha > 0f) return;

            _canvas.gameObject.SetActive(false);
            _active = false;
        }
    }
}
