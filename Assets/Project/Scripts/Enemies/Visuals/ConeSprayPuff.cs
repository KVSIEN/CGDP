using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // One stream of a cone spray (a flamethrower's flame): a capsule that shoots from the
    // muzzle out to the cone's reach, widening as it goes and fading out, then returns to the
    // pool. Several side by side fill the cone, so together they show where it hits. Needs a
    // transparent material; the colour fades through its tint.
    [RequireComponent(typeof(Renderer))]
    public class ConeSprayPuff : MonoBehaviour, IPoolable
    {
        // Metres: the stream's width at the muzzle, and its shortest and longest length.
        private const float MuzzleWidth = 0.2f;
        private const float MinLength   = 0.3f;
        [Tooltip("Share of the reach the stream's length grows to")]
        [SerializeField, Range(0.1f, 1f)] private float _lengthShare = 0.35f;
        [SerializeField, Min(0.05f)] private float _duration = 0.3f;

        private Renderer _renderer;
        private Vector3  _origin;
        private Vector3  _direction;
        private float    _reach;
        private float    _endWidth;
        private Color    _color;
        private float    _age;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        // A stream from the pool along `direction`, reaching `reach` metres and `endWidth` wide there.
        public static void Spawn(GameObject prefab, Vector3 origin, Vector3 direction, float reach, float endWidth, Color color)
        {
            if (prefab == null) return;

            GameObject spawned = PrefabPool.Spawn(prefab, origin, Quaternion.FromToRotation(Vector3.up, direction));
            if (spawned.TryGetComponent(out ConeSprayPuff puff)) puff.Play(origin, direction, reach, endWidth, color);
        }

        private void Play(Vector3 origin, Vector3 direction, float reach, float endWidth, Color color)
        {
            _origin    = origin;
            _direction = direction.normalized;
            _reach     = reach;
            _endWidth  = endWidth;
            _color     = color;
            _age       = 0f;
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
            // Fast out, slow finish, like a jet losing pressure.
            float eased  = 1f - (1f - progress) * (1f - progress);
            float tip    = _reach * eased;
            float length = Mathf.Clamp(tip, MinLength, _reach * _lengthShare);
            float width  = Mathf.Lerp(MuzzleWidth, _endWidth, tip / _reach);

            // The built-in capsule is 2 m tall along its up axis and 1 m across.
            transform.position   = _origin + _direction * (tip - length * 0.5f);
            transform.localScale = new Vector3(width, length * 0.5f, width);

            Color color = _color;
            color.a *= 1f - progress * progress;
            _renderer.SetTint(color);
        }
    }
}
