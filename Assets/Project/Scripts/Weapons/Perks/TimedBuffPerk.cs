using UnityEngine;
using CGD.Items;

namespace CGD.Weapons
{
    // A short buff on the wielder, e.g. +25% damage for 5 s after a kill. It ends early when the
    // weapon is swapped away, so it only ever boosts the weapon that earned it.
    [CreateAssetMenu(fileName = "TimedBuffPerk", menuName = "CGD/Weapons/Perks/Timed Buff")]
    public class TimedBuffPerk : WeaponPerk
    {
        [SerializeField] private StatModifier[] _modifiers = System.Array.Empty<StatModifier>();
        [SerializeField, Min(0.1f)] private float _duration = 5f;

        public override bool Apply(PerkContext context) => context.Buff(this, _modifiers, _duration);
    }
}
