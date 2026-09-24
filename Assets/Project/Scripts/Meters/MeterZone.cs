using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Meters
{
    // Trigger volume that drains or restores one meter on everyone inside: water drains
    // Oxygen, a toxic cloud drains Stamina, a shrine restores Mana. Optionally hurts
    // characters whose meter has run dry while they stay inside (drowning, suffocation).
    [RequireComponent(typeof(Collider))]
    public class MeterZone : MonoBehaviour
    {
        [SerializeField] private MeterDefinition _meter;
        [Tooltip("Units per second. Negative drains, positive restores.")]
        [SerializeField] private float _ratePerSecond = -10f;

        [Header("Empty Meter")]
        [Tooltip("Damage dealt each tick to characters inside whose meter is empty (0 = none)")]
        [SerializeField, Min(0f)] private float _damageWhenEmpty;
        [SerializeField, Min(0.05f)] private float _damageInterval = 1f;
        [SerializeField] private DamageType _damageType = DamageType.Physical;

        // A character can overlap with several colliders (capsule + hitboxes); it stays an
        // occupant until the last one leaves.
        private readonly Dictionary<MeterSet, int> _overlaps  = new();
        private readonly List<MeterSet>            _occupants = new();
        private float _damageTimer;

        private void OnTriggerEnter(Collider other)
        {
            MeterSet set = other.GetComponentInParent<MeterSet>();
            if (set == null) return;

            _overlaps.TryGetValue(set, out int count);
            _overlaps[set] = count + 1;
            if (count == 0) _occupants.Add(set);
        }

        private void OnTriggerExit(Collider other)
        {
            MeterSet set = other.GetComponentInParent<MeterSet>();
            if (set == null || !_overlaps.TryGetValue(set, out int count)) return;

            if (count > 1)
            {
                _overlaps[set] = count - 1;
                return;
            }

            _overlaps.Remove(set);
            _occupants.Remove(set);
        }

        private void OnDisable()
        {
            _overlaps.Clear();
            _occupants.Clear();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            _damageTimer -= deltaTime;
            bool damageTick = _damageTimer <= 0f;
            if (damageTick) _damageTimer = _damageInterval;

            for (int i = _occupants.Count - 1; i >= 0; i--)
            {
                MeterSet set = _occupants[i];
                if (set == null || !set.isActiveAndEnabled)
                {
                    _occupants.RemoveAt(i);
                    _overlaps.Remove(set);
                    continue;
                }

                if (!set.TryGet(_meter, out Meter meter)) continue;

                ApplyRate(meter, deltaTime);
                if (damageTick && meter.IsEmpty) DamageOccupant(set);
            }
        }

        private void ApplyRate(Meter meter, float deltaTime)
        {
            if (_ratePerSecond < 0f) meter.Drain(-_ratePerSecond * deltaTime);
            else                     meter.Restore(_ratePerSecond * deltaTime);
        }

        private void DamageOccupant(MeterSet set)
        {
            if (_damageWhenEmpty <= 0f || !set.TryGetComponent(out HealthManager health)) return;

            // Teamless damage: the environment hurts everyone.
            health.TakeDamage(new DamageInfo(_damageWhenEmpty, type: _damageType));
        }
    }
}
