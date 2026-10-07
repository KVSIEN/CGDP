using UnityEngine;
using CGD.Meters;

namespace CGD.Perks
{
    // Refills part of a meter, e.g. stamina back on a parry.
    [CreateAssetMenu(fileName = "RestoreMeterPerk", menuName = "CGD/Perks/Restore Meter")]
    public class RestoreMeterPerk : TriggeredPerk
    {
        [SerializeField] private MeterDefinition _meter;
        [SerializeField, Min(0f)] private float _amount = 25f;

        public override bool Apply(PerkContext context)
        {
            if (_meter == null || context.Meters == null || !context.Meters.TryGet(_meter, out Meter meter)) return false;
            if (meter.IsFull) return false;

            meter.Restore(_amount);
            return true;
        }
    }
}
