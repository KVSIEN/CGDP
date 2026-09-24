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
    // authored once and nested. Gear rolled here gets its rarity from Tier Weights, which
    // luck skews toward the higher tiers.
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
        [Tooltip("Tier odds for rolled gear. Empty = gear drops at its definition's own tier.")]
        [SerializeField] private TierWeight[] _tierWeights = Array.Empty<TierWeight>();

        // Appends this table's drops to results. Luck 0 = authored odds; positive luck makes
        // empty draws rarer and higher tiers likelier (1 = each tier step ×2).
        public void Roll(List<LootDrop> results, float luck = 0f) => Roll(results, Mathf.Max(0f, luck), 0);

        private void Roll(List<LootDrop> results, float luck, int depth)
        {
            if (depth > MaxNesting) return;

            foreach (LootEntry entry in _guaranteed)
            {
                if (entry != null && entry.IsValid) Resolve(entry, results, luck, depth);
            }

            RollPool(results, luck, depth);
        }

        private void RollPool(List<LootDrop> results, float luck, int depth)
        {
            if (_pool.Length == 0) return;

            // Loot is rolled occasionally, not per frame; a small array keeps this readable.
            bool[] drawn   = _uniqueDraws ? new bool[_pool.Length] : null;
            float  nothing = _nothingWeight / (1f + luck);
            int    draws   = _rolls.Evaluate();

            for (int i = 0; i < draws; i++)
            {
                int index = Draw(drawn, nothing);
                if (index < 0) continue;

                if (drawn != null) drawn[index] = true;
                Resolve(_pool[index], results, luck, depth);
            }
        }

        // Index of the drawn pool entry, or -1 for an empty draw.
        private int Draw(bool[] drawn, float nothingWeight)
        {
            float total = nothingWeight;
            for (int i = 0; i < _pool.Length; i++)
                total += WeightOf(i, drawn);

            if (total <= 0f) return -1;

            float pick = UnityEngine.Random.value * total;
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

        private void Resolve(LootEntry entry, List<LootDrop> results, float luck, int depth)
        {
            int count = entry.RollCount();

            switch (entry.Kind)
            {
                case LootEntryKind.Item:
                    AddItem(entry.Item, count, results, luck);
                    break;

                case LootEntryKind.Table:
                    for (int i = 0; i < count; i++)
                        entry.Table.Roll(results, luck, depth + 1);
                    break;

                case LootEntryKind.Prefab:
                    for (int i = 0; i < count; i++)
                        results.Add(LootDrop.Spawn(entry.Prefab));
                    break;
            }
        }

        // Stackables drop as one counted stack; gear rolls one unique piece per count.
        private void AddItem(ItemDefinition item, int count, List<LootDrop> results, float luck)
        {
            if (item is not GearDefinition gear)
            {
                results.Add(LootDrop.Stack(item, count));
                return;
            }

            for (int i = 0; i < count; i++)
            {
                ItemInstance instance = gear.CreateInstance(gear.Roll(RollTier(gear.Tier, luck)));
                if (instance != null) results.Add(LootDrop.Gear(instance));
            }
        }

        private ItemTier RollTier(ItemTier fallback, float luck)
        {
            float total = 0f;
            foreach (TierWeight tier in _tierWeights)
                total += LuckWeighted(tier, luck);

            if (total <= 0f) return fallback;

            float pick = UnityEngine.Random.value * total;
            foreach (TierWeight tier in _tierWeights)
            {
                pick -= LuckWeighted(tier, luck);
                if (pick < 0f) return tier.Tier;
            }
            return fallback;
        }

        private static float LuckWeighted(TierWeight tier, float luck) =>
            tier.Weight * Mathf.Pow(1f + luck, (int)tier.Tier);
    }
}
