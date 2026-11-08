using UnityEngine;
using UnityEngine.Rendering;
using CGD.Abilities;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // Placeholder effects for what an enemy's abilities do, until real VFX exist: a flash as
    // an ability goes off and a pulsing shell (plus a ring at the feet or a trail) for as long
    // as its buff lasts (AbilityLook per ability); a burst where it vanished and where it
    // reappears after a Phase Shift; and a small pulse each time it heals off a hit
    // (lifesteal). Built at runtime from one transparent material, so prefabs only assign it.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyAbilityVisuals : MonoBehaviour
    {
        private const float ChestHeight   = 1.1f;
        private const float HealPulseGap  = 0.3f;   // seconds between heal pulses, however often it heals
        private const float TrailTime     = 0.3f;
        private const float TrailWidth    = 0.7f;

        [Tooltip("Transparent unlit material, tinted per effect (e.g. ExplosionFlash)")]
        [SerializeField] private Material _material;
        [Tooltip("Mesh of the shell round the body (the built-in Capsule)")]
        [SerializeField] private Mesh _shellMesh;
        [Tooltip("Mesh of the ring at the feet (the built-in Cylinder, flattened)")]
        [SerializeField] private Mesh _ringMesh;
        [Tooltip("What the shell and ring follow — the model, so they topple and pop in with it")]
        [SerializeField] private Transform _model;
        [Tooltip("Pooled prefab with an ExplosionFlash (Prefabs/VFX/ExplosionFlash)")]
        [SerializeField] private GameObject _flash;
        [SerializeField] private AbilityLook[] _looks = System.Array.Empty<AbilityLook>();

        [Header("Phase Shift")]
        [SerializeField] private Color _relocateColor = new(0.7f, 0.4f, 1f, 0.8f);
        [Tooltip("Metres: the bursts where it vanished and reappeared. 0 = none")]
        [SerializeField, Min(0f)] private float _relocateFlash = 1.4f;

        [Header("Heals (lifesteal)")]
        [SerializeField] private Color _healColor = new(0.3f, 1f, 0.4f, 0.6f);
        [Tooltip("Metres: the pulse when it heals. 0 = none")]
        [SerializeField, Min(0f)] private float _healFlash = 0.7f;

        private EnemyAI     _ai;
        private EnemyHealth _health;
        private BuffAura    _aura;
        private float       _nextHealPulse;

        private void Awake()
        {
            _ai     = GetComponent<EnemyAI>();
            _health = GetComponent<EnemyHealth>();

            Transform parent = _model != null ? _model : transform;
            Renderer shell = CreatePart("Ability Shell", _shellMesh, parent, Vector3.up, new Vector3(1.2f, 1.1f, 1.2f));
            Renderer ring  = NeedsAny(look => look.GroundRing)
                ? CreatePart("Ability Ring", _ringMesh, parent, Vector3.up * 0.03f, new Vector3(2.4f, 0.01f, 2.4f))
                : null;
            TrailRenderer trail = NeedsAny(look => look.Trail) ? CreateTrail() : null;
            _aura = new BuffAura(shell, ring, trail);
        }

        private void OnEnable()
        {
            _ai.AbilityUsed   += OnAbilityUsed;
            _ai.Relocated     += OnRelocated;
            _health.OnHealed  += OnHealed;
            _health.OnDeath   += _aura.Hide;
            _health.OnRevived += _aura.Hide;
        }

        private void OnDisable()
        {
            _ai.AbilityUsed   -= OnAbilityUsed;
            _ai.Relocated     -= OnRelocated;
            _health.OnHealed  -= OnHealed;
            _health.OnDeath   -= _aura.Hide;
            _health.OnRevived -= _aura.Hide;
        }

        private void Update() => _aura.Tick(Time.time);

        private void OnAbilityUsed(Ability ability)
        {
            if (!TryGetLook(ability, out AbilityLook look)) return;

            if (look.CastFlash > 0f) Flash(Chest(transform.position), look.CastFlash, look.Color);
            if (ability.EffectDuration > 0f) _aura.Show(look.Color, ability.EffectDuration, look.GroundRing, look.Trail, Time.time);
        }

        private void OnRelocated(Vector3 from)
        {
            if (_relocateFlash <= 0f) return;
            Flash(Chest(from), _relocateFlash, _relocateColor);
            Flash(Chest(transform.position), _relocateFlash, _relocateColor);
        }

        private void OnHealed(float amount)
        {
            if (_healFlash <= 0f || Time.time < _nextHealPulse) return;
            _nextHealPulse = Time.time + HealPulseGap;
            Flash(Chest(transform.position), _healFlash, _healColor);
        }

        private void Flash(Vector3 position, float radius, Color color)
        {
            if (_flash == null) return;
            GameObject flash = PrefabPool.Spawn(_flash, position, Quaternion.identity);
            if (flash.TryGetComponent(out ExplosionFlash sphere)) sphere.Play(radius, color);
        }

        private bool TryGetLook(Ability ability, out AbilityLook look)
        {
            foreach (AbilityLook candidate in _looks)
            {
                if (candidate.Ability != ability) continue;
                look = candidate;
                return true;
            }
            look = default;
            return false;
        }

        private bool NeedsAny(System.Predicate<AbilityLook> wants) => System.Array.Exists(_looks, wants);

        private static Vector3 Chest(Vector3 feet) => feet + Vector3.up * ChestHeight;

        // A child with only a mesh and the effect material: no collider, so it never blocks
        // sight, shots or movement.
        private Renderer CreatePart(string partName, Mesh mesh, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale    = localScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial    = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows    = false;
            return renderer;
        }

        private TrailRenderer CreateTrail()
        {
            var part = new GameObject("Ability Trail");
            part.transform.SetParent(transform, false);
            part.transform.localPosition = Vector3.up * ChestHeight;

            var trail = part.AddComponent<TrailRenderer>();
            trail.sharedMaterial    = _material;
            trail.time              = TrailTime;
            trail.widthMultiplier   = TrailWidth;
            trail.widthCurve        = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.minVertexDistance = 0.1f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows    = false;
            trail.emitting          = false;
            return trail;
        }
    }
}
