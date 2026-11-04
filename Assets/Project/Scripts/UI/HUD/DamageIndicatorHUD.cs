using UnityEngine;
using UnityEngine.UI;
using CGD.Combat;
using CGD.Player;
using CGD.Settings;

namespace CGD.UI
{
    // Arrows around the crosshair pointing at whoever hurt the player, turning as the player
    // does, fading after a couple of seconds. One per attacker; a repeat hit from the same one
    // refreshes its arrow. Hits with no attacker (hazards, status ticks) and the player's own
    // (a grenade thrown too close) show nothing. Turn off under Settings > Accessibility.
    [RequireComponent(typeof(RectTransform))]
    public class DamageIndicatorHUD : HUDElement
    {
        // Hits weaker than this still show, at this share of full strength.
        private const float MinIntensity = 0.35f;

        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private PlayerCamera _playerCamera;

        [Header("Look")]
        [Tooltip("Optional art for the arrow; it should point up. Empty = a plain arrowhead")]
        [SerializeField] private Sprite _sprite;
        [Tooltip("Distance from the centre of the screen, in HUD pixels")]
        [SerializeField] private float _radius = 180f;
        [SerializeField] private Vector2 _size = new(36f, 44f);

        [Header("Behavior")]
        [SerializeField, Min(0.2f)] private float _duration = 2f;
        [SerializeField, Range(1, 12)] private int _maxIndicators = 6;
        [Tooltip("A hit of this share of the player's maximum health shows at full strength")]
        [SerializeField, Range(0.05f, 1f)] private float _fullStrengthShare = 0.25f;

        private DamageIndicators _indicators;
        private RectTransform[]  _pivots;
        private Image[]          _arrows;

        private void Awake()
        {
            var self = (RectTransform)transform;
            UIFactory.Stretch(self);

            _indicators = new DamageIndicators(_maxIndicators, _duration);
            _pivots     = new RectTransform[_maxIndicators];
            _arrows     = new Image[_maxIndicators];

            for (int i = 0; i < _maxIndicators; i++)
                BuildArrow(self, i);
        }

        // A pivot at the screen centre that is rotated to the attacker's bearing, with the
        // arrow out at the radius on it.
        private void BuildArrow(RectTransform parent, int index)
        {
            var pivotGo = new GameObject("Pivot_" + index, typeof(RectTransform));
            pivotGo.transform.SetParent(parent, false);
            var pivot = (RectTransform)pivotGo.transform;
            pivot.anchorMin = pivot.anchorMax = pivot.pivot = new Vector2(0.5f, 0.5f);
            pivot.anchoredPosition = Vector2.zero;
            pivot.sizeDelta = Vector2.zero;

            Image arrow = UIFactory.MakeImage("Arrow", pivot);
            arrow.sprite = _sprite != null ? _sprite : ProceduralSprites.Arrow;
            arrow.color = Color.clear;
            RectTransform rt = arrow.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, _radius);
            rt.sizeDelta = _size;

            pivotGo.SetActive(false);
            _pivots[index] = pivot;
            _arrows[index] = arrow;
        }

        private void OnEnable()  => CombatEvents.DamageDealt += OnDamageDealt;
        private void OnDisable() => CombatEvents.DamageDealt -= OnDamageDealt;

        private void OnDamageDealt(DamageReport report)
        {
            if (_playerHealth == null || report.Target != _playerHealth || report.Amount <= 0f) return;
            if (!GameSettings.Current.DamageIndicators) return;

            GameObject attacker = report.Source.Owner;
            if (attacker == null || attacker.transform.IsChildOf(_playerHealth.transform)) return;

            float share = _playerHealth.MaxHealth > 0f ? report.Amount / (_playerHealth.MaxHealth * _fullStrengthShare) : 1f;
            float intensity = Mathf.Lerp(MinIntensity, 1f, Mathf.Clamp01(share));
            _indicators.Hit(attacker, attacker.transform.position, intensity);
        }

        private void Update()
        {
            Camera view = _playerCamera != null ? _playerCamera.Camera : null;
            if (view == null) return;

            _indicators.Tick(Time.deltaTime);

            for (int i = 0; i < _arrows.Length; i++)
            {
                bool active = _indicators.IsActive(i);
                if (_pivots[i].gameObject.activeSelf != active) _pivots[i].gameObject.SetActive(active);
                if (!active) continue;

                // Follow an attacker that is still around; otherwise keep its last position.
                if (_indicators[i].Source is GameObject attacker && attacker != null)
                    _indicators.Retarget(i, attacker.transform.position);

                Transform eye = view.transform;
                if (!DamageDirection.TryAngle(eye.forward, eye.position, _indicators[i].Position, out float degrees))
                {
                    _arrows[i].color = Color.clear;
                    continue;
                }

                // UI rotation runs counter-clockwise; the bearing is clockwise.
                _pivots[i].localRotation = Quaternion.Euler(0f, 0f, -degrees);

                float intensity = _indicators[i].Intensity;
                _arrows[i].color = Theme.Danger.WithAlpha(_indicators.Fade(i) * intensity);
                _arrows[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.1f, intensity);
            }
        }

        public override void Refresh() { }
    }
}
