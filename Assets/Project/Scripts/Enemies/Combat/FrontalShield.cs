using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.Combat;

namespace CGD.Enemies
{
    // A shield carried in front (a riot bot, a bulwark): hits from within its arc in front lose
    // most of their damage; from the side or behind they land in full. The answer to it is to
    // flank — so pair it with Advance Facing, which keeps the front to the target. Lightning
    // goes through it (it shorts the shield out) unless Stops Lightning is on.
    // With a Durability it can also be broken by focused fire: the bearer reels for a moment and
    // fights without it until it comes back up, the other answer to a shield wall.
    // With a Barrier Size it also stands a one-way wall of force in front of the bearer
    // (ShieldBarrier): hostile fire from the front stops on it, so allies behind are covered too,
    // while shots fired from behind (the bearer's, its allies') pass through. Blasts, zones, melee
    // and beams from its front don't reach anyone behind it either. Shots the wall stops (and hits
    // on the bearer itself) wear its Durability down. The bearer lowers the shield while it casts any of its Lower For
    // abilities (both hands busy: a repair, a smoke screen) and raises it again once done.
    [DisallowMultipleComponent]
    public class FrontalShield : MonoBehaviour, IDamageInterceptor
    {
        [Tooltip("Degrees either side of straight ahead that the shield covers")]
        [SerializeField, Range(5f, 90f)] private float _halfArc = 60f;
        [Tooltip("Share of a covered hit's damage the shield stops")]
        [SerializeField, Range(0f, 1f)] private float _reduction = 0.8f;
        [SerializeField] private bool _stopsLightning;

        [Header("Breaking")]
        [Tooltip("Damage the shield can stop before it breaks (0 = it never breaks)")]
        [SerializeField, Min(0f)] private float _durability;
        [Tooltip("Seconds it stays down once broken, before it is back up at full strength")]
        [SerializeField, Min(0.5f)] private float _downSeconds = 6f;
        [Tooltip("Seconds the bearer reels (stunned, its attack spoiled) when the shield breaks. 0 = none")]
        [SerializeField, Min(0f)] private float _breakStagger = 1.2f;
        [Tooltip("Optional: the shield's model, hidden while it is down")]
        [SerializeField] private GameObject _visual;

        [Header("Barrier")]
        [Tooltip("Width, height and depth in metres of the one-way wall it stands in front of itself, covering allies behind. " +
                 "Zero = no wall, the shield only protects the bearer")]
        [SerializeField] private Vector3 _barrierSize;
        [Tooltip("Metres in front of the bearer's centre the wall stands")]
        [SerializeField, Min(0.3f)] private float _barrierDistance = 0.9f;
        [Tooltip("Material the wall is drawn with (a transparent one); none = no visible wall")]
        [SerializeField] private Material _barrierMaterial;
        [SerializeField] private Color _barrierColor = new(0.35f, 0.75f, 1f, 0.3f);

        [Header("Lowering")]
        [Tooltip("Abilities it lowers the shield to cast (a repair, a smoke screen): down from the start of the cast until it goes off")]
        [SerializeField] private List<Ability> _lowerFor = new();

        private ShieldDurability _state;
        private HealthManager    _health;
        private EnemyAI          _ai;
        private Stunnable        _stunnable;
        private GameObject       _barrier;

        // Before guards and reflectors: the shield takes the blow first.
        public int Order => -10;

        public bool IsUp => !IsLowered && !IsBroken;

        // Stands a one-way wall in front of it, not only a guard for itself.
        internal bool HasBarrier => _barrierSize.x > 0f && _barrierSize.y > 0f;

        // Broken by focused fire and not back up yet.
        public bool IsBroken => _state != null && !_state.IsUp(Time.time);

        // Put down for a cast that needs both hands.
        public bool IsLowered => _ai != null && _ai.Abilities != null && _ai.Abilities.IsCasting && _lowerFor.Contains(_ai.Abilities.Casting);

