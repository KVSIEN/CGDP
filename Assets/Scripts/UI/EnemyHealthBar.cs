using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float   _fadeDelay   = 2.5f;
    [SerializeField] private float   _fadeSpeed   = 2f;
    [SerializeField] private Color   _fillColor   = new Color(0.2f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color   _bgColor     = new Color(0.85f, 0.15f, 0.1f, 1f);

    private Canvas      _canvas;
    private Image       _fill;
    private CanvasGroup _group;
    private Camera      _cam;
    private float       _fadeTimer;
    private bool        _active;

    private void Awake()
    {
        BuildCanvas();
        _canvas.gameObject.SetActive(false);
    }

    private void BuildCanvas()
    {
        var go = new GameObject("HealthBarCanvas");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = _worldOffset;

        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;

        var rt = (RectTransform)_canvas.transform;
        rt.sizeDelta  = new Vector2(200f, 20f);
        rt.localScale = Vector3.one * 0.005f;

        _group = go.AddComponent<CanvasGroup>();

        CreateStretchImage(go.transform, "BG", _bgColor);

        _fill = CreateStretchImage(go.transform, "Fill", _fillColor);
    }

    private static Image CreateStretchImage(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    public void ShowDamage(float current, float max)
    {
        float t = max > 0f ? current / max : 0f;
        _fill.rectTransform.anchorMax = new Vector2(t, 1f);
        _fill.rectTransform.offsetMax = Vector2.zero;
        _group.alpha = 1f;
        _fadeTimer       = _fadeDelay;
        _active          = true;
        _canvas.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (!_active) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
            _canvas.transform.rotation = _cam.transform.rotation;

        _fadeTimer -= Time.deltaTime;
        if (_fadeTimer >= 0f) return;

        _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, _fadeSpeed * Time.deltaTime);
        if (_group.alpha > 0f) return;

        _canvas.gameObject.SetActive(false);
        _active = false;
    }
}
