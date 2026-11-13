using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // A shield carried in front (a riot bot, a bulwark): hits from within its arc in front lose
    // most of their damage; from the side or behind they land in full. The answer to it is to
    // flank — so pair it with Advance Facing, which keeps the front to the target. Lightning
    // goes through it (it shorts the shield out) unless Stops Lightning is on.
    [DisallowMultipleComponent]
    public class FrontalShield : MonoBehaviour, IDamageInterceptor
    {
        [Tooltip("Degrees either side of straight ahead that the shield covers")]
        [SerializeField, Range(5f, 90f)] private float _halfArc = 60f;
        [Tooltip("Share of a covered hit's damage the shield stops")]
        [SerializeField, Range(0f, 1f)] private float _reduction = 0.8f;
        [SerializeField] private bool _stopsLightning;

        // Before guards and reflectors: the shield takes the blow first.
        public int Order => -10;

        public float Intercept(in DamageInfo info, float amount, Vector3 point)
        {
            if (info.Type == DamageType.Lightning && !_stopsLightning) return amount;
            if (!TryGetHitDirection(info, point, out Vector3 from)) return amount;
            return Covers(transform.forward, from, _halfArc) ? amount * (1f - _reduction) : amount;
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
