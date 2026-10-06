using System;
using UnityEngine;
using CGD.Combat;
using CGD.Weapons;

namespace CGD.Player
{
    // Counts this character's combat actions — landing a hit, a kill, a parry — for systems
    // that reward active play, such as ability Surge. Hits are counted at most once per
    // HitInterval so a fast-firing gun doesn't outpace a slow, heavy one.
    public class CombatActions : MonoBehaviour
    {
        private const float HitInterval = 0.25f;

        // Raised once per action (a killing hit counts as two: the hit and the kill).
        public event Action Performed;

        private MeleeController _melee;
        private float _lastHit = float.NegativeInfinity;

        private void Awake() => TryGetComponent(out _melee);

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
            if (report.Source.Owner != gameObject || report.Amount <= 0f) return;

            if (Time.time - _lastHit >= HitInterval)
            {
                _lastHit = Time.time;
                Performed?.Invoke();
            }
            if (report.Killed) Performed?.Invoke();
        }

        private void OnParried() => Performed?.Invoke();
    }
}