        private void Awake()
        {
            _state = new ShieldDurability(_durability, _downSeconds);
            TryGetComponent(out _ai);
            TryGetComponent(out _stunnable);
            if (TryGetComponent(out _health)) _health.OnRevived += Restore;
            if (HasBarrier) BuildBarrier();
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= Restore;
        }

        // Shows the model and wall while the shield is up: back once a broken shield recovers or a
        // lowering cast is done.
        private void Update()
        {
            bool up = IsUp;
            if (_visual  != null && _visual.activeSelf  != up) _visual.SetActive(up);
            if (_barrier != null && _barrier.activeSelf != up) _barrier.SetActive(up);
        }

        // A thin box in front of the bearer, on the Ignore Raycast layer so sight and cover checks
        // (which look at the default layer) see through it, while shots (which hit every layer) stop on it.
        private void BuildBarrier()
        {
            _barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _barrier.name  = "ShieldBarrier";
            _barrier.layer = LayerMask.NameToLayer(ShieldBarrier.Layer);
            Transform wall = _barrier.transform;
            wall.SetParent(transform, false);
            wall.localPosition = new Vector3(0f, _barrierSize.y * 0.5f, _barrierDistance);
            wall.localRotation = Quaternion.identity;
            wall.localScale    = new Vector3(_barrierSize.x, _barrierSize.y, Mathf.Max(0.05f, _barrierSize.z));

            var renderer = _barrier.GetComponent<MeshRenderer>();
            if (_barrierMaterial == null) renderer.enabled = false;
            else
            {
                renderer.sharedMaterial    = _barrierMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var tint = new MaterialPropertyBlock();
                tint.SetColor("_BaseColor", _barrierColor);
                renderer.SetPropertyBlock(tint);
            }

            var barrier = _barrier.AddComponent<ShieldBarrier>();
            barrier.StopEveryHit();
            barrier.Absorbed += OnBarrierHit;
        }

        private void OnBarrierHit(float damage)
        {
            if (_state.Absorb(damage, Time.time)) Break();
        }

        public float Intercept(in DamageInfo info, float amount, Vector3 point)
        {
            if (info.Type == DamageType.Lightning && !_stopsLightning) return amount;
            if (!IsUp || !TryGetHitDirection(info, point, out Vector3 from)) return amount;
            if (!Covers(transform.forward, from, _halfArc)) return amount;

            float stopped = amount * _reduction;
            if (_state.Absorb(stopped, Time.time)) Break();
            return amount - stopped;
        }

        // Whether a hit coming from `from` (a direction from the bearer toward the hit) lands
        // within the shield's arc round `forward`. Flat: height doesn't matter.
        public static bool Covers(Vector3 forward, Vector3 from, float halfArc)
        {
            forward.y = 0f;
            from.y    = 0f;
            if (forward.sqrMagnitude < 0.0001f || from.sqrMagnitude < 0.0001f) return false;
            return Vector3.Angle(forward, from) <= halfArc;
        }

        private void Break()
        {
            if (_visual  != null) _visual.SetActive(false);
            if (_barrier != null) _barrier.SetActive(false);
            if (_breakStagger > 0f)
            {
                if (_ai != null) _ai.Stagger(_breakStagger);
                else if (_stunnable != null) _stunnable.ApplyStun(_breakStagger);
            }
        }

        private void Restore()
        {
            _state.Restore();
            if (_visual  != null) _visual.SetActive(true);
            if (_barrier != null) _barrier.SetActive(true);
        }

        // From the hit point if it has one, otherwise from where the attacker stands.
        private bool TryGetHitDirection(in DamageInfo info, Vector3 point, out Vector3 from)
        {
            from = point - transform.position;
            from.y = 0f;
            if (point != Vector3.zero && from.sqrMagnitude > 0.01f) return true;

            if (info.Source.Owner == null) return false;
            from = info.Source.Owner.transform.position - transform.position;
            return from.sqrMagnitude > 0.01f;
        }
    }
}
