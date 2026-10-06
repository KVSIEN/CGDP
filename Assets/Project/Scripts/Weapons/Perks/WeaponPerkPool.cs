using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;

namespace CGD.Weapons
{
    // The perks weapons can roll, and how many by tier. Each weapon draws its own from its
    // seed, without repeats and only from perks that fit it (a reload perk never lands on a
    // sword), so the same category, tier and seed always get the same perks.
    [CreateAssetMenu(fileName = "WeaponPerkPool", menuName = "CGD/Weapons/Perks/Perk Pool")]
    public class WeaponPerkPool : ScriptableObject
    {
        private const string PerksLayer = "perks";

        [SerializeField] private WeightedPerk[] _perks = Array.Empty<WeightedPerk>();
        [Tooltip("Perks per weapon, indexed Common -> Legendary")]
        [SerializeField] private int[] _perksPerTier = { 0, 1, 1, 2, 2 };
        [Tooltip("Chance of one perk more than the tier gives")]
        [SerializeField, Range(0f, 1f)] private float _extraPerkChance = 0.25f;

        public IReadOnlyList<WeaponPerk> Roll(WeaponItem weapon, ItemRoll roll)
        {
            RandomStream random = roll.Seed.Derive(PerksLayer).Stream();
            int count = CountFor(roll.Tier) + (random.Chance(_extraPerkChance) ? 1 : 0);

            var candidates = new List<WeightedPerk>();
            foreach (WeightedPerk entry in _perks)
                if (entry.Perk != null && entry.Weight > 0f && entry.Perk.Fits(weapon))
                    candidates.Add(entry);

            var rolled = new List<WeaponPerk>(count);
            while (rolled.Count < count && candidates.Count > 0)
            {
                WeightedPerk pick = random.PickWeighted(candidates, e => e.Weight);
                rolled.Add(pick.Perk);
                candidates.RemoveAll(e => e.Perk == pick.Perk);
            }
            return rolled;
        }

        private int CountFor(ItemTier tier)
        {
            int index = (int)tier;
            return _perksPerTier != null && index < _perksPerTier.Length ? Mathf.Max(0, _perksPerTier[index]) : 0;
        }
    }
}
