using System;
using UnityEngine;

namespace CGD.Meters
{
    // An authored price in a resource: "30 Mana", "25 Stamina". Leave Meter empty for
    // no cost. A character without the named meter can't pay it — a mana ability on
    // someone with no mana is unusable rather than free.
    [Serializable]
    public struct MeterCost
    {
        public MeterDefinition Meter;
        [Min(0f)] public float Amount;

        public bool IsFree => Meter == null || Amount <= 0f;

        public bool CanAfford(MeterSet meters) =>
            IsFree || (meters != null && meters.TryGet(Meter, out var meter) && meter.CanSpend(Amount));

        public bool TryPay(MeterSet meters)
        {
            if (IsFree) return true;
            return meters != null && meters.TryGet(Meter, out var meter) && meter.TrySpend(Amount);
        }

        // Continuous costs (Amount is per second). Returns false once the meter can't
        // cover it, so the caller stops the action — e.g. sprint ends when stamina runs out.
        public bool TryDrain(MeterSet meters, float deltaTime)
        {
            if (IsFree) return true;
            if (meters == null || !meters.TryGet(Meter, out var meter)) return false;
            if (meter.IsEmpty || meter.IsExhausted) return false;

            meter.Drain(Amount * deltaTime);
            return true;
        }
    }
}
