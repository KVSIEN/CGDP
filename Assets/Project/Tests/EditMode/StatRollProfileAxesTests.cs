using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CGD.Items;

namespace CGD.Tests
{
    public class StatRollProfileAxesTests
    {
        private static readonly (string Name, Action<StatRollProfile> Apply)[] GunPresets =
        {
            ("Standard firearm",  p => p.ApplyStandardFirearmAxes()),
            ("Precision rifle",   p => p.ApplyPrecisionRifleAxes()),
            ("Automatic support", p => p.ApplyAutomaticSupportAxes()),
            ("Shotgun",           p => p.ApplyShotgunAxes()),
        };

        private static StatRollProfile Build(Action<StatRollProfile> apply)
        {
            var profile = ScriptableObject.CreateInstance<StatRollProfile>();
            apply(profile);
            return profile;
        }

        [Test]
        public void NoStatSitsOnTwoAxes()
        {
            foreach (var (name, apply) in GunPresets)
            {
                StatRollProfile profile = Build(apply);
                ItemStat[] stats = profile.Axes.SelectMany(a => a.Gains.Concat(a.Costs)).ToArray();

                Assert.AreEqual(stats.Length, stats.Distinct().Count(), name);
            }
        }

        [Test]
        public void DamageFireRateAndRecoilShareOneAxis()
        {
            foreach (var (name, apply) in GunPresets)
            {
                StatRollProfile profile = Build(apply);
                TradeoffAxis axis = profile.Axes.Single(a => Holds(a, ItemStat.Damage) || Holds(a, ItemStat.FireRate));

                Assert.IsTrue(Holds(axis, ItemStat.Damage), name);
                Assert.IsTrue(Holds(axis, ItemStat.FireRate), name);
                Assert.IsTrue(Holds(axis, ItemStat.Recoil), name);
            }
        }

        [Test]
        public void DamageFireRateAndRecoilCannotAllGainTogether()
        {
            foreach (var (name, apply) in GunPresets)
            {
                StatRollProfile profile = Build(apply);
                TradeoffAxis axis = profile.Axes.Single(a => Holds(a, ItemStat.Damage));

                int onGainSide = new[] { ItemStat.Damage, ItemStat.FireRate, ItemStat.Recoil }
                    .Count(stat => axis.Gains.Contains(stat));

                Assert.That(onGainSide, Is.InRange(1, 2), name);
            }
        }

        private static bool Holds(TradeoffAxis axis, ItemStat stat) =>
            axis.Gains.Contains(stat) || axis.Costs.Contains(stat);
    }
}
