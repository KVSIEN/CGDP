using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace CGD.UI
{
    // Floating damage number. Popups are pooled: each is built once and reused, so a
    // shotgun blast doesn't create a canvas per pellet.
    public class DamagePopup : MonoBehaviour
    {
        private const float FloatSpeed = 1.5f;   // world units per second
        private const float FadeDelay  = 0.35f;
        private const float FadeSpeed  = 3.5f;
        private const float NormalPx   = 52f;
        private const float HeadshotPx = 64f;

        private static readonly Color NormalColor   = Color.red;
        private static readonly Color HeadshotColor = new Color(1f, 0.85f, 0.1f, 1f);

        private static readonly Stack<DamagePopup> _pool = new();
        private static Transform _poolRoot;
        private static Camera    _overlayCamera;

        private Canvas          _canvas;
        private CanvasGroup     _group;
        private TextMeshProUGUI _text;
        private float           _fadeTimer;
        private Camera          _cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _pool.Clear();
            _poolRoot      = null;
            _overlayCamera = null;
        }

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
            DamagePopup popup = null;
            while (popup == null && _pool.Count > 0)
                popup = _pool.Pop();

            if (popup == null) popup = Create();
            popup.Show(damage, worldPos, headshot);
        }

        private static DamagePopup Create()
        {
            if (_poolRoot == null)
            {
                var root = new GameObject("DamagePopups");
                DontDestroyOnLoad(root);
                _poolRoot = root.transform;
            }

            var go = new GameObject("DamagePopup");
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(_poolRoot, false);

            var popup = go.AddComponent<DamagePopup>();
            popup.Build();
            return popup;
        }

        private void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode      = RenderMode.WorldSpace;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder    = 0;

            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(200f, 60f);

            _group = gameObject.AddComponent<CanvasGroup>();

            _text = UIFactory.MakeText("Text", (RectTransform)transform, gameObject.layer);
            UIFactory.Stretch(_text.rectTransform);
            _text.alignment    = TextAlignmentOptions.Center;
            _text.outlineWidth = 0.25f;
            _text.outlineColor = Color.black;
        }

        private void Show(float damage, Vector3 worldPos, bool headshot)
        {
            _cam = Camera.main;
            var overlayCam = GetOrCreateOverlayCamera();
            _canvas.worldCamera = overlayCam;

            float dist      = Vector3.Distance(_cam.transform.position, worldPos);
            float unitPerPx = dist * Mathf.Tan(overlayCam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / Screen.height;
            float targetPx  = headshot ? HeadshotPx : NormalPx;

            var rt = (RectTransform)transform;
            rt.position   = worldPos;
            rt.localScale = Vector3.one * (targetPx * unitPerPx / 60f);
            rt.rotation   = _cam.transform.rotation;

            _text.text      = Mathf.RoundToInt(damage).ToString();
            _text.color     = headshot ? HeadshotColor : NormalColor;
            _text.fontSize  = headshot ? 48f : 36f;
            _text.fontStyle = headshot ? FontStyles.Bold : FontStyles.Normal;

            _group.alpha = 1f;
            _fadeTimer   = FadeDelay;
            gameObject.SetActive(true);
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (_cam == null)
            {
                Release();
                return;
            }

            transform.position += Vector3.up * (FloatSpeed * Time.deltaTime);
            transform.rotation  = _cam.transform.rotation;

            _fadeTimer -= Time.deltaTime;
            if (_fadeTimer >= 0f) return;

            _group.alpha -= FadeSpeed * Time.deltaTime;
            if (_group.alpha <= 0f)
                Release();
        }

        private void Release()
        {
            gameObject.SetActive(false);
            _pool.Push(this);
        }
    }
}
