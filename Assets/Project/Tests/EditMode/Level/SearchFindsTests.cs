using System.Collections.Generic;
using NUnit.Framework;
using CGD.Core;
using CGD.Level;

namespace CGD.Tests
{
    // A level's search finds (SearchFinds): as many as its searchable pieces add, spread over
    // different pieces by weight.
    public class SearchFindsTests
    {
        private static int Count(bool[] holds)
        {
            int n = 0;
            foreach (bool h in holds) if (h) n++;
            return n;
        }

        [Test]
        public void TheLevelHoldsAsManyFindsAsItsPiecesAddWhereverTheyLand()
        {
            var spots = new List<SearchSpot>();
            for (int i = 0; i < 10; i++) spots.Add(new SearchSpot(1f, 0.45f));   // 4.5 finds between them
            for (int i = 0; i < 20; i++) spots.Add(new SearchSpot(1f, 0f));      // more furniture, no more loot

            int total = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                int n = Count(SearchFinds.Choose(spots, new RandomStream(Seed.From(seed))));
                Assert.That(n, Is.EqualTo(4).Or.EqualTo(5));
                total += n;
            }
            Assert.That(total / 200f, Is.EqualTo(4.5f).Within(0.25f), "the fraction of a find is drawn once, so the mean is kept");
        }

        [Test]
        public void HeavierPiecesHoldFindsMoreOftenAndWeightlessOnesNever()
        {
            var spots = new List<SearchSpot> { new(3f, 0.5f), new(1f, 0.5f), new(0f, 1f) };
            int heavy = 0, light = 0;
            for (int seed = 0; seed < 400; seed++)
            {
                bool[] holds = SearchFinds.Choose(spots, new RandomStream(Seed.From(seed)));
                Assert.That(holds[2], Is.False, "a piece with no weight is no place for a find");
                Assert.That(Count(holds), Is.EqualTo(2), "two finds between the two that can hold them");
                if (holds[0]) heavy++;
                if (holds[1]) light++;
            }
            Assert.That(heavy, Is.EqualTo(400), "two finds for two pieces: both hold one");

            spots = new List<SearchSpot> { new(3f, 0.5f), new(1f, 0.5f), new(1f, 0f), new(1f, 0f) };
            heavy = light = 0;
            for (int seed = 0; seed < 400; seed++)
            {
                bool[] holds = SearchFinds.Choose(spots, new RandomStream(Seed.From(seed)));
                if (holds[0]) heavy++;
                if (holds[1]) light++;
            }
            Assert.That(heavy, Is.GreaterThan(light * 2), "weight 3 against 1");
        }

        [Test]
        public void TheSameSeedChoosesTheSamePieces()
        {
            var spots = new List<SearchSpot>();
            for (int i = 0; i < 12; i++) spots.Add(new SearchSpot(1f + i % 3, 0.3f));
            Assert.That(SearchFinds.Choose(spots, new RandomStream(Seed.From(7))), Is.EqualTo(SearchFinds.Choose(spots, new RandomStream(Seed.From(7)))));
        }

        [Test]
        public void RoomsOffTheMainPathAndBehindGatesWeighMore()
        {
            var settings = new SearchLootSettings(2f, 3f);
            Assert.That(settings.RoomWeight(onMainPath: true, behindGate: false), Is.EqualTo(1f));
            Assert.That(settings.RoomWeight(onMainPath: false, behindGate: false), Is.EqualTo(2f));
            Assert.That(settings.RoomWeight(onMainPath: false, behindGate: true), Is.EqualTo(3f));
        }
    }
}
