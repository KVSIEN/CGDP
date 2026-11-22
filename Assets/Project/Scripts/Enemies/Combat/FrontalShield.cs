using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // A shield carried in front (a riot bot, a bulwark): hits from within its arc in front lose
    // most of their damage; from the side or behind they land in full. The answer to it is to
    // flank — so pair it with Advance Facing, which keeps the front to the target. Lightning
    // goes through it (it shorts the shield out) unless Stops Lightning is on.
    // With a Durability it can also be broken by focused fire: the bearer reels for a moment and
    // fights without it until it comes back up, the other answer to a shield wall.
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

        private ShieldDurability _state;
        private HealthManager    _health;
        private EnemyAI          _ai;
        private Stunnable        _stunnable;

        // Before guards and reflectors: the shield takes the blow first.
        public int Order => -10;

        public bool IsUp => _state == null || _state.IsUp(Time.time);

        private void Awake()
        {
            _state = new ShieldDurability(_durability, _downSeconds);
            TryGetComponent(out _ai);
            TryGetComponent(out _stunnable);
            if (TryGetComponent(out _health)) _health.OnRevived += Restore;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= Restore;
        }

        // Brings the model back once a broken shield is up again.
        private void Update()
        {
            if (_visual != null && _visual.activeSelf != IsUp) _visual.SetActive(IsUp);
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
            if (_visual != null) _visual.SetActive(false);
            if (_breakStagger > 0f)
            {
                if (_ai != null) _ai.Stagger(_breakStagger);
                else if (_stunnable != null) _stunnable.ApplyStun(_breakStagger);
            }
        }

        private void Restore()
        {
            _state.Restore();
            if (_visual != null) _visual.SetActive(true);
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
