using UnityEngine;
using TMPro;
using CGD.Enemies;

namespace CGD.UI
{
    // The "?" / "!" over an enemy, so the player can read what it believes:
    //   ?  yellow, growing  something caught its eye or ear; it's working out what
    //   ?  orange           it knows someone is about and is hunting for them
    //   !  red              it has you — shown for a moment when it engages
    //   …  grey             it stands down after a fight
    // Nothing while it's unaware or dead. Builds its own world-space canvas. Each faction's
    // tell (EnemyAI.Told) moves the mark its own way: a scanner's quick wobble, a beast's bob
    // or pumped-up roar, a stare's slow, flickering swell. The mark takes a tint of the
    // faction's colour, so what is looking at you reads from a distance.
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
        [Tooltip("How much of the faction's colour the mark takes on")]
        [SerializeField, Range(0f, 1f)] private float _factionTint = 0.35f;
        [SerializeField] private Color _standDownColor = new(0.7f, 0.7f, 0.7f);

        private EnemyAI         _ai;
        private Canvas          _canvas;
        private TextMeshProUGUI _mark;
        private Camera          _camera;
        private AwarenessLevel  _shownLevel;
        private float           _engagedTimer;
        private float           _alpha;
        private TellKind        _tell;
        private float           _tellAt = float.NegativeInfinity;

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            Build();
        }

        private void OnEnable() => _ai.Told += OnTold;

        private void OnDisable() => _ai.Told -= OnTold;

        private void OnTold(TellKind kind)
        {
            _tell   = kind;
            _tellAt = Time.time;
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
            // A tell holds the mark up for as long as it plays, even for the quiet stand-down.
            float tellAge = Time.time - _tellAt;
            float tellLength = TellLength(_tell);
            bool telling = alive && _tell != TellKind.None && tellAge < tellLength;
            if (telling) target = 1f;

            _alpha = Mathf.MoveTowards(_alpha, target, _fadeSpeed * Time.deltaTime);

            bool visible = _alpha > 0.01f;
            if (_canvas.enabled != visible) _canvas.enabled = visible;
            if (!visible) return;

            ShowLevel(level, telling ? _tell : TellKind.None, telling ? tellAge / tellLength : 0f);
            Billboard();
        }

        private void ShowLevel(AwarenessLevel level, TellKind tell, float tellProgress)
        {
            bool quiet   = IsStandDown(tell);
            bool engaged = !quiet && (level == AwarenessLevel.Engaged || tell is TellKind.LockOn or TellKind.Roar or TellKind.StepUp);
            Color color  = quiet ? _standDownColor
                         : engaged ? _engagedColor
                         : level == AwarenessLevel.Searching ? _searchingColor : _suspiciousColor;
            Color faction = _ai.Data.DamageNumberColor;
            if (faction.a > 0f) color = Color.Lerp(color, faction, _factionTint);
            color.a = _alpha;

            _mark.text = quiet ? "…" : engaged ? "!" : "?";

            // A curious mark grows as it makes up its mind.
            float size = level == AwarenessLevel.Suspicious ? Mathf.Lerp(0.7f, 1f, _ai.AwarenessValue) : 1f;
            float lift = 0f;
            float tilt = 0f;
            ApplyTell(tell, tellProgress, ref size, ref lift, ref tilt, ref color);

            _mark.color = color;
            _mark.rectTransform.localScale = Vector3.one * size;
            _mark.rectTransform.anchoredPosition = new Vector2(0f, lift);
            _mark.rectTransform.localEulerAngles = new Vector3(0f, 0f, tilt);
        }

        private static bool IsStandDown(TellKind kind) => kind is TellKind.AllClear or TellKind.Settle or TellKind.Fade;

        // Seconds a tell's motion lasts on the mark.
        private static float TellLength(TellKind kind) => kind switch
        {
            TellKind.Scan     => 0.6f,
            TellKind.Sniff    => 0.8f,
            TellKind.Stare    => 1.2f,
            TellKind.LockOn   => 0.5f,
            TellKind.Roar     => 0.9f,
            TellKind.AllClear => 1f,
            TellKind.Settle   => 1.4f,
            TellKind.Fade     => 1.8f,
            TellKind.StepUp   => 0.9f,
            _                 => 0f,
        };

        // How each faction's sign moves the mark (progress runs 0 to 1). TECH is exact: a quick
        // scanner wobble, a crisp punch as it locks on, a brief all-clear. BIO is animal: nose
        // bobs, then a pumped, shaking roar. VOID is slow and wrong: a swelling stare that
        // flickers, a fade that will not finish.
        private static void ApplyTell(TellKind kind, float t, ref float size, ref float lift, ref float tilt, ref Color color)
        {
            switch (kind)
            {
                case TellKind.Scan:
                    tilt = Mathf.Sin(t * Mathf.PI * 4f) * 14f;
                    break;
                case TellKind.LockOn:
                    size *= Mathf.Lerp(1.9f, 1f, Mathf.Clamp01(t * 3f));
                    break;
                case TellKind.Sniff:
                    lift = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * 8f;
                    break;
                case TellKind.Roar:
                    size *= Mathf.Lerp(2.3f, 1.2f, t);
                    tilt = Random.Range(-8f, 8f);
                    break;
                case TellKind.Stare:
                    size *= Mathf.Lerp(0.5f, 1.25f, t * t);
                    color.a *= 0.75f + 0.25f * Mathf.Sin(t * 40f);
                    break;
                case TellKind.StepUp:
                    // Rising to the occasion: the mark climbs and swells twice.
                    size *= 1f + 0.5f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f));
                    lift  = t * 14f;
                    break;
                case TellKind.AllClear:
                case TellKind.Settle:
                case TellKind.Fade:
                    size  *= 0.8f;
                    color.a *= 1f - t * (kind == TellKind.Fade ? 0.85f : 1f);
                    break;
            }
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
