using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private const float FloatSpeed    = 50f;  // Adjusted for screen space (pixels per second)
    private const float FadeDelay     = 0.35f;
    private const float FadeSpeed     = 3.5f;
    private const float NormalScale   = 1f;
    private const float HeadshotScale = 1.2f;

    private static readonly Color NormalColor   = Color.white;
    private static readonly Color HeadshotColor = new Color(1f, 0.85f, 0.1f, 1f);

    private CanvasGroup _group;
    private float       _fadeTimer;

    public static void Spawn(float damage, Vector3 worldPos, bool headshot)
    {
        var go = new GameObject("DamagePopup");
        go.AddComponent<DamagePopup>().Setup(damage, worldPos, headshot);
    }

    private void Setup(float damage, Vector3 worldPos, bool headshot)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var rt = (RectTransform)canvas.transform;
        rt.sizeDelta  = new Vector2(200f, 60f);
        rt.localScale = Vector3.one * (headshot ? HeadshotScale : NormalScale);
        rt.position   = Camera.main.WorldToScreenPoint(worldPos);

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
        text.fontSize      = headshot ? 64f : 52f;
        text.fontStyle     = headshot ? FontStyles.Bold : FontStyles.Normal;
        text.alignment     = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private void Update()
    {
        if (_group == null) return;

        transform.position += Vector3.up * (FloatSpeed * Time.deltaTime);

        _fadeTimer -= Time.deltaTime;
        if (_fadeTimer >= 0f) return;

        _group.alpha -= FadeSpeed * Time.deltaTime;
        if (_group.alpha <= 0f)
            Destroy(gameObject);
    }
}
