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
    // higher tiers. The outermost table with checks decides for everything nested in it, so a
    // shared pool ("weapons") rolls at the odds of whichever container draws from it. A tier
    // with its own pool swaps the drawn gear for a roll of that pool, all at that tier, so a
    // 1% Legendary hit can draw from a Legendary-only table.
    //
    // A roll can be given a LootFilter (a container's subtype, "Pistols"): entries it rejects,
    // and nested tables with nothing it allows, are skipped and the rest share the odds. A
    // filter that would leave the whole table empty is ignored instead.
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

        // Everything one roll carries down into the tables nested in it.
        private readonly struct RollContext
        {
            public readonly List<LootDrop> Results;
            public readonly float          Luck;
            public readonly RandomStream   Random;
            public readonly int            Depth;
            public readonly TierCheck[]    Checks;   // an outer table's, which win over a nested one's own
            public readonly ItemTier?      Tier;     // set inside a tier's pool: its gear drops at that tier, unchecked
            public readonly LootFilter     Filter;

            public RollContext(List<LootDrop> results, float luck, RandomStream random, int depth,
                               TierCheck[] checks, ItemTier? tier, LootFilter filter)
            {
                Results = results;
                Luck    = luck;
                Random  = random;
                Depth   = depth;
                Checks  = checks;
                Tier    = tier;
                Filter  = filter;
            }

            public RollContext Nested(TierCheck[] checks, ItemTier? tier) =>
                new(Results, Luck, Random, Depth + 1, checks, tier, Filter);
        }

        // Appends this table's drops to results. Luck 0 = authored odds; positive luck makes
        // empty draws rarer and higher tiers likelier (1 = each tier step ×2). `filter`: only
        // what it allows drops, unless it allows nothing here.
        public void Roll(List<LootDrop> results, float luck = 0f) => Roll(results, luck, Seed.Random());

        public void Roll(List<LootDrop> results, float luck, Seed seed, LootFilter filter = null)
        {
            if (filter != null && !CanYield(filter, 0)) filter = null;
            Roll(new RollContext(results, Mathf.Max(0f, luck), seed.Stream(), 0, null, null, filter));
        }

        private void Roll(in RollContext ctx)
        {
            if (ctx.Depth > MaxNesting) return;

            RollContext roll = ctx.Checks == null && _tierChecks.Length > 0
                ? new RollContext(ctx.Results, ctx.Luck, ctx.Random, ctx.Depth, _tierChecks, ctx.Tier, ctx.Filter)
                : ctx;

            foreach (LootEntry entry in _guaranteed)
            {
                if (Allows(entry, roll.Filter, roll.Depth)) Resolve(entry, roll);
            }

            RollPool(roll);
        }

        // Whether anything in this table, nested tables included, passes the filter.
        private bool CanYield(LootFilter filter, int depth)
        {
            if (depth > MaxNesting) return false;

            foreach (LootEntry entry in _guaranteed)
                if (Allows(entry, filter, depth)) return true;
            foreach (LootEntry entry in _pool)
                if (Allows(entry, filter, depth)) return true;
            return false;
        }

        private static bool Allows(LootEntry entry, LootFilter filter, int depth)
        {
            if (entry == null || !entry.IsValid) return false;
            if (filter == null) return true;

            return entry.Kind switch
            {
                LootEntryKind.Item  => filter.Allows(entry.Item),
                LootEntryKind.Table => entry.Table.CanYield(filter, depth + 1),
                _                   => false,
            };
        }

        private void RollPool(in RollContext ctx)
        {
            if (_pool.Length == 0) return;

            // Loot is rolled occasionally, not per frame; small arrays keep this readable.
            var    weights = new float[_pool.Length];
            for (int i = 0; i < _pool.Length; i++)
                weights[i] = Allows(_pool[i], ctx.Filter, ctx.Depth) ? _pool[i].Weight : 0f;

            float nothing = _nothingWeight / (1f + ctx.Luck);
            int   draws   = _rolls.Evaluate(ctx.Random);

            for (int i = 0; i < draws; i++)
            {
                int index = Draw(weights, nothing, ctx.Random);
                if (index < 0) continue;

                if (_uniqueDraws) weights[index] = 0f;
                Resolve(_pool[index], ctx);
            }
        }

        // Index of the drawn pool entry, or -1 for an empty draw.
        private static int Draw(float[] weights, float nothingWeight, RandomStream random)
        {
            float total = nothingWeight;
            foreach (float weight in weights)
                total += weight;

            if (total <= 0f) return -1;

            float pick = random.Value * total;
            for (int i = 0; i < weights.Length; i++)
            {
                pick -= weights[i];
                if (pick < 0f) return i;
            }
            return -1;
        }

        private static void Resolve(LootEntry entry, in RollContext ctx)
        {
            int count = entry.RollCount(ctx.Random);

            switch (entry.Kind)
            {
                case LootEntryKind.Item:
                    AddItem(entry.Item, count, ctx);
                    break;

                case LootEntryKind.Table:
                    for (int i = 0; i < count; i++)
                        entry.Table.Roll(ctx.Nested(ctx.Checks, ctx.Tier));
                    break;

                case LootEntryKind.Prefab:
                    for (int i = 0; i < count; i++)
                        ctx.Results.Add(LootDrop.Spawn(entry.Prefab));
                    break;
            }
        }

        // Stackables drop as one counted stack; gear rolls one unique piece per count, each with
        // its own tier — or, when that tier has a pool, as a roll of the pool instead.
        private static void AddItem(ItemDefinition item, int count, in RollContext ctx)
        {
            if (item is not GearDefinition gear)
            {
                ctx.Results.Add(LootDrop.Stack(item, count));
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (ctx.Tier.HasValue)
                {
                    AddGear(gear, ctx.Tier.Value, ctx);
                    continue;
                }

                int check = RollTierCheck(ctx.Checks, ctx.Luck, ctx.Random);
                if (check < 0)
                {
                    AddGear(gear, gear.Tier, ctx);
                    continue;
                }

                TierCheck hit = ctx.Checks[check];
                if (hit.Pool != null && (ctx.Filter == null || hit.Pool.CanYield(ctx.Filter, ctx.Depth + 1)))
                    hit.Pool.Roll(ctx.Nested(ctx.Checks, hit.Tier));
                else
                    AddGear(gear, hit.Tier, ctx);
            }
        }

        private static void AddGear(GearDefinition gear, ItemTier tier, in RollContext ctx)
        {
            ItemInstance instance = gear.CreateInstance(gear.Roll(tier, ctx.Random.NextSeed()));
            if (instance != null) ctx.Results.Add(LootDrop.Gear(instance));
        }

        // Index of the tier check that hits, or -1 when none does (gear keeps its own tier).
        //
        // Running the checks one by one, top tier first, gives each tier the chance left over
        // from the tiers above times its own. Drawing once from those shares gives the same odds,
        // and lets luck lean them toward the high tiers the way it always has (×(1 + luck) per
        // tier step). Whatever the checks leave over is the chance of no hit.
        private static int RollTierCheck(TierCheck[] checks, float luck, RandomStream random)
        {
            if (checks == null || checks.Length == 0) return -1;

            var   weights   = new float[checks.Length];
            float remaining = 1f;
            float total     = 0f;
            for (int tier = (int)ItemTier.Legendary; tier >= (int)ItemTier.Common; tier--)
            {
                int check = CheckFor(checks, (ItemTier)tier);
                if (check < 0) continue;

                float share = remaining * Mathf.Clamp01(checks[check].Chance);
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
        private static int CheckFor(TierCheck[] checks, ItemTier tier)
        {
            for (int i = 0; i < checks.Length; i++)
                if (checks[i].Tier == tier) return i;
            return -1;
        }
    }
}
