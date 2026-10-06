using UnityEngine;
using CGD.Combat;
using CGD.Meters;
using CGD.Weapons;

namespace CGD.Player
{
    // Fills an earned meter (Rage) from fighting: damage this character deals, kills and
    // parries. It is the uncommon cost of a few unique abilities, so staying aggressive is
    // what pays for them; the meter itself drains when the fighting stops (see its regen).
    [RequireComponent(typeof(MeterSet))]
    public class CombatMeterGain : MonoBehaviour
    {
        [Tooltip("The earned meter; it must be on this character's MeterSet")]
        [SerializeField] private MeterDefinition _meter;
        [Tooltip("Gained per point of damage dealt")]
        [SerializeField, Min(0f)] private float _perDamage = 0.25f;
        [Tooltip("Most one hit can give, so a single big hit doesn't fill the meter")]
        [SerializeField, Min(0f)] private float _maxPerHit = 8f;
        [SerializeField, Min(0f)] private float _perKill   = 10f;
        [SerializeField, Min(0f)] private float _perParry  = 15f;

        private Meter _target;
        private MeleeController _melee;

        private void Awake()
        {
            TryGetComponent(out _melee);
        }

        private void Start()
        {
            // MeterSet builds its meters in Awake.
            if (_meter != null) GetComponent<MeterSet>().TryGet(_meter, out _target);
        }

        private void OnEnable()
        {
            CombatEvents.DamageDealt += OnDamageDealt;
            if (_melee != null) _melee.Parried += OnParried;
        }

        private void OnDisable()
        {
            CombatEvents.DamageDealt -= OnDamageDealt;
            if (_melee != null) _melee.Parried -= OnParried;
        }

        private void OnDamageDealt(DamageReport report)
        {
            if (_target == null || report.Source.Owner != gameObject) return;

            float gain = Mathf.Min(report.Amount * _perDamage, _maxPerHit);
            if (report.Killed) gain += _perKill;
            _target.Restore(gain);
        }

        private void OnParried() => _target?.Restore(_perParry);
    }
}
