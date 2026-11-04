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
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private float   _fadeDelay   = 2.5f;
        [SerializeField] private float   _fadeSpeed   = 2f;
        [SerializeField] private Color   _fillColor   = new Color(0.2f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color   _bgColor     = new Color(0.85f, 0.15f, 0.1f, 1f);
        [SerializeField] private Color   _shieldColor = new Color(0.35f, 0.65f, 1f, 1f);

        [Header("Always-on display")]
        [Tooltip("Stay visible instead of fading out after a hit")]
        [SerializeField] private bool _alwaysVisible;
        [Tooltip("Write current health and shield under the bar")]
        [SerializeField] private bool _showNumbers;

        private Canvas          _canvas;
        private Image           _fill;
        private RectTransform   _shieldBar;
        private Image           _shieldFill;
        private TextMeshProUGUI _numbers;
        private CanvasGroup     _group;
        private Camera          _cam;
        private float           _fadeTimer;
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
            rt.localScale    = Vector3.one * 0.005f;

            _group = go.AddComponent<CanvasGroup>();

            CreateStretchImage(go.transform, "BG", _bgColor);
            _fill = CreateStretchImage(go.transform, "Fill", _fillColor);

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

            CreateStretchImage(_shieldBar, "BG", new Color(0f, 0f, 0f, 0.6f));
            _shieldFill = CreateStretchImage(_shieldBar, "Fill", _shieldColor);
            _shieldBar.gameObject.SetActive(false);
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
            Reveal();
        }

        // Updates the fills without bringing the bar up, e.g. as a shield regenerates.
        public void Refresh(float health, float maxHealth, float shield, float maxShield)
        {
            if (_fill == null) return;

            SetFill(_fill, health, maxHealth);
            _shieldBar.gameObject.SetActive(maxShield > 0f);
            SetFill(_shieldFill, shield, maxShield);

            if (_numbers != null)
                _numbers.text = maxShield > 0f
                    ? $"{health:0} / {maxHealth:0}   <color=#{ColorUtility.ToHtmlStringRGB(_shieldColor)}>{shield:0} / {maxShield:0}</color>"
                    : $"{health:0} / {maxHealth:0}";
        }

        private static void SetFill(Image fill, float current, float max)
        {
            float t = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            fill.rectTransform.anchorMax = new Vector2(t, 1f);
            fill.rectTransform.offsetMax = Vector2.zero;
        }

        private void Reveal()
        {
            _group.alpha = 1f;
            _fadeTimer   = _fadeDelay;
            _active      = true;
            _canvas.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!_active) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
                _canvas.transform.rotation = _cam.transform.rotation;

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
