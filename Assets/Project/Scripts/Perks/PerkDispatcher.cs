using System;
using System.Collections.Generic;

namespace CGD.Perks
{
    // Runs an item's triggered perks when something happens: every one with that trigger, off
    // cooldown and passing its chance applies. Cooldowns are kept per perk, not per item, so
    // two items with the same perk can't be swapped between to get around it.
    public sealed class PerkDispatcher
    {
        private readonly Dictionary<TriggeredPerk, float> _readyAt = new();
        private readonly PerkContext _context;
        private readonly Func<float> _random;
        // A perk's effect can set off another trigger (a heal on kill → Healed); those are
        // ignored so perks can't chain into a loop.
        private bool _firing;

        public PerkDispatcher(PerkContext context, Func<float> random)
        {
            _context = context;
            _random  = random;
        }

        // A perk that went off (for its on-screen cue).
        public event Action<TriggeredPerk> Triggered;

        public PerkContext Context => _context;

        public void Fire(PerkTrigger trigger, Items.ItemInstance gear, float now)
        {
            if (gear == null || _firing) return;

            _firing = true;
            _context.Gear = gear;
            try
            {
                IReadOnlyList<GearPerk> perks = gear.Perks;
                for (int i = 0; i < perks.Count; i++)
                {
                    if (perks[i] is not TriggeredPerk perk || perk.Trigger != trigger) continue;
                    if (_readyAt.TryGetValue(perk, out float readyAt) && now < readyAt) continue;
                    if (perk.Chance < 1f && _random() >= perk.Chance) continue;
                    if (!perk.Apply(_context)) continue;

                    if (perk.Cooldown > 0f) _readyAt[perk] = now + perk.Cooldown;
                    Triggered?.Invoke(perk);
                }
            }
            finally
            {
                _firing = false;
            }
        }

        public void ResetCooldowns() => _readyAt.Clear();
    }
}
