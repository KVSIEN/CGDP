using System;
using System.Collections.Generic;

namespace CGD.Weapons
{
    // Runs the perks of the weapon in hand when something happens: every perk on it with that
    // trigger and off cooldown applies. Cooldowns are kept per perk, not per weapon, so two
    // weapons with the same perk can't be swapped between to get around it.
    public sealed class PerkDispatcher
    {
        private readonly Dictionary<WeaponPerk, float> _readyAt = new();
        private readonly PerkContext _context;

        public PerkDispatcher(PerkContext context) => _context = context;

        // A perk that went off (for its on-screen cue).
        public event Action<WeaponPerk> Triggered;

        public void Fire(PerkTrigger trigger, WeaponItem weapon, float now)
        {
            if (weapon == null) return;
            _context.Weapon = weapon;

            IReadOnlyList<WeaponPerk> perks = weapon.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                WeaponPerk perk = perks[i];
                if (perk == null || perk.Trigger != trigger) continue;
                if (_readyAt.TryGetValue(perk, out float readyAt) && now < readyAt) continue;
                if (!perk.Apply(_context)) continue;

                if (perk.Cooldown > 0f) _readyAt[perk] = now + perk.Cooldown;
                Triggered?.Invoke(perk);
            }
        }

        // Perk buffs only hold while their weapon is in hand, so they never boost the next weapon
        // of a combo: each weapon's power stays its own.
        public void WeaponChanged() => _context.EndBuffs();

        public void ResetCooldowns() => _readyAt.Clear();
    }
}
