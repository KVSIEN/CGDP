using System.Collections.Generic;
using NUnit.Framework;
using CGD.Core;

namespace CGD.Tests
{
    public class RandomStreamTests
    {
        [Test]
        public void SameSeedGivesTheSameSequence()
        {
            var a = Seed.From(42).Stream();
            var b = Seed.From(42).Stream();

            for (int i = 0; i < 20; i++) Assert.AreEqual(a.NextULong(), b.NextULong());
        }

        [Test]
        public void DifferentSeedsDiverge()
        {
            Assert.AreNotEqual(Seed.From(1).Stream().NextULong(), Seed.From(2).Stream().NextULong());
        }

        [Test]
        public void ValueStaysInUnitInterval()
        {
            var stream = Seed.From(3).Stream();

            for (int i = 0; i < 1000; i++) Assert.That(stream.Value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }

        [Test]
        public void IntRangeIsMinInclusiveMaxExclusive()
        {
            var stream = Seed.From(5).Stream();
            var seen = new HashSet<int>();

            for (int i = 0; i < 500; i++) seen.Add(stream.Range(2, 6));

            CollectionAssert.AreEquivalent(new[] { 2, 3, 4, 5 }, seen);
        }

        [Test]
        public void EmptyIntRangeReturnsMin()
        {
            Assert.AreEqual(4, Seed.From(1).Stream().Range(4, 4));
        }

        [Test]
        public void ForkDependsOnTheSeedNotOnHowFarTheParentWasRead()
        {
            var fresh = Seed.From(9).Stream();
            var used  = Seed.From(9).Stream();
            for (int i = 0; i < 50; i++) used.NextULong();

            Assert.AreEqual(fresh.Fork("map").NextULong(), used.Fork("map").NextULong());
        }

        [Test]
        public void DifferentForkLabelsGiveDifferentStreams()
        {
            var stream = Seed.From(9).Stream();

            Assert.AreNotEqual(stream.Fork("map").NextULong(), stream.Fork("loot").NextULong());
        }

        [Test]
        public void VariantZeroIsThePlainDerive()
        {
            var seed = Seed.From(11);

            Assert.AreEqual(seed.Derive("layout"), seed.Derive("layout", 0));
            Assert.AreNotEqual(seed.Derive("layout"), seed.Derive("layout", 1));
        }

        [Test]
        public void ParseUsesNumbersAsIsAndHashesText()
        {
            Assert.AreEqual(new Seed(123UL), Seed.Parse(" 123 "));
            Assert.AreEqual(Seed.Parse("banana"), Seed.Parse("banana"));
            Assert.AreNotEqual(Seed.Parse("banana"), Seed.Parse("apple"));
        }

        [Test]
        public void WeightedPickNeverChoosesZeroWeightItems()
        {
            var items  = new List<string> { "never", "always" };
            var stream = Seed.From(2).Stream();

            for (int i = 0; i < 200; i++)
                Assert.AreEqual("always", stream.PickWeighted(items, s => s == "always" ? 1f : 0f));
        }

        [Test]
        public void WeightedPickWithNoWeightReturnsDefault()
        {
            var items = new List<string> { "a", "b" };

            Assert.IsNull(Seed.From(2).Stream().PickWeighted(items, _ => 0f));
        }

        [Test]
        public void ShuffleKeepsEveryItem()
        {
            var items = new List<int> { 1, 2, 3, 4, 5, 6 };
            Seed.From(8).Stream().Shuffle(items);

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, items);
        }
    }
}
