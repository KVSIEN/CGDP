using UnityEngine;
using TMPro;
using CGD.Enemies;

namespace CGD.UI
{
    // The "?" / "!" over an enemy, so the player can read what it believes:
    //   ?  yellow, growing  something caught its eye or ear; it's working out what
    //   ?  orange           it knows someone is about and is hunting for them
    //   !  red              it has you — shown for a moment when it engages
    // Nothing while it's unaware or dead. Builds its own world-space canvas.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyAwarenessIndicator : MonoBehaviour
    {
        private const float CanvasScale = 0.01f;

        [SerializeField] private Vector3 _worldOffset    = new(0f, 2.6f, 0f);
        [SerializeField] private Color   _suspiciousColor = new(1f, 0.85f, 0.2f);
        [SerializeField] private Color   _searchingColor  = new(1f, 0.5f, 0.1f);
        [SerializeField] private Color   _engagedColor    = new(1f, 0.15f, 0.1f);
        [Tooltip("Seconds the ! stays up after the enemy engages")]
        [SerializeField, Min(0f)] private float _engagedShowTime = 1.5f;
        [Tooltip("How fast the mark fades in and out")]
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 4f;

        private EnemyAI         _ai;
        private Canvas          _canvas;
        private TextMeshProUGUI _mark;
        private Camera          _camera;
        private AwarenessLevel  _shownLevel;
        private float           _engagedTimer;
        private float           _alpha;

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            Build();
        }

        private void LateUpdate()
        {
            bool alive = _ai.State != EnemyAI.AiState.Dead;
            AwarenessLevel level = alive ? _ai.Awareness : AwarenessLevel.Unaware;

            if (level == AwarenessLevel.Engaged && _shownLevel != AwarenessLevel.Engaged) _engagedTimer = _engagedShowTime;
            _shownLevel = level;
            _engagedTimer -= Time.deltaTime;

            float target = level switch
            {
                AwarenessLevel.Suspicious => 0.45f + 0.55f * _ai.AwarenessValue,
                AwarenessLevel.Searching  => 1f,
                AwarenessLevel.Engaged    => _engagedTimer > 0f ? 1f : 0f,
                _                         => 0f,
            };
            _alpha = Mathf.MoveTowards(_alpha, target, _fadeSpeed * Time.deltaTime);

            bool visible = _alpha > 0.01f;
            if (_canvas.enabled != visible) _canvas.enabled = visible;
            if (!visible) return;

            ShowLevel(level);
            Billboard();
        }

        private void ShowLevel(AwarenessLevel level)
        {
            bool engaged = level == AwarenessLevel.Engaged;
            Color color  = engaged ? _engagedColor : level == AwarenessLevel.Searching ? _searchingColor : _suspiciousColor;
            color.a      = _alpha;

            _mark.text  = engaged ? "!" : "?";
            _mark.color = color;

            // A curious mark grows as it makes up its mind.
            float size = level == AwarenessLevel.Suspicious ? Mathf.Lerp(0.7f, 1f, _ai.AwarenessValue) : 1f;
            _mark.rectTransform.localScale = Vector3.one * size;
        }

        private void Billboard()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) _canvas.transform.rotation = _camera.transform.rotation;
        }

        private void Build()
        {
            var go = new GameObject("AwarenessCanvas");
            go.transform.SetParent(transform, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            // Placed only after switching to WorldSpace (see EnemyHealthBar).
            var rt = (RectTransform)_canvas.transform;
            rt.sizeDelta     = new Vector2(60f, 60f);
            rt.localPosition = _worldOffset;
            rt.localScale    = Vector3.one * CanvasScale;

            _mark = UIFactory.MakeText("Mark", rt);
            UIFactory.Stretch(_mark.rectTransform);
            _mark.fontSize     = 56f;
            _mark.fontStyle    = FontStyles.Bold;
            _mark.alignment    = TextAlignmentOptions.Center;
            _mark.outlineWidth = 0.25f;
            _mark.outlineColor = Color.black;

            _canvas.enabled = false;
        }
    }
}
