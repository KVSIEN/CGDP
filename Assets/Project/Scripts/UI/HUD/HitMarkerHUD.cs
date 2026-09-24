using UnityEngine;
using UnityEngine.UI;

namespace CGD.UI
{
    // The X around the crosshair that confirms a hit: white for a hit, orange for a
    // critical, larger and red for a kill. Pops in slightly oversized and fades.
    [RequireComponent(typeof(RectTransform))]
    public class HitMarkerHUD : HUDElement
    {
        public enum Kind { Hit, Critical, Kill }

        [SerializeField] private float _gap       = 8f;
        [SerializeField] private float _length    = 9f;
        [SerializeField] private float _thickness = 2f;
        [SerializeField, Min(0.01f)] private float _duration = 0.25f;

        [Header("Colors")]
        [SerializeField] private Color _hitColor      = Color.white;
        [SerializeField] private Color _criticalColor = new(1f, 0.55f, 0.1f);
        [SerializeField] private Color _killColor     = new(1f, 0.2f, 0.2f);

        private readonly Image[] _lines = new Image[4];
        private RectTransform _root;
        private float _remaining;
        private float _scale = 1f;

        private void Awake()
        {
            _root = GetComponent<RectTransform>();
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = Vector2.zero;

            for (int i = 0; i < _lines.Length; i++)
            {
                // Diagonals: one line per corner, pointing out from the centre.
                float angle = 45f + 90f * i;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;

                Image line = UIFactory.MakeImage($"Line{i}", _root);
                RectTransform rt = line.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(_length, _thickness);
                rt.anchoredPosition = direction * _gap;
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                _lines[i] = line;
            }

            SetAlpha(0f);
        }

        public void Show(Kind kind)
        {
            if (_root == null) return;

            Color color = kind switch
            {
                Kind.Critical => _criticalColor,
                Kind.Kill     => _killColor,
                _             => _hitColor,
            };
            foreach (Image line in _lines) line.color = color;

            _scale     = kind == Kind.Kill ? 1.6f : 1.25f;
            _remaining = kind == Kind.Kill ? _duration * 2f : _duration;
            Tick(0f);
        }

        public override void Refresh() { }

        private void Update()
        {
            if (_remaining > 0f) Tick(Time.unscaledDeltaTime);
        }

        private void Tick(float deltaTime)
        {
            _remaining = Mathf.Max(0f, _remaining - deltaTime);
            float t = Mathf.Clamp01(_remaining / _duration);

            // Settles from the pop size back to 1 as it fades.
            _root.localScale = Vector3.one * Mathf.Lerp(1f, _scale, t * t);
            SetAlpha(t);
        }

        private void SetAlpha(float alpha)
        {
            foreach (Image line in _lines)
            {
                Color c = line.color;
                c.a = alpha;
                line.color = c;
            }
        }
    }
}
