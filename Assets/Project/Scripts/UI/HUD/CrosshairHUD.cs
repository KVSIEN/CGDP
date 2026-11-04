using UnityEngine;
using UnityEngine.UI;
using CGD.Player;
using CGD.Settings;

namespace CGD.UI
{
    // The crosshair on the HUD, drawn from the player's style in GameSettings. Spread comes
    // from the weapon every frame; the obstruction dot marks where an obstructed aim lands.
    [RequireComponent(typeof(RectTransform))]
    public class CrosshairHUD : HUDElement
    {
        [SerializeField] private PlayerCamera _playerCamera;

        [Header("Spread")]
        [Tooltip("Scale multiplier on top of the FOV-derived px/deg conversion. 1 = physically accurate.")]
        [SerializeField] private float _spreadScale = 1f;

        private const float ObstructionDotAlpha = 0.9f;

        [Header("Obstruction Dot")]
        [SerializeField] private float _obstructionDotSize = 6f;

        private CrosshairView _view;
        private Image _obstructionDot;

        private void Awake()
        {
            var rt = (RectTransform)transform;
            _view = new CrosshairView(rt);

            _obstructionDot = UIFactory.MakeImage("ObstructionDot", rt);
            _obstructionDot.rectTransform.anchorMin = _obstructionDot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _obstructionDot.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _obstructionDot.color = Color.clear;

            Refresh();
        }

        private void OnEnable()
        {
            GameSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => GameSettings.Changed -= Refresh;

        private void Update()
        {
            if (_playerCamera == null || _obstructionDot == null) return;

            if (_playerCamera.IsAimObstructed)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    _playerCamera.Camera, _playerCamera.ObstructionPoint);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform, screenPoint, null, out Vector2 localPos);
                _obstructionDot.rectTransform.anchoredPosition = localPos;
                _obstructionDot.rectTransform.sizeDelta = Vector2.one * _obstructionDotSize;
                _obstructionDot.color = Theme.Danger.WithAlpha(ObstructionDotAlpha);
            }
            else
            {
                _obstructionDot.color = Color.clear;
            }
        }

        /// <summary>
        /// Called by WeaponController every frame with the current spread in degrees.
        /// Pass 0 when no weapon is equipped.
        /// </summary>
        public void SetDynamicSpread(float spreadDegrees)
        {
            // Derive px/deg from the live camera FOV so the crosshair scales correctly
            // at all FOVs (hip, ADS, sprint) instead of using a hardcoded constant.
            if (_playerCamera == null || _playerCamera.Camera == null || _view == null) return;
            float canvasHeight = ((RectTransform)transform.root).rect.height;
            float pixPerDeg    = canvasHeight / _playerCamera.Camera.fieldOfView;
            float response     = GameSettings.Current.Crosshair.SpreadResponse;
            _view.SetSpread(spreadDegrees * pixPerDeg * _spreadScale * response);
        }

        public override void Refresh()
        {
            if (_view != null) _view.Apply(GameSettings.Current.Crosshair);
        }
    }
}
