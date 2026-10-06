using UnityEngine;
using CGD.Meters;

namespace CGD.Weapons
{
    // Refills part of a meter, e.g. stamina back on a parry.
    [CreateAssetMenu(fileName = "RestoreMeterPerk", menuName = "CGD/Weapons/Perks/Restore Meter")]
    public class RestoreMeterPerk : WeaponPerk
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
