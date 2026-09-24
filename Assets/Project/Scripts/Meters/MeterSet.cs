using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Meters
{
    // The resource meters a character owns (stamina, mana, oxygen...), built from the
    // listed definitions. Ticks regen and resets every meter when its HealthManager
    // revives. Systems look meters up by definition — or use MeterCost — rather than
    // holding their own copies.
    public class MeterSet : MonoBehaviour
    {
        [SerializeField] private MeterDefinition[] _definitions = Array.Empty<MeterDefinition>();

        private readonly List<Meter> _meters = new();
        private readonly Dictionary<MeterDefinition, Meter> _byDefinition = new();
        private HealthManager _health;

        public IReadOnlyList<Meter> Meters => _meters;

        private void Awake()
        {
            foreach (MeterDefinition definition in _definitions)
            {
                if (definition == null || _byDefinition.ContainsKey(definition)) continue;

                var meter = new Meter(definition);
                _meters.Add(meter);
                _byDefinition.Add(definition, meter);
            }

            if (TryGetComponent(out _health)) _health.OnRevived += ResetAll;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= ResetAll;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _meters.Count; i++)
                _meters[i].Tick(deltaTime);
        }

        public bool TryGet(MeterDefinition definition, out Meter meter)
        {
            meter = null;
            return definition != null && _byDefinition.TryGetValue(definition, out meter);
        }

        public void ResetAll()
        {
            for (int i = 0; i < _meters.Count; i++)
                _meters[i].Reset();
        }
    }
}
