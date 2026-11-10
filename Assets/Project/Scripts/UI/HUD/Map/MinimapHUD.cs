using UnityEngine;
using UnityEngine.UI;
using CGD.WorldMap;

namespace CGD.UI
{
    // Round minimap in the top-right corner: the area around the player with fog of war
    // and markers. Rotates with the player's view by default (player arrow always points
    // up); objective markers flagged ClampToEdge stay on the rim when out of range.
    [RequireComponent(typeof(RectTransform))]
    public class MinimapHUD : HUDElement
    {
        [SerializeField] private WorldMapArea _area;
        [Tooltip("Centre of the map and source of its rotation — usually the Player's camera")]
        [SerializeField] private Transform _viewer;

        [Header("View")]
        [Tooltip("World metres from the centre to the edge")]
        [SerializeField, Min(5f)] private float _range = 40f;
        [SerializeField] private bool _rotateWithViewer = true;

        [Header("Layout")]
        [SerializeField] private Vector2 _screenPadding = new(20f, 60f);
        [SerializeField, Min(50f)] private float _diameter = 200f;
        [SerializeField] private Color _borderColor = new(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color _playerColor = Color.white;

        private RectTransform _rotator;
        private RawImage      _background;
        private RawImage      _fog;
        private RectTransform _playerArrow;
        private MapMarkerLayer _markers;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            UIFactory.AnchorToCorner(self, new Vector2(1f, 1f), _screenPadding);
            self.sizeDelta = Vector2.one * _diameter;

            Image border = UIFactory.MakeImage("Border", self);
            border.sprite = ProceduralSprites.Circle;
            border.color  = _borderColor;
            UIFactory.Stretch(border.rectTransform);
            border.rectTransform.offsetMin = Vector2.one * -3f;
            border.rectTransform.offsetMax = Vector2.one * 3f;

            // Circular mask; everything that rotates sits inside it.
            Image mask = UIFactory.MakeImage("Mask", self);
            mask.sprite = ProceduralSprites.Circle;
            UIFactory.Stretch(mask.rectTransform);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            _rotator = new GameObject("Rotator", typeof(RectTransform)).GetComponent<RectTransform>();
            _rotator.SetParent(mask.rectTransform, false);
            UIFactory.Stretch(_rotator);

            _background = UIFactory.MakeRawImage("Background", _rotator);
            _fog        = UIFactory.MakeRawImage("Fog", _rotator);
            _markers    = new MapMarkerLayer(_rotator);

            Image arrow = UIFactory.MakeImage("Player", self);
            arrow.sprite = ProceduralSprites.Arrow;
            arrow.color  = _playerColor;
            _playerArrow = arrow.rectTransform;
            _playerArrow.anchorMin = _playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
            _playerArrow.sizeDelta = Vector2.one * 14f;
        }

        public override void Refresh() { }

        private void LateUpdate()
        {
            if (_area == null || _viewer == null) return;

            BindTextures();

            Vector3 center  = _viewer.position;
            float   heading = MapProjection.Heading(_viewer);
            float   radius  = _diameter * 0.5f;

            // Show a (2 × range)-metre square of the map around the viewer.
            Vector2 n    = _area.Projection.ToNormalized(center);
            Vector2 span = new(2f * _range / _area.Size.x, 2f * _range / _area.Size.y);
            var uv = new Rect(n - span * 0.5f, span);
            _background.uvRect = uv;
            _fog.uvRect        = uv;

            _rotator.localRotation     = Quaternion.Euler(0f, 0f, _rotateWithViewer ? heading : 0f);
            _playerArrow.localRotation = Quaternion.Euler(0f, 0f, _rotateWithViewer ? 0f : -heading);

            PlaceMarkers(center, radius, heading);
        }

        private void PlaceMarkers(Vector3 center, float radius, float heading)
        {
            float pixelsPerMetre = radius / _range;
            var markers = MapMarker.Active;

            _markers.Begin();
            for (int i = 0; i < markers.Count; i++)
            {
                MapMarker marker = markers[i];
                if (!marker.ShowOnMinimap || !marker.IsVisibleOn(_area)) continue;

                Vector3 delta = marker.Position - center;
                Vector2 local = new Vector2(delta.x, delta.z) * pixelsPerMetre;

                if (local.sqrMagnitude > radius * radius)
                {
                    if (!marker.ClampToEdge) continue;
                    local = local.normalized * (radius - marker.Size * 0.5f);
                }

                // Icons live in the rotating layer, so undo its spin for arrows to point true.
                float rotation = marker.Shape == MapMarkerShape.Arrow ? -marker.Heading : 0f;
                _markers.Place(marker, local, rotation);
            }
            _markers.End();
        }

        // The background may be built in WorldMapArea.Start, after our Awake.
        private void BindTextures()
        {
            if (_background.texture != _area.Background)
            {
                _background.texture = _area.Background;
                _background.color   = _area.Background != null ? Color.white : _area.BackgroundColor;
            }

            if (_fog.texture != _area.FogTexture)
            {
                _fog.texture = _area.FogTexture;
                _fog.enabled = _area.FogTexture != null;
            }
        }
    }
}
