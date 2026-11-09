using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // Makes an agent's body see-through while it is cloaked. The body materials are opaque, so
    // while faded the renderers swap to transparent copies (made once, on first need) and go
    // back to the originals when fully visible again, so the normal look costs nothing.
    internal sealed class CloakFade
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SurfaceId   = Shader.PropertyToID("_Surface");
        private static readonly int SrcBlendId  = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId  = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId    = Shader.PropertyToID("_ZWrite");

        private const float FadeSpeed = 4f;   // opacity per second

        private readonly struct Part
        {
            public readonly Renderer   Renderer;
            public readonly Material[] Originals;
            public readonly Material[] Faded;
            public readonly Color[]    Colours;

            public Part(Renderer renderer, Material[] originals, Material[] faded, Color[] colours)
            {
                Renderer = renderer; Originals = originals; Faded = faded; Colours = colours;
            }
        }

        private readonly List<Part> _parts = new();
        private readonly float _cloakedOpacity;
        private float _opacity = 1f;
        private bool  _swapped;

        // `bodies` must exclude effect parts (shells, trails) that should stay as they are.
        public CloakFade(IEnumerable<Renderer> bodies, float cloakedOpacity)
        {
            _cloakedOpacity = cloakedOpacity;
            foreach (Renderer renderer in bodies)
            {
                if (renderer is TrailRenderer) continue;
                Material[] originals = renderer.sharedMaterials;
                var faded   = new Material[originals.Length];
                var colours = new Color[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    faded[i]   = originals[i] != null ? MakeTransparent(originals[i]) : null;
                    colours[i] = originals[i] != null && originals[i].HasProperty(BaseColorId) ? originals[i].GetColor(BaseColorId) : Color.white;
                }
                _parts.Add(new Part(renderer, originals, faded, colours));
            }
        }

        public void Tick(bool cloaked, float deltaTime)
        {
            float target = cloaked ? _cloakedOpacity : 1f;
            if (Mathf.Approximately(_opacity, target) && (_opacity < 1f) == _swapped) return;

            _opacity = Mathf.MoveTowards(_opacity, target, FadeSpeed * deltaTime);
            bool fading = _opacity < 1f;
            if (fading != _swapped) Swap(fading);
            if (fading) Apply();
        }

        private void Swap(bool toFaded)
        {
            _swapped = toFaded;
            foreach (Part part in _parts)
                if (part.Renderer != null) part.Renderer.sharedMaterials = toFaded ? part.Faded : part.Originals;
        }

        private void Apply()
        {
            foreach (Part part in _parts)
                for (int i = 0; i < part.Faded.Length; i++)
                {
                    if (part.Faded[i] == null) continue;
                    Color colour = part.Colours[i];
                    colour.a *= _opacity;
                    part.Faded[i].SetColor(BaseColorId, colour);
                }
        }

        private static Material MakeTransparent(Material source)
        {
            var copy = new Material(source);
            copy.SetFloat(SurfaceId, 1f);
            copy.SetFloat(SrcBlendId, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            copy.SetFloat(DstBlendId, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            copy.SetFloat(ZWriteId, 0f);
            copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            copy.SetOverrideTag("RenderType", "Transparent");
            copy.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return copy;
        }
    }
}
