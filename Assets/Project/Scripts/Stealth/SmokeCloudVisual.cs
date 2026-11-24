using UnityEngine;
using CGD.Core;

namespace CGD.Stealth
{
    // What a smoke cloud looks like until real VFX exist: a clump of translucent grey puffs
    // filling its sphere, billowing out from nothing when it appears, churning slowly while it
    // lasts and thinning away over its last seconds (it times its fade from its PooledLifetime).
    // The player's smoke grenade uses the SmokeCloud prefab; the enemies' concealment abilities
    // (Tech's Smoke Screen, Smoke Cover and Smoke Mortar, Bio's Plague Cloud and Spore Burst) use
    // SmokeSphere, which sets Single Sphere: one round, still sphere exactly filling the cloud's
    // radius instead of puffs. The puffs are built at runtime
    // as children, without colliders, on the cloud's own layer. Needs a transparent material.
    [RequireComponent(typeof(SphereCollider))]
    public class SmokeCloudVisual : MonoBehaviour
    {
        private const float BillowTime = 0.6f;
        private const float FadeTime   = 2.5f;
        private const float ChurnSpeed = 0.35f;   // radians per second a puff drifts round its spot
        private const float ChurnReach = 0.12f;   // share of the cloud's radius a puff drifts
        private const float FlatHeight = 0.7f;    // puffs are squashed a little: smoke hugs the ground

        [Tooltip("Transparent material whose base colour is tinted per puff (Materials/VFX/ExplosionFlash)")]
        [SerializeField] private Material _material;
        [Tooltip("Colour of the smoke; alpha is how thick it looks")]
        [SerializeField] private Color _color = new(0.72f, 0.72f, 0.7f, 0.55f);
        [Tooltip("How many puffs make up the cloud")]
        [SerializeField, Range(3, 24)] private int _puffs = 12;
        [Tooltip("One round, still sphere exactly filling the cloud instead of churning puffs")]
        [SerializeField] private bool _singleSphere;

        private Transform[] _puffTransforms;
        private Renderer[]  _puffRenderers;
        private Vector3[]   _homes;
        private float[]     _sizes;
        private float[]     _phases;
        private float       _radius;
        private Vector3     _centre;
        private float       _lifetime;
        private float       _age;

        private void Awake()
        {
            var sphere = GetComponent<SphereCollider>();
            _radius   = sphere.radius;
            _centre   = sphere.center;
            _lifetime = TryGetComponent(out PooledLifetime life) ? life.Lifetime : 0f;
            BuildPuffs();
        }

        // Pooled: every spawn is a fresh cloud, laid out anew.
        private void OnEnable()
        {
            _age = 0f;
            if (_homes != null) Scatter();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float grow  = Mathf.Clamp01(_age / BillowTime);
            grow = grow * (2f - grow);   // eases out: a burst, then settling
            float alpha = _color.a * Mathf.Clamp01(_age / BillowTime);
            if (_lifetime > 0f) alpha *= Mathf.Clamp01((_lifetime - _age) / FadeTime);

            Color tint = _color;
            tint.a = alpha;
            for (int i = 0; i < _puffTransforms.Length; i++)
            {
                float phase = _phases[i] + _age * ChurnSpeed;
                Vector3 drift = _singleSphere ? Vector3.zero : new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 1.3f) * 0.5f, Mathf.Cos(phase)) * (_radius * ChurnReach);
                Transform puff = _puffTransforms[i];
                puff.localPosition = _centre + (_homes[i] + drift) * grow;
                float size = _sizes[i] * grow;
                puff.localScale = new Vector3(size, _singleSphere ? size : size * FlatHeight, size);
                _puffRenderers[i].SetTint(tint);
            }
        }

        private void BuildPuffs()
        {
            int count = _singleSphere ? 1 : _puffs;
            _puffTransforms = new Transform[count];
            _puffRenderers  = new Renderer[count];
            _homes  = new Vector3[count];
            _sizes  = new float[count];
            _phases = new float[count];
            for (int i = 0; i < count; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(puff.GetComponent<Collider>());
                puff.name  = "Puff";
                puff.layer = gameObject.layer;
                puff.transform.SetParent(transform, false);

                var renderer = puff.GetComponent<Renderer>();
                renderer.sharedMaterial    = _material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows    = false;
                if (_material == null) renderer.enabled = false;

                _puffTransforms[i] = puff.transform;
                _puffRenderers[i]  = renderer;
            }
            Scatter();
        }

        // One big puff in the middle, the rest spread through the sphere (lower ones wider), so the
        // cloud reads as filling its whole radius without leaving a gap you could see through.
        private void Scatter()
        {
            for (int i = 0; i < _homes.Length; i++)
            {
                if (i == 0)
                {
                    _homes[i] = Vector3.zero;
                    _sizes[i] = _radius * (_singleSphere ? 2f : 1.4f);
                }
                else
                {
                    Vector3 spot = Random.insideUnitSphere * (_radius * 0.6f);
                    spot.y *= 0.5f;
                    _homes[i] = spot;
                    _sizes[i] = _radius * Random.Range(0.7f, 1.1f) * (spot.y < 0f ? 1.15f : 0.9f);
                }
                _phases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }
    }
}
