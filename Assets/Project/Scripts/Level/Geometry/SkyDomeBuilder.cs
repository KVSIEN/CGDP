using UnityEngine;
using Unity.AI.Navigation;

namespace CGD.Level
{
    // Puts a simulated sky over every open-air room (RoomFunction.OpenCeiling: parks, towns):
    // a dome rising from the room's wall height, shaded from a pale horizon to a deeper zenith,
    // as on the habitat decks of a colony ship. It is only to be seen: no collider, no shadows,
    // and out of the NavMesh. One gradient texture and one material copy serve every dome.
    public class SkyDomeBuilder
    {
        private const int Segments = 48, Rings = 10, GradientSteps = 32;

        private readonly LevelBuildSettings _settings;
        private readonly GeneratedAssets _assets;

        public SkyDomeBuilder(LevelBuildSettings settings, GeneratedAssets assets)
        {
            _settings = settings;
            _assets   = assets;
        }

        public void Build(LevelLayout layout, Transform parent)
        {
            Material sky = null;
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (room.Function == null || !room.Function.OpenCeiling) continue;
                sky ??= MakeSkyMaterial();
                BuildDome(layout, room, sky, parent);
            }
        }

        private void BuildDome(LevelLayout layout, LevelRoom room, Material sky, Transform parent)
        {
            RectInt bounds = room.Footprint.Bounds;
            float tile = layout.TileSize;
            var shape = new SkyDomeShape(new Vector2(bounds.width, bounds.height) * tile, _settings.SkyDomeHeight, Segments, Rings);

            var uvs = new Vector2[shape.Vertices.Length];
            for (int i = 0; i < uvs.Length; i++) uvs[i] = new Vector2(0.5f, shape.Elevation[i]);

            var mesh = new Mesh { name = $"SkyDome #{room.Node.Id}", vertices = shape.Vertices, triangles = shape.Triangles, uv = uvs };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _assets.Track(mesh);

            var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = _settings.CeilingLayer;
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            // The rim sits on top of the room's walls, over the middle of its bounds.
            go.transform.localPosition = new Vector3((bounds.xMin + bounds.width * 0.5f) * tile, room.WallHeight, (bounds.yMin + bounds.height * 0.5f) * tile);

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial    = sky;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows    = false;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        // The settings' sky material with a horizon-to-zenith gradient as its texture: the dome's
        // uv.y is its elevation, so the gradient needs no special shader.
        private Material MakeSkyMaterial()
        {
            var gradient = new Texture2D(1, GradientSteps, TextureFormat.RGBA32, false)
            {
                name = "SkyDomeGradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < GradientSteps; y++)
            {
                // Eased, so the haze hugs the horizon and the zenith takes over quickly.
                float t = Mathf.Sqrt((float)y / (GradientSteps - 1));
                gradient.SetPixel(0, y, Color.Lerp(_settings.SkyHorizon, _settings.SkyZenith, t));
            }
            gradient.Apply(false, true);
            _assets.Track(gradient);

            var material = _assets.Track(new Material(_settings.SkyDomeMaterial) { name = "SkyDome (gradient)" });
            material.SetTexture("_BaseMap", gradient);
            material.SetColor("_BaseColor", Color.white);
            return material;
        }
    }
}
