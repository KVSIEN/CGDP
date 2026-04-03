using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private const float FloatSpeed    = 1.4f;
    private const float FadeDelay     = 0.35f;
    private const float FadeSpeed     = 3.5f;
    private const float NormalScale   = 0.004f;
    private const float HeadshotScale = 0.006f;

    private static readonly Color NormalColor   = Color.white;
    private static readonly Color HeadshotColor = new Color(1f, 0.85f, 0.1f, 1f);

    private CanvasGroup _group;
    private Camera      _cam;
    private float       _fadeTimer;

    public static void Spawn(float damage, Vector3 worldPos, bool headshot)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = worldPos;
        go.AddComponent<DamagePopup>().Setup(damage, headshot);
    }

    private void Setup(float damage, bool headshot)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)canvas.transform;
        rt.sizeDelta  = new Vector2(200f, 60f);
        rt.localScale = Vector3.one * (headshot ? HeadshotScale : NormalScale);

        _group     = gameObject.AddComponent<CanvasGroup>();
        _fadeTimer = FadeDelay;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text          = Mathf.RoundToInt(damage).ToString();
        text.color         = headshot ? HeadshotColor : NormalColor;
        text.fontSize      = headshot ? 52f : 36f;
        text.fontStyle     = headshot ? FontStyles.Bold : FontStyles.Normal;
        text.alignment     = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
            transform.rotation = _cam.transform.rotation;

        transform.position += Vector3.up * (FloatSpeed * Time.deltaTime);

        _fadeTimer -= Time.deltaTime;
        if (_fadeTimer >= 0f) return;

        _group.alpha -= FadeSpeed * Time.deltaTime;
        if (_group.alpha <= 0f)
            Destroy(gameObject);
    }
}
