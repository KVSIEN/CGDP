using System;
using System.Collections.Generic;
using CGD.Items;

namespace CGD.Weapons
{
    // Anything that goes in a weapon slot: a firearm (WeaponInstance) or a melee weapon
    // (MeleeWeaponInstance). The loadout, weapon wheel, pickups and shops work with this,
    // and each kind keeps its own stats and its own controller.
    public abstract class WeaponItem : ItemInstance
    {
        private IReadOnlyList<WeaponPerk> _perks = Array.Empty<WeaponPerk>();

        protected WeaponItem(GearDefinition definition, ItemRoll roll) : base(definition, roll) { }

        // Hand-authored weapon placed directly in a scene: no roll, no quality, no attachment slots, no perks.
        protected WeaponItem(ItemDefinition definition) : base(definition) { }

        // Rolled with the weapon and fixed for its life, unlike attachments.
        public IReadOnlyList<WeaponPerk> Perks => _perks;

        // "Evasive Reload, Reap", or empty without perks. For labels and listings.
        public string PerkNames()
        {
            if (_perks.Count == 0) return string.Empty;

            var names = new string[_perks.Count];
            for (int i = 0; i < names.Length; i++) names[i] = _perks[i] != null ? _perks[i].DisplayName : "?";
            return string.Join(", ", names);
        }

        internal void SetPerks(IReadOnlyList<WeaponPerk> perks) => _perks = perks ?? Array.Empty<WeaponPerk>();

        // Restores whatever the weapon spends (a firearm's magazine) on revive.
        public virtual void Refill() { }
    }
}
