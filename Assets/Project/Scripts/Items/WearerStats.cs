using UnityEngine;
using CGD.Perks;
using CGD.Stats;

namespace CGD.Items
{
    // Puts gear that changes its wielder — worn armor, a held artifact — onto CharacterStats:
    // everything it rolled (with its attachments applied) as flat bonuses, plus its passive perks
    // as authored. The gear is the Source of its own modifiers, so re-applying is remove + add and
    // taking it off is RemoveFrom.
    public static class WearerStats
    {
        public static void Apply(CharacterStats stats, ItemInstance gear)
        {
            if (stats == null) return;

            stats.RemoveFrom(gear);
            for (int i = 1; i < ItemStatTraits.Count; i++)
            {
                var stat    = (ItemStat)i;
                float value = gear.GetStat(stat);
                if (!Mathf.Approximately(value, 0f))
                    stats.Add(stat, StatModifierOp.Additive, value, gear);
            }

            foreach (GearPerk perk in gear.Perks)
            {
                if (perk is not PassivePerk passive) continue;
                foreach (StatModifier modifier in passive.Modifiers)
                    if (modifier.Stat != ItemStat.None)
                        stats.Add(modifier.Stat, modifier.Op, modifier.Value, gear);
            }
        }

        public static void Remove(CharacterStats stats, ItemInstance gear) => stats?.RemoveFrom(gear);
    }
}
