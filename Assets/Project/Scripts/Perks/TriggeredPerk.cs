using UnityEngine;

namespace CGD.Perks
{
    // A perk that does something when its trigger happens (loads rounds, heals, grants a short
    // buff…). Subclasses decide the effect.
    public abstract class TriggeredPerk : GearPerk
    {
        [SerializeField] private PerkTrigger _trigger;
        [Tooltip("Chance it goes off each time the trigger happens")]
        [SerializeField, Range(0f, 1f)] private float _chance = 1f;
        [Tooltip("Seconds before it can go off again. Shared by every item with this perk, so swapping between two doesn't double it up")]
        [SerializeField, Min(0f)] private float _cooldown;

        public PerkTrigger Trigger  => _trigger;
        public float       Chance   => _chance;
        public float       Cooldown => _cooldown;

        // Returns whether it did anything; a perk that had nothing to do (a full magazine,
        // full health) doesn't start its cooldown.
        public abstract bool Apply(PerkContext context);
    }
}
