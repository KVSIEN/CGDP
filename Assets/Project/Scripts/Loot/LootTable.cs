using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;

namespace CGD.Loot
{
    // What something yields when it dies, breaks or is opened.
    //
    // A roll always gives every Guaranteed entry, then draws Rolls times from the
    // weighted Pool, where Nothing Weight is the chance of a draw coming up empty. Entries
    // can point at other tables, so shared pools ("common ammo", "boss uniques") are
    // authored once and nested.
    //
    // Gear rolled here gets its rarity from the Tier Checks, run from the highest tier down
    // (Legendary 1%, else Epic 5%, else …, else Common 100%), which luck skews toward the
    // higher tiers. A tier with its own pool swaps the drawn gear for a roll of that pool, all
    // at that tier, so a 1% Legendary hit can draw from a Legendary-only table.
    //
    // Every draw comes from one RandomStream, and each gear drop gets the stream's next
    // seed, so rolling a table from the same seed yields the same drops, rolls and all.
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "CGD/Loot/Loot Table")]
    public class LootTable : ScriptableObject
    {
        private const int MaxNesting = 8;

        [Header("Guaranteed")]
        [Tooltip("Always dropped, every roll")]
        [SerializeField] private LootEntry[] _guaranteed = Array.Empty<LootEntry>();

        [Header("Weighted Pool")]
        [Tooltip("How many draws are made from the pool")]
        [SerializeField] private IntRange _rolls = new(1, 1);
        [SerializeField] private LootEntry[] _pool = Array.Empty<LootEntry>();
        [Tooltip("Weight of a draw giving nothing, relative to the pool entries")]
        [SerializeField, Min(0f)] private float _nothingWeight;
        [Tooltip("Each pool entry can be drawn at most once per roll")]
        [SerializeField] private bool _uniqueDraws;

        [Header("Rarity (gear only)")]
        [Tooltip("Checked from the highest tier down; the first that hits is the gear's tier. Empty, or no check hitting, = gear drops at its definition's own tier.")]
        [SerializeField] private TierCheck[] _tierChecks = Array.Empty<TierCheck>();

        // Appends this table's drops to results. Luck 0 = authored odds; positive luck makes
        // empty draws rarer and higher tiers likelier (1 = each tier step ×2).
        public void Roll(List<LootDrop> results, float luck = 0f) => Roll(results, luck, Seed.Random());

        public void Roll(List<LootDrop> results, float luck, Seed seed) =>
            Roll(results, Mathf.Max(0f, luck), seed.Stream(), 0, null);

        // `tier`: set when rolling a tier's pool — its gear drops at that tier, unchecked.
        private void Roll(List<LootDrop> results, float luck, RandomStream random, int depth, ItemTier? tier)
        {
            if (depth > MaxNesting) return;

            foreach (LootEntry entry in _guaranteed)
            {
                if (entry != null && entry.IsValid) Resolve(entry, results, luck, random, depth, tier);
            }

            RollPool(results, luck, random, depth, tier);
        }

        private void RollPool(List<LootDrop> results, float luck, RandomStream random, int depth, ItemTier? tier)
        {
            if (_pool.Length == 0) return;

            // Loot is rolled occasionally, not per frame; a small array keeps this readable.
            bool[] drawn   = _uniqueDraws ? new bool[_pool.Length] : null;
            float  nothing = _nothingWeight / (1f + luck);
            int    draws   = _rolls.Evaluate(random);

            for (int i = 0; i < draws; i++)
            {
                int index = Draw(drawn, nothing, random);
                if (index < 0) continue;

                if (drawn != null) drawn[index] = true;
                Resolve(_pool[index], results, luck, random, depth, tier);
            }
        }

        // Index of the drawn pool entry, or -1 for an empty draw.
        private int Draw(bool[] drawn, float nothingWeight, RandomStream random)
        {
            float total = nothingWeight;
            for (int i = 0; i < _pool.Length; i++)
                total += WeightOf(i, drawn);

            if (total <= 0f) return -1;

            float pick = random.Value * total;
            for (int i = 0; i < _pool.Length; i++)
            {
                pick -= WeightOf(i, drawn);
                if (pick < 0f) return i;
            }
            return -1;
        }

        private float WeightOf(int index, bool[] drawn)
        {
            LootEntry entry = _pool[index];
            if (entry == null || !entry.IsValid || (drawn != null && drawn[index])) return 0f;
            return entry.Weight;
        }

        private void Resolve(LootEntry entry, List<LootDrop> results, float luck, RandomStream random, int depth, ItemTier? tier)
        {
            int count = entry.RollCount(random);

            switch (entry.Kind)
            {
                case LootEntryKind.Item:
                    AddItem(entry.Item, count, results, luck, random, depth, tier);
                    break;

                case LootEntryKind.Table:
                    for (int i = 0; i < count; i++)
                        entry.Table.Roll(results, luck, random, depth + 1, tier);
                    break;

                case LootEntryKind.Prefab:
                    for (int i = 0; i < count; i++)
                        results.Add(LootDrop.Spawn(entry.Prefab));
                    break;
            }
        }

        // Stackables drop as one counted stack; gear rolls one unique piece per count, each with
        // its own tier — or, when that tier has a pool, as a roll of the pool instead.
        private void AddItem(ItemDefinition item, int count, List<LootDrop> results, float luck,
                             RandomStream random, int depth, ItemTier? tier)
        {
            if (item is not GearDefinition gear)
            {
                results.Add(LootDrop.Stack(item, count));
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (tier.HasValue)
                {
                    AddGear(gear, tier.Value, results, random);
                    continue;
                }

                int check = RollTierCheck(luck, random);
                if (check < 0)
                {
                    AddGear(gear, gear.Tier, results, random);
                    continue;
                }

                TierCheck hit = _tierChecks[check];
                if (hit.Pool != null) hit.Pool.Roll(results, luck, random, depth + 1, hit.Tier);
                else                  AddGear(gear, hit.Tier, results, random);
            }
        }

        private static void AddGear(GearDefinition gear, ItemTier tier, List<LootDrop> results, RandomStream random)
        {
            ItemInstance instance = gear.CreateInstance(gear.Roll(tier, random.NextSeed()));
            if (instance != null) results.Add(LootDrop.Gear(instance));
        }

        // Index of the tier check that hits, or -1 when none does (gear keeps its own tier).
        //
        // Running the checks one by one, top tier first, gives each tier the chance left over
        // from the tiers above times its own. Drawing once from those shares gives the same odds,
        // and lets luck lean them toward the high tiers the way it always has (×(1 + luck) per
        // tier step). Whatever the checks leave over is the chance of no hit.
        private int RollTierCheck(float luck, RandomStream random)
        {
            if (_tierChecks.Length == 0) return -1;

            // Loot is rolled occasionally, not per frame; a small array keeps this readable.
            var   weights   = new float[_tierChecks.Length];
            float remaining = 1f;
            float total     = 0f;
            for (int tier = (int)ItemTier.Legendary; tier >= (int)ItemTier.Common; tier--)
            {
                int check = CheckFor((ItemTier)tier);
                if (check < 0) continue;

                float share = remaining * Mathf.Clamp01(_tierChecks[check].Chance);
                remaining     -= share;
                weights[check] = share * Mathf.Pow(1f + luck, tier);
                total         += weights[check];
            }

            float pick = random.Value * (total + remaining);
            for (int i = 0; i < weights.Length; i++)
            {
                pick -= weights[i];
                if (pick < 0f) return i;
            }
            return -1;
        }

        // The first check listed for this tier, or -1.
        private int CheckFor(ItemTier tier)
        {
            for (int i = 0; i < _tierChecks.Length; i++)
                if (_tierChecks[i].Tier == tier) return i;
            return -1;
        }
    }
}
