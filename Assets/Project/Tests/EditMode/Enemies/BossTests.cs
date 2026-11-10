using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // A boss fight escalates through its phases in order, once each, and the boss is the
    // enemy it's built from made tougher.
    public class BossTests
    {
        [Test]
        public void PhasesStartAsHealthDropsPastEachThreshold()
        {
            var phases = new BossPhases(new[] { 0.33f, 0.66f });
            Assert.AreEqual(3, phases.Count);
            Assert.AreEqual(0, phases.Current);

            Assert.AreEqual(0, phases.Advance(0.9f));
            Assert.AreEqual(1, phases.Advance(0.6f));
            Assert.AreEqual(1, phases.Current);
            Assert.AreEqual(0, phases.Advance(0.5f), "staying inside a phase starts nothing");
            Assert.AreEqual(1, phases.Advance(0.2f));
            Assert.AreEqual(2, phases.Current);
        }

        [Test]
        public void OneBigHitEntersEveryPhaseItPasses()
        {
            var phases = new BossPhases(new[] { 0.75f, 0.5f, 0.25f });
            Assert.AreEqual(3, phases.Advance(0.1f));
            Assert.AreEqual(3, phases.Current);
        }

        [Test]
        public void HealingNeverReplaysAPhase()
        {
            var phases = new BossPhases(new[] { 0.5f });
            phases.Advance(0.4f);
            Assert.AreEqual(0, phases.Advance(1f));
            Assert.AreEqual(1, phases.Current);
            Assert.AreEqual(0, phases.Advance(0.3f));
        }

        [Test]
        public void ABossWithoutPhasesStaysInItsOpeningPhase()
        {
            var phases = new BossPhases(new float[0]);
            Assert.AreEqual(1, phases.Count);
            Assert.AreEqual(0, phases.Advance(0f));
        }

        [Test]
        public void DefaultBossIsTougherAndEscalatesFromTheTop()
        {
            BossDefinition boss = BossDefinition.CreateDefault();
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.MaxHealth = 100f;
            enemy.Armor     = 0f;
            try
            {
                EnemyData data = boss.CreateData(enemy);
                Assert.Greater(data.MaxHealth, enemy.MaxHealth);
                Assert.Greater(data.Armor, enemy.Armor);
                Assert.AreEqual(100f, enemy.MaxHealth, "the enemy's own data is left alone");
                Object.DestroyImmediate(data);

                BossPhase[] ordered = boss.PhasesInOrder();
                Assert.Greater(ordered.Length, 0);
                for (int i = 1; i < ordered.Length; i++)
                    Assert.GreaterOrEqual(ordered[i - 1].HealthBelow, ordered[i].HealthBelow);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void APhaseMakesTheBossHitHarderAndFaster()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.AttackDamage   = 10f;
            data.ChaseSpeed     = 5f;
            data.AttackCooldown = 1f;
            try
            {
                new BossPhase { DamageMultiplier = 1.5f, SpeedMultiplier = 1.2f, AttackRateMultiplier = 2f }.ApplyTo(data);
                Assert.AreEqual(15f, data.AttackDamage, 1e-4f);
                Assert.AreEqual(6f, data.ChaseSpeed, 1e-4f);
                Assert.AreEqual(0.5f, data.AttackCooldown, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }
    }
}
