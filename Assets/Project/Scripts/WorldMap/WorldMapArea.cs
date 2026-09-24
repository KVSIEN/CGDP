using UnityEngine;
using CGD.Map;

namespace CGD.WorldMap
{
    // Defines the mapped part of the level and owns what maps draw: the background
    // picture and the fog of war. Place one per scene, centred on the playable area;
    // the minimap and world map read from it and MapRevealer uncovers it.
    [DefaultExecutionOrder(-50)]
    public class WorldMapArea : MonoBehaviour
    {
        [Tooltip("World size of the mapped area on X and Z, centred on this transform")]
        [SerializeField] private Vector2 _size = new(200f, 200f);

        [Header("Background")]
        [SerializeField] private MapBackgroundSource _source = MapBackgroundSource.Capture;
        [SerializeField] private Color _backgroundColor = new(0.08f, 0.09f, 0.1f);
        [Tooltip("Texture source: a top-down image covering exactly the area (+Z up)")]
        [SerializeField] private Texture _texture;
        [Tooltip("MapGraph source: the graph to draw")]
        [SerializeField] private MapGraphAsset _graph;
        [Tooltip("Capture source: layers included in the top-down render")]
        [SerializeField] private LayerMask _captureMask = ~0;
        [SerializeField, Min(64)] private int _resolution = 1024;
        [Tooltip("Capture source: how far above the area the capture camera sits")]
        [SerializeField, Min(1f)] private float _captureHeight = 150f;

        [Header("Fog of War")]
        [SerializeField] private bool _fogOfWar = true;
        [Tooltip("World metres per fog cell")]
        [SerializeField, Min(0.25f)] private float _fogCellSize = 2f;
        [SerializeField] private Color _fogColor = new(0.02f, 0.02f, 0.03f, 1f);

        private Texture2D _fogTexture;

        public MapProjection Projection { get; private set; }
        public Texture       Background { get; private set; }
        public FogOfWar      Fog        { get; private set; }
        // Null when fog of war is off.
        public Texture       FogTexture => _fogTexture;
        public Color         BackgroundColor => _backgroundColor;
        public Vector2       Size       => _size;

        private void Awake()
        {
            Projection = new MapProjection(transform.position, _size);
            if (_fogOfWar) CreateFog();
        }

        // Scene geometry is only guaranteed to be in place once every Awake has run.
        private void Start() => Background = BuildBackground();

        // Reveals are batched: the texture is uploaded at most once per frame.
        private void LateUpdate()
        {
            if (Fog == null || !Fog.IsDirty) return;

            _fogTexture.SetPixels32(Fog.Pixels);
            _fogTexture.Apply(false);
            Fog.ClearDirty();
        }

        private void OnDestroy()
        {
            if (_fogTexture != null) Destroy(_fogTexture);
            if (Background is RenderTexture rt) rt.Release();
            if (Background != null && Background != _texture) Destroy(Background);
        }

        public bool IsExplored(Vector3 world) => Fog == null || Fog.IsExplored(Projection.ToNormalized(world));

        public void Reveal(Vector3 world, float radius)
        {
            if (Fog == null) return;
            Fog.Reveal(Projection.ToNormalized(world), new Vector2(radius / _size.x, radius / _size.y));
        }

        private void CreateFog()
        {
            int width  = Mathf.Max(1, Mathf.CeilToInt(_size.x / _fogCellSize));
            int height = Mathf.Max(1, Mathf.CeilToInt(_size.y / _fogCellSize));
            Fog = new FogOfWar(width, height, _fogColor);

            _fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name       = "FogOfWar",
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
            };
            _fogTexture.SetPixels32(Fog.Pixels);
            _fogTexture.Apply(false);
        }

        private Texture BuildBackground() => _source switch
        {
            MapBackgroundSource.Texture  => _texture,
            MapBackgroundSource.MapGraph => RenderGraph(),
            MapBackgroundSource.Capture  => Capture(),
            _                            => null,
        };

        private Texture RenderGraph()
        {
            if (_graph == null) return null;

            (int width, int height) = PixelSize();
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "MapGraph", wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(MapGraphRasterizer.Render(_graph.Graph, width, height, _backgroundColor));
            texture.Apply(false);
            return texture;
        }

        // One orthographic render straight down, kept as a RenderTexture (no readback).
        // +Z ends up at the top of the image, matching MapProjection.
        private Texture Capture()
        {
            (int width, int height) = PixelSize();
            var target = new RenderTexture(width, height, 24) { name = "MapCapture", wrapMode = TextureWrapMode.Clamp };

            var go = new GameObject("MapCaptureCamera");
            var camera = go.AddComponent<Camera>();
            camera.enabled          = false;
            camera.orthographic     = true;
            camera.orthographicSize = _size.y * 0.5f;
            camera.aspect           = _size.x / _size.y;
            camera.cullingMask      = _captureMask;
            camera.clearFlags       = CameraClearFlags.SolidColor;
            camera.backgroundColor  = _backgroundColor;
            camera.nearClipPlane    = 0.3f;
            camera.farClipPlane     = _captureHeight * 2f;
            camera.targetTexture    = target;
            go.transform.SetPositionAndRotation(transform.position + Vector3.up * _captureHeight, Quaternion.Euler(90f, 0f, 0f));

            camera.Render();
            camera.targetTexture = null;
            Destroy(go);
            return target;
        }

        // Longest side gets the full resolution; the other keeps the area's aspect.
        private (int width, int height) PixelSize()
        {
            float aspect = _size.x / _size.y;
            return aspect >= 1f
                ? (_resolution, Mathf.Max(1, Mathf.RoundToInt(_resolution / aspect)))
                : (Mathf.Max(1, Mathf.RoundToInt(_resolution * aspect)), _resolution);
        }
    }
}
