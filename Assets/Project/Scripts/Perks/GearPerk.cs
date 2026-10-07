using UnityEngine;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Perks
{
    // A rolled trait on a piece of gear, fixed for its life (unlike attachments). Either
    // passive (always-on stat changes, see PassivePerk) or triggered by a moment in combat
    // (see TriggeredPerk). Perks are shared assets; what differs per item is which it rolled.
    public abstract class GearPerk : ScriptableObject
    {
        [SerializeField] private string _displayName = "Perk";
        [Tooltip("Shown on the gear, e.g. \"Dodging reloads this weapon\"")]
        [SerializeField, TextArea] private string _description;
        [SerializeField] private PerkFits _fits = PerkFits.Firearm | PerkFits.Melee;

        public string DisplayName => _displayName;
        public string Description => _description;

        public virtual bool Fits(ItemInstance gear) => gear switch
        {
            WeaponInstance      => (_fits & PerkFits.Firearm) != 0,
            MeleeWeaponInstance => (_fits & PerkFits.Melee)   != 0,
            _ when gear?.Definition is ArmorDefinition => (_fits & PerkFits.Armor) != 0,
            _                   => false,
        };
    }
}
