using System.Collections.Generic;
using CGD.Combat;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Weapons
{
    // What a perk can act on: the weapon in hand and the wielder. Any of the wielder's parts
    // may be missing, and perks that need one do nothing without it.
    public sealed class PerkContext
    {
        private readonly Dictionary<WeaponPerk, TimedModifier> _buffs = new();

        public PerkContext(WeaponController firearm, CharacterStats stats, HealthManager health, MeterSet meters)
        {
            Firearm = firearm;
            Stats   = stats;
            Health  = health;
            Meters  = meters;
        }

        public WeaponItem       Weapon  { get; internal set; }
        public WeaponController Firearm { get; }
        public CharacterStats   Stats   { get; }
        public HealthManager    Health  { get; }
        public MeterSet         Meters  { get; }

        // Triggering a perk's buff again restarts it rather than stacking a second copy. Buffs
        // belong to the weapon in hand: EndBuffs runs when it is swapped away.
        public bool Buff(WeaponPerk perk, IEnumerable<StatModifier> modifiers, float seconds)
        {
            if (Stats == null || seconds <= 0f) return false;

            if (_buffs.TryGetValue(perk, out TimedModifier running) && running.IsActive) running.End();
            _buffs[perk] = Stats.AddTimed(modifiers, seconds);
            return true;
        }

        public void EndBuffs()
        {
            foreach (TimedModifier buff in _buffs.Values)
                if (buff.IsActive) buff.End();
            _buffs.Clear();
        }
    }
}
