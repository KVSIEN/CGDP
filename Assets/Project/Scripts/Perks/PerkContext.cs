using System.Collections.Generic;
using CGD.Combat;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.Perks
{
    // What a perk can act on: the gear it sits on and the wearer. Any of the wearer's parts
    // may be missing, and perks that need one do nothing without it.
    public sealed class PerkContext
    {
        private readonly Dictionary<TriggeredPerk, (TimedModifier buff, ItemInstance from)> _buffs = new();

        public PerkContext(WeaponController firearm, CharacterStats stats, HealthManager health, MeterSet meters)
        {
            Firearm = firearm;
            Stats   = stats;
            Health  = health;
            Meters  = meters;
        }

        // The item whose perk is going off: the weapon in hand or a worn armor piece.
        public ItemInstance     Gear    { get; internal set; }
        public WeaponController Firearm { get; }
        public CharacterStats   Stats   { get; }
        public HealthManager    Health  { get; }
        public MeterSet         Meters  { get; }

        // The gun in hand, if the perk may act on it: a weapon's perks only on itself, armor's
        // perks on whatever gun is held.
        public WeaponInstance HeldFirearm
        {
            get
            {
                WeaponInstance held = Firearm != null ? Firearm.Current : null;
                if (held == null) return null;
                return Gear is WeaponItem && Gear != held ? null : held;
            }
        }

        // Triggering a perk's buff again restarts it rather than stacking a second copy.
        public bool Buff(TriggeredPerk perk, IEnumerable<StatModifier> modifiers, float seconds)
        {
            if (Stats == null || seconds <= 0f) return false;

            if (_buffs.TryGetValue(perk, out var running) && running.buff.IsActive) running.buff.End();
            _buffs[perk] = (Stats.AddTimed(modifiers, seconds), Gear);
            return true;
        }

        // A weapon's buffs only hold while it is in hand, so they never boost the next weapon of
        // a combo; armor's buffs run their course.
        public void EndBuffsFrom(ItemInstance gear)
        {
            if (gear == null) return;
            foreach (var entry in _buffs.Values)
                if (entry.from == gear && entry.buff.IsActive) entry.buff.End();
        }
    }
}
