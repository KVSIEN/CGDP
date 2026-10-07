using UnityEngine;
using CGD.Items;

namespace CGD.Perks
{
    // A short buff on the wearer, e.g. +25% damage for 5 s after a kill. A weapon's buff ends
    // early when the weapon is swapped away, so it only ever boosts the weapon that earned it;
    // an armor piece's buff runs its full time.
    [CreateAssetMenu(fileName = "TimedBuffPerk", menuName = "CGD/Perks/Timed Buff")]
    public class TimedBuffPerk : TriggeredPerk
    {
        [SerializeField] private StatModifier[] _modifiers = System.Array.Empty<StatModifier>();
        [SerializeField, Min(0.1f)] private float _duration = 5f;

        public override bool Apply(PerkContext context) => context.Buff(this, _modifiers, _duration);
    }
}
