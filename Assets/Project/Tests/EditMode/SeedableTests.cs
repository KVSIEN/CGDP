using NUnit.Framework;
using UnityEngine;
using CGD.Core;

namespace CGD.Tests
{
    // A level hands each placed or spawned object one seed; every seedable part of it must
    // get its own child of that seed, the same one every time.
    public class SeedableTests
    {
        private class Recorder : MonoBehaviour, ISeedable
        {
            public Seed? Seed;
            public void ApplySeed(Seed seed) => Seed = seed;
        }

        private static (Seed first, Seed second) SeedTwoParts(Seed seed)
        {
            var root  = new GameObject("Seeded");
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            Recorder a = root.AddComponent<Recorder>();
            Recorder b = child.AddComponent<Recorder>();

            ISeedable.SeedAll(root, seed);
            var result = (a.Seed.Value, b.Seed.Value);
            Object.DestroyImmediate(root);
            return result;
        }

        [Test]
        public void EveryPartGetsItsOwnStableSeed()
        {
            (Seed first, Seed second) = SeedTwoParts(Seed.Parse("level"));
            (Seed againFirst, Seed againSecond) = SeedTwoParts(Seed.Parse("level"));

            Assert.AreNotEqual(first, second);
            Assert.AreEqual(first, againFirst);
            Assert.AreEqual(second, againSecond);
        }

        [Test]
        public void DifferentSeedsGiveDifferentParts()
        {
            Assert.AreNotEqual(SeedTwoParts(Seed.Parse("a")).first, SeedTwoParts(Seed.Parse("b")).first);
        }
    }
}
