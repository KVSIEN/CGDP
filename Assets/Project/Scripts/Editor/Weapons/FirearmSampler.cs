using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Editor
{
    // The numbers a designer judges a firearm roll by.
    internal enum FirearmStat
    {
        Damage, FireRate, Dps, TimeToKill, Magazine, Reload, RecoilVertical, RecoilSideways, HipSpread, Range,
    }

    internal readonly struct FirearmSample
    {
        public readonly WeaponModel Model;   // null when the category has no models
        public readonly string      Name;
        private readonly float[]    _stats;

        public FirearmSample(WeaponModel model, string name, float[] stats)
        {
            Model  = model;
            Name   = name;
            _stats = stats;
        }

        public float this[FirearmStat stat] => _stats[(int)stat];
    }

    // Min / average / max of one stat over a set of rolls.
    internal readonly struct StatSpread
    {
        public readonly float Min, Average, Max;
        public StatSpread(float min, float average, float max) { Min = min; Average = average; Max = max; }
    }

    // Rolls a category many times, the same way the game does, and sums up what came out — so a
    // new model or changed weights can be judged by the guns they actually produce.
    internal static class FirearmSampler
    {
        // The baseline enemy from WEAPON_BALANCE.md: time to kill is measured against this much health.
        public const float TargetHealth = 100f;

        public static readonly int StatCount = Enum.GetValues(typeof(FirearmStat)).Length;

        public static List<FirearmSample> Roll(WeaponCategoryData category, ItemTier tier, int count, ulong baseSeed)
        {
            var samples = new List<FirearmSample>(count);
            var seeds   = new RandomStream(new Seed(baseSeed));
            for (int i = 0; i < count; i++)
                samples.Add(RollOne(category, tier, seeds.NextSeed()));
            return samples;
        }

        private static FirearmSample RollOne(WeaponCategoryData category, ItemTier tier, Seed seed)
        {
            ItemRoll roll = category.Roll(tier, seed);

            // The build is the first thing a roll draws from its character stream, so picking it again here names the model the weapon below is made from.
            WeaponModel model = WeaponBuild.Pick(category, roll.StreamFor(WeaponGenerator.CharacterLayer)).Model;

            WeaponInstance weapon = WeaponGenerator.Generate(category, roll);
            try   { return new FirearmSample(model, weapon.DisplayName, Measure(weapon)); }
            finally { UnityEngine.Object.DestroyImmediate(weapon.Data); }
        }

        private static float[] Measure(WeaponInstance weapon)
        {
            WeaponData data = weapon.Data;
            int   pellets   = Mathf.Max(1, data.PelletCount);
            float perShot   = weapon.Damage * pellets;
            float rpm       = weapon.RoundsPerMinute;

            var stats = new float[FirearmSampler.StatCount];
            stats[(int)FirearmStat.Damage]         = perShot;
            stats[(int)FirearmStat.FireRate]       = rpm;
            stats[(int)FirearmStat.Dps]            = perShot * rpm / 60f;
            stats[(int)FirearmStat.TimeToKill]     = TimeToKill(perShot, rpm);
            stats[(int)FirearmStat.Magazine]       = weapon.MagazineSize;
            stats[(int)FirearmStat.Reload]         = weapon.ReloadTime;
            stats[(int)FirearmStat.RecoilVertical] = data.RecoilScale.y;
            stats[(int)FirearmStat.RecoilSideways] = data.RecoilScale.x;
            stats[(int)FirearmStat.HipSpread]      = data.HipSpreadDeg;
            stats[(int)FirearmStat.Range]          = data.RangeOptimal;
            return stats;
        }

        // Seconds from the first shot to the kill, every shot landing at full damage (burst gaps ignored).
        public static float TimeToKill(float damagePerShot, float roundsPerMinute)
        {
            if (damagePerShot <= 0f || roundsPerMinute <= 0f) return float.PositiveInfinity;
            int shots = Mathf.CeilToInt(TargetHealth / damagePerShot);
            return (shots - 1) * 60f / roundsPerMinute;
        }

        public static StatSpread Spread(IReadOnlyList<FirearmSample> samples, FirearmStat stat)
        {
            if (samples.Count == 0) return default;
            float min = float.MaxValue, max = float.MinValue, sum = 0f;
            foreach (FirearmSample sample in samples)
            {
                float value = sample[stat];
                min  = Mathf.Min(min, value);
                max  = Mathf.Max(max, value);
                sum += value;
            }
            return new StatSpread(min, sum / samples.Count, max);
        }

        // Share of rolls that came out as each distinct weapon name (model × firing style × stat style).
        public static List<KeyValuePair<string, float>> NameShares(IReadOnlyList<FirearmSample> samples)
        {
            var counts = new Dictionary<string, int>();
            foreach (FirearmSample sample in samples)
            {
                counts.TryGetValue(sample.Name, out int n);
                counts[sample.Name] = n + 1;
            }

            var shares = new List<KeyValuePair<string, float>>();
            foreach (KeyValuePair<string, int> pair in counts)
                shares.Add(new KeyValuePair<string, float>(pair.Key, pair.Value / (float)samples.Count));
            shares.Sort((a, b) => b.Value.CompareTo(a.Value));
            return shares;
        }

        public static List<FirearmSample> Of(IReadOnlyList<FirearmSample> samples, WeaponModel model)
        {
            var own = new List<FirearmSample>();
            foreach (FirearmSample sample in samples)
                if (sample.Model == model) own.Add(sample);
            return own;
        }
    }
}
