using UnityEngine;
using CGD.Core;

namespace CGD.Weapons
{
    // A quick fireball: a sphere that swells to the blast radius while fading out, then returns
    // to the pool. Needs a transparent material; the colour fades through the property block.
    [RequireComponent(typeof(Renderer))]
    public class ExplosionFlash : MonoBehaviour, IPoolable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Min(0.05f)] private float _duration = 0.35f;
        [Tooltip("Share of the blast radius the flash starts at")]
        [SerializeField, Range(0f, 1f)] private float _startScale = 0.3f;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Color _color;
        private float _radius;
        private float _age;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block    = new MaterialPropertyBlock();
        }

        // A flash from the pool at `position`: what every blast, cast and impact effect uses.
        // Does nothing without a prefab, so an optional effect field can simply be left empty.
        public static void Spawn(GameObject prefab, Vector3 position, float radius, Color color)
        {
            if (prefab == null) return;

            GameObject flash = PrefabPool.Spawn(prefab, position, Quaternion.identity);
            if (flash.TryGetComponent(out ExplosionFlash burst)) burst.Play(radius, color);
        }

        // The blast's radius; the mesh is a unit sphere, so its scale is the diameter.
        public void Play(float radius, Color color)
        {
            _radius = radius;
            _color  = color;
            _age    = 0f;
            Apply(0f);
            PrefabPool.Release(gameObject, _duration);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            Apply(Mathf.Clamp01(_age / _duration));
        }

        private void Apply(float progress)
        {
            // Fast out, slow finish.
            float eased = 1f - (1f - progress) * (1f - progress);
            transform.localScale = Vector3.one * (_radius * 2f * Mathf.Lerp(_startScale, 1f, eased));

            Color color = _color;
            color.a *= 1f - progress;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
