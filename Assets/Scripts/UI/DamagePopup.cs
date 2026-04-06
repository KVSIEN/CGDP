using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private const float FloatSpeed = 1.5f;   // world units per second
    private const float FadeDelay  = 0.35f;
    private const float FadeSpeed  = 3.5f;
    private const float NormalPx   = 52f;
    private const float HeadshotPx = 64f;

    private static readonly Color NormalColor   = Color.red;
    private static readonly Color HeadshotColor = new Color(1f, 0.85f, 0.1f, 1f);

    private static Camera _overlayCamera;

    private CanvasGroup _group;
    private float       _fadeTimer;
    private Camera      _cam;

    // ── Overlay camera ────────────────────────────────────────────────────

    public static Camera GetOrCreateOverlayCamera()
    {
        if (_overlayCamera != null) return _overlayCamera;

        int layer   = LayerMask.NameToLayer("UI");
        var mainCam = Camera.main;

        // Parent to main camera so it always shares its transform
        var go = new GameObject("DamagePopupCamera");
        go.transform.SetParent(mainCam.transform, false);

        var cam = go.AddComponent<Camera>();
        cam.clearFlags  = CameraClearFlags.Depth;
        cam.cullingMask = 1 << layer;
        cam.depth       = mainCam.depth + 1;

        var camData = go.AddComponent<UniversalAdditionalCameraData>();
        camData.renderType = CameraRenderType.Overlay;

        // Add to main camera's URP stack
        var mainData = mainCam.GetComponent<UniversalAdditionalCameraData>();
        mainData.cameraStack.Add(cam);

        // Exclude layer from main camera so it isn't rendered twice
        mainCam.cullingMask &= ~(1 << layer);

        _overlayCamera = cam;
        return cam;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────

    public static void Spawn(float damage, Vector3 worldPos, bool headshot)
    {
        int layer = LayerMask.NameToLayer("UI");
        var go    = new GameObject("DamagePopup");
        go.layer  = layer;
        go.AddComponent<DamagePopup>().Setup(damage, worldPos, headshot);
    }

    private void Setup(float damage, Vector3 worldPos, bool headshot)
    {
        _cam = Camera.main;
        var overlayCam = GetOrCreateOverlayCamera();

        float dist       = Vector3.Distance(_cam.transform.position, worldPos);
        float unitPerPx  = dist * Mathf.Tan(overlayCam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / Screen.height;
        float targetPx   = headshot ? HeadshotPx : NormalPx;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode      = RenderMode.WorldSpace;
        canvas.worldCamera     = overlayCam;
        canvas.overrideSorting = true;
        canvas.sortingOrder    = 0;

        var rt = (RectTransform)canvas.transform;
        rt.sizeDelta  = new Vector2(200f, 60f);
        rt.position   = worldPos;
        rt.localScale = Vector3.one * (targetPx * unitPerPx / 60f);
        rt.rotation   = _cam.transform.rotation;

        _group     = gameObject.AddComponent<CanvasGroup>();
        _fadeTimer = FadeDelay;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(transform, false);
        textGo.layer = gameObject.layer;

        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text          = Mathf.RoundToInt(damage).ToString();
        text.color         = headshot ? HeadshotColor : NormalColor;
        text.fontSize      = headshot ? 48f : 36f;
        text.fontStyle     = headshot ? FontStyles.Bold : FontStyles.Normal;
        text.alignment     = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.outlineWidth  = 0.25f;
        text.outlineColor  = Color.black;
    }

    // ── Update ────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (_group == null) return;

        transform.position += Vector3.up * (FloatSpeed * Time.deltaTime);
        transform.rotation  = _cam.transform.rotation;

        _fadeTimer -= Time.deltaTime;
        if (_fadeTimer >= 0f) return;

        _group.alpha -= FadeSpeed * Time.deltaTime;
        if (_group.alpha <= 0f)
            Destroy(gameObject);
    }
}
