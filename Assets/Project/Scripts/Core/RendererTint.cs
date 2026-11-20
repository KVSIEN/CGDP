using UnityEngine;

namespace CGD.Core
{
    // Colours a renderer without making it its own material copy: sets the URP Lit / Unlit
    // _BaseColor through a property block, keeping anything else the renderer's block holds, so
    // many objects can share one material and still be tinted apart. Every caller uses the one
    // scratch block, which is filled and handed to the renderer within the same call.
    public static class RendererTint
    {
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static MaterialPropertyBlock _scratch;

        public static void SetTint(this Renderer renderer, Color color)
        {
            _scratch ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_scratch);
            _scratch.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(_scratch);
        }

        // Back to the material's own colour (and no other per-renderer overrides).
        public static void ClearTint(this Renderer renderer) => renderer.SetPropertyBlock(null);
    }
}
