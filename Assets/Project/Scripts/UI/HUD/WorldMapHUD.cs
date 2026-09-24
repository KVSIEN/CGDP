using UnityEngine;
using UnityEngine.UI;
using CGD.Input;
using CGD.Timing;
using CGD.WorldMap;

namespace CGD.UI
{
    // Full-screen map of the whole area, opened with the Map action (M): background, fog
    // of war, every world-map marker and the player's position and heading. Locks player
    // input while open and, optionally, pauses game time.
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class WorldMapHUD : HUDElement
    {
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private WorldMapArea       _area;
        [Tooltip("Shown as the player arrow — usually the Player's camera")]
        [SerializeField] private Transform          _viewer;
        [SerializeField] private bool _pauseWhileOpen = true;

        [Header("Layout")]
        [Tooltip("Share of the screen the map may fill")]
        [SerializeField, Range(0.3f, 1f)] private float _screenFill = 0.85f;
        [SerializeField] private Color _dimColor    = new(0f, 0f, 0f, 0.8f);
        [SerializeField] private Color _playerColor = Color.white;
        [SerializeField, Min(0.5f)] private float _markerScale = 1.4f;

        private CanvasGroup   _group;
        private RectTransform _mapRect;
        private RawImage      _background;
        private RawImage      _fog;
        private RectTransform _playerArrow;
        private MapMarkerLayer _markers;

        public override bool ShowWithHud => false;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            var self = GetComponent<RectTransform>();
            UIFactory.Stretch(self);

            Image dim = UIFactory.MakeImage("Dim", self);
            dim.color = _dimColor;
            UIFactory.Stretch(dim.rectTransform);

            _mapRect = new GameObject("Map", typeof(RectTransform)).GetComponent<RectTransform>();
            _mapRect.SetParent(self, false);
            _mapRect.anchorMin = _mapRect.anchorMax = _mapRect.pivot = new Vector2(0.5f, 0.5f);

            _background = UIFactory.MakeRawImage("Background", _mapRect);
            _fog        = UIFactory.MakeRawImage("Fog", _mapRect);
            _markers    = new MapMarkerLayer(_mapRect);

            Image arrow = UIFactory.MakeImage("Player", _mapRect);
            arrow.sprite = ProceduralSprites.Arrow;
            arrow.color  = _playerColor;
            _playerArrow = arrow.rectTransform;
            _playerArrow.anchorMin = _playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
            _playerArrow.sizeDelta = Vector2.one * 18f;

            SetOpen(false);
        }

        private void OnDisable()
        {
            if (IsVisible) SetOpen(false);
        }

        private void Update()
        {
            // Raw so the key still closes the map while it has input locked.
            if (_input != null && _input.WasPressedRaw(GameAction.Map)) Toggle();
            if (IsVisible) Redraw();
        }

        public override void Show() => SetOpen(true);
        public override void Hide() => SetOpen(false);
        public override void Toggle() => SetOpen(!IsVisible);
        public override void Refresh() { if (IsVisible) Redraw(); }

        private void SetOpen(bool open)
        {
            if (open && _area == null) return;

            IsVisible = open;
            _group.alpha          = open ? 1f : 0f;
            _group.blocksRaycasts = open;
            if (_input != null) _input.InputEnabled = !open;

            if (_pauseWhileOpen)
            {
                if (open)                   GameTime.Instance.Clock.Pause(this);
                else if (GameTime.Exists)   GameTime.Instance.Clock.Resume(this);
            }

            if (open) Redraw();
        }

        private void Redraw()
        {
            FitToScreen();

            _background.texture = _area.Background;
            _background.color   = _area.Background != null ? Color.white : _area.BackgroundColor;
            _fog.texture        = _area.FogTexture;
            _fog.enabled        = _area.FogTexture != null;

            Vector2 size = _mapRect.rect.size;

            if (_viewer != null)
            {
                _playerArrow.anchoredPosition = ToLocal(_viewer.position, size);
                _playerArrow.localRotation    = Quaternion.Euler(0f, 0f, -MapProjection.Heading(_viewer));
            }

            var markers = MapMarker.Active;
            _markers.Begin();
            for (int i = 0; i < markers.Count; i++)
            {
                MapMarker marker = markers[i];
                if (!marker.ShowOnWorldMap || !_area.Projection.Contains(marker.Position) || !marker.IsVisibleOn(_area)) continue;

                float rotation = marker.Shape == MapMarkerShape.Arrow ? -marker.Heading : 0f;
                _markers.Place(marker, ToLocal(marker.Position, size), rotation, _markerScale);
            }
            _markers.End();
            _playerArrow.SetAsLastSibling();
        }

        // Largest rect with the area's aspect that fits the screen share.
        private void FitToScreen()
        {
            Vector2 available = ((RectTransform)transform).rect.size * _screenFill;
            float aspect = _area.Size.x / _area.Size.y;
            _mapRect.sizeDelta = available.x / available.y > aspect
                ? new Vector2(available.y * aspect, available.y)
                : new Vector2(available.x, available.x / aspect);
        }

        private Vector2 ToLocal(Vector3 world, Vector2 size) =>
            (_area.Projection.ToNormalized(world) - new Vector2(0.5f, 0.5f)) * size;
    }
}
