using UnityEngine;

namespace CGD.Level
{
    // A lit-up piece of a generated level that is only seen, never collided with: door signs,
    // the room overview's outlines, floor stripes. One GameObject per colour, its material a copy
    // of `template` in that colour, glowing (emission) at `gain` times it.
    public static class GlowingPart
    {
        public const float DefaultGain = 2f;

        public static GameObject Create(string name, BoxMeshBuilder builder, Material template, Color color, string label,
                                        GeneratedAssets assets, Transform parent, float gain = DefaultGain)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = assets.Track(builder.ToMesh(name));

            var material = assets.Track(new Material(template) { name = $"{template.name} ({label})", color = color });
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * gain);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
