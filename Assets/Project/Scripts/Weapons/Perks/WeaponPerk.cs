using UnityEngine;

namespace CGD.Weapons
{
    // A rolled trait on a weapon: when its trigger happens with the weapon in hand, it does
    // something (loads rounds, heals, grants a short buff…). Perks are shared assets; what
    // differs per weapon is which ones it rolled. Subclasses decide the effect.
    public abstract class WeaponPerk : ScriptableObject
    {
        [SerializeField] private string _displayName = "Perk";
        [Tooltip("Shown on the weapon, e.g. \"Dodging reloads this weapon\"")]
        [SerializeField, TextArea] private string _description;
        [SerializeField] private PerkTrigger _trigger;
        [SerializeField] private PerkWeaponKinds _fits = PerkWeaponKinds.Firearm | PerkWeaponKinds.Melee;
        [Tooltip("Seconds before it can go off again. Shared by every weapon with this perk, so swapping between two doesn't double it up")]
        [SerializeField, Min(0f)] private float _cooldown;

        public string      DisplayName => _displayName;
        public string      Description => _description;
        public PerkTrigger Trigger     => _trigger;
        public float       Cooldown    => _cooldown;

        public virtual bool Fits(WeaponItem weapon) => weapon switch
        {
            WeaponInstance      => (_fits & PerkWeaponKinds.Firearm) != 0,
            MeleeWeaponInstance => (_fits & PerkWeaponKinds.Melee)   != 0,
            _                   => false,
        };

        // Returns whether it did anything; a perk that had nothing to do (a full magazine,
        // full health) doesn't start its cooldown.
        public abstract bool Apply(PerkContext context);
    }
}
