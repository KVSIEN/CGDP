using NUnit.Framework;
using CGD.Stats;

namespace CGD.Tests
{
    public class ModifierSetTests
    {
        private enum TestStat { Damage, Speed }

        private static Modifier Mod(StatModifierOp op, float value, object source = null) => new(op, value, source);

        [Test]
        public void UnmodifiedStatReturnsBase()
        {
            var set = new ModifierSet<TestStat>();

            Assert.AreEqual(10f, set.Apply(TestStat.Damage, 10f), 0.0001f);
        }

        [Test]
        public void AdditiveThenMultiplicativeThenCompound()
        {
            var set = new ModifierSet<TestStat>();
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 5f));
            set.Add(TestStat.Damage, Mod(StatModifierOp.Multiplicative, 0.5f));
            set.Add(TestStat.Damage, Mod(StatModifierOp.Compound, 0.1f));

            // (10 + 5) × (1 + 0.5) × 1.1
            Assert.AreEqual(24.75f, set.Apply(TestStat.Damage, 10f), 0.0001f);
        }

        [Test]
        public void MultiplicativeSumsAndCompoundMultiplies()
        {
            var summed = new ModifierSet<TestStat>();
            summed.Add(TestStat.Speed, Mod(StatModifierOp.Multiplicative, 0.1f));
            summed.Add(TestStat.Speed, Mod(StatModifierOp.Multiplicative, 0.1f));

            var compounded = new ModifierSet<TestStat>();
            compounded.Add(TestStat.Speed, Mod(StatModifierOp.Compound, 0.1f));
            compounded.Add(TestStat.Speed, Mod(StatModifierOp.Compound, 0.1f));

            Assert.AreEqual(1.2f,  summed.Apply(TestStat.Speed, 1f),     0.0001f);
            Assert.AreEqual(1.21f, compounded.Apply(TestStat.Speed, 1f), 0.0001f);
        }

        [Test]
        public void OverrideReplacesEverythingElse()
        {
            var set = new ModifierSet<TestStat>();
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 5f));
            set.Add(TestStat.Damage, Mod(StatModifierOp.Override, 0f));

            Assert.AreEqual(0f, set.Apply(TestStat.Damage, 10f), 0.0001f);
        }

        [Test]
        public void StatsAreIndependent()
        {
            var set = new ModifierSet<TestStat>();
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 5f));

            Assert.AreEqual(3f, set.Apply(TestStat.Speed, 3f), 0.0001f);
        }

        [Test]
        public void RemoveFromDropsOnlyThatSourcesModifiers()
        {
            var buff = new object();
            var gear = new object();
            var set = new ModifierSet<TestStat>();
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 5f, buff));
            set.Add(TestStat.Speed,  Mod(StatModifierOp.Additive, 2f, buff));
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 1f, gear));

            int removed = set.RemoveFrom(buff);

            Assert.AreEqual(2, removed);
            Assert.AreEqual(11f, set.Apply(TestStat.Damage, 10f), 0.0001f);
            Assert.AreEqual(3f,  set.Apply(TestStat.Speed, 3f),   0.0001f);
        }

        [Test]
        public void ChangedFiresForEachAffectedStat()
        {
            var source = new object();
            var set = new ModifierSet<TestStat>();
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 1f, source));
            set.Add(TestStat.Speed,  Mod(StatModifierOp.Additive, 1f, source));

            int changes = 0;
            set.Changed += _ => changes++;
            set.RemoveFrom(source);

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void ClearRemovesEveryModifier()
        {
            var set = new ModifierSet<TestStat>();
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 5f));
            set.Clear();

            Assert.AreEqual(10f, set.Apply(TestStat.Damage, 10f), 0.0001f);
            Assert.IsEmpty(set.ModifiersOf(TestStat.Damage));
        }

        [Test]
        public void ListenerMayAddModifiersWhileChangedFires()
        {
            var source = new object();
            var set = new ModifierSet<TestStat>();
            set.Add(TestStat.Damage, Mod(StatModifierOp.Additive, 1f, source));
            set.Changed += stat =>
            {
                if (stat == TestStat.Damage) set.Add(TestStat.Speed, Mod(StatModifierOp.Additive, 2f));
            };

            Assert.DoesNotThrow(() => set.RemoveFrom(source));
            Assert.DoesNotThrow(() => set.Clear());
        }
    }
}
