using UnityEngine;
using CGD.Core;
using CGD.Abilities;
using CGD.Stealth;
using CGD.Targeting;
using CGD.Weapons;

namespace CGD.Enemies
{
    // Placeholder effects for what an enemy's abilities do, until real VFX exist, each as the
    // ability's own Look sets it: a flash as it goes off; its hitbox as it hits (streams fanned
    // across a cone, a burst filling the sphere round it); and a pulsing shell (plus a ring at
    // the feet or a trail) for as long as its buff lasts. Also a burst where it vanished and
    // where it reappears after a Phase Shift, and a small pulse each time it heals off a hit
    // (lifesteal). Shell, ring and trail are built at runtime from one transparent material;
    // flashes and streams come from the pool.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyAbilityVisuals : MonoBehaviour
    {
        private const float ChestHeight     = 1.1f;
        private const float HealPulseGap    = 0.3f;  // seconds between heal pulses, however often it heals
        private const float TrailTime       = 0.3f;
        private const float TrailWidth      = 0.7f;
        private const float MuzzleAhead     = 0.4f;  // metres in front of the eyes the streams start, clear of the head
        private const float StreamSpacing   = 6.5f;  // degrees of cone per stream…
        private const int   MinStreams      = 3;     // …within these bounds
        private const int   MaxStreams      = 12;
        private const float StreamOverlap   = 1.3f;  // streams a little wider than their slice, so the cone has no gaps
        private const float WidestHalfAngle = 80f;   // degrees: wider cones are drawn this wide at their reach

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
        [Tooltip("Pooled prefab with a ConeSprayPuff (Prefabs/VFX/ConeSprayPuff), for cone hitboxes. Empty = cones aren't drawn")]
        [SerializeField] private GameObject _coneStream;

        [Header("Cloak")]
        [Tooltip("How visible the body stays while the agent is cloaked (0 = invisible)")]
        [SerializeField, Range(0.05f, 1f)] private float _cloakedOpacity = 0.25f;

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
        private CloakFade   _cloakFade;
        private Stealthable _stealth;
        private float       _nextHealPulse;

        private void Awake()
        {
            _ai     = GetComponent<EnemyAI>();
            _health = GetComponent<EnemyHealth>();

            TryGetComponent(out _stealth);
            if (_stealth != null) _cloakFade = new CloakFade(GetComponentsInChildren<Renderer>(), _cloakedOpacity);

            // Built up front, hidden: which abilities it ends up with isn't known yet (a tree can
            // name its own), and hidden parts cost nothing.
            Transform parent = _model != null ? _model : transform;
            Renderer shell = CreatePart("Ability Shell", _shellMesh, parent, Vector3.up, new Vector3(1.2f, 1.1f, 1.2f));
            Renderer ring  = CreatePart("Ability Ring", _ringMesh, parent, Vector3.up * 0.03f, new Vector3(2.4f, 0.01f, 2.4f));
            _aura = new BuffAura(shell, ring, CreateTrail());
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

        private void Update()
        {
            _aura.Tick(Time.time);
            _cloakFade?.Tick(_stealth.IsHidden, Time.deltaTime);
        }

        private void OnAbilityUsed(Ability ability)
        {
            AbilityLook look = ability.Look;
            if (!look.IsSet) return;

            if (look.CastFlash > 0f) Flash(Chest(transform.position), look.CastFlash, look.Color);
            if (look.ShowArea) DrawArea(ability.Area, look.Color);
            if (ability.EffectDuration > 0f) _aura.Show(look.Color, ability.EffectDuration, look.GroundRing, look.Trail, Time.time);
        }

        private void DrawArea(in TargetArea area, Color color)
        {
            switch (area.Shape)
            {
                case TargetArea.Kind.AroundCaster: Flash(Chest(transform.position), area.Radius, color); break;
                case TargetArea.Kind.Cone:         SprayCone(area, color); break;
            }
        }

        // Fans streams out evenly across the cone, each down the middle of its own slice, from
        // the aim the cone was just taken from, out to its reach.
        private void SprayCone(in TargetArea cone, Color color)
        {
            if (_coneStream == null) return;

            Transform aim   = _ai.Abilities.Aim;
            Vector3 origin  = aim.position + aim.forward * MuzzleAhead;
            int streams     = Mathf.Clamp(Mathf.RoundToInt(cone.Angle / StreamSpacing), MinStreams, MaxStreams);
            float slice     = cone.Angle / streams;
            float halfAngle = Mathf.Min(cone.Angle * 0.5f, WidestHalfAngle);
            float width     = 2f * cone.Radius * Mathf.Tan(halfAngle * Mathf.Deg2Rad) / streams * StreamOverlap;
            float reach     = Mathf.Max(0.5f, cone.Radius - MuzzleAhead);

            for (int i = 0; i < streams; i++)
            {
                float yaw = -cone.Angle * 0.5f + slice * (i + 0.5f);
                Vector3 direction = Quaternion.AngleAxis(yaw, aim.up) * aim.forward;
                ConeSprayPuff.Spawn(_coneStream, origin, direction, reach, width, color);
            }
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
            ExplosionFlash.Spawn(_flash, position, radius, color);
        }

        private static Vector3 Chest(Vector3 feet) => feet + Vector3.up * ChestHeight;

        private Renderer CreatePart(string partName, Mesh mesh, Transform parent, Vector3 localPosition, Vector3 localScale) =>
            EffectParts.MeshPart(partName, parent, mesh, _material, localPosition, localScale);

        private TrailRenderer CreateTrail() =>
            EffectParts.Trail("Ability Trail", transform, Vector3.up * ChestHeight, _material, TrailTime, TrailWidth, 0.1f);
    }
}
