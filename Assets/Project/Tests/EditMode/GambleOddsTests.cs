using NUnit.Framework;
using CGD.Economy;

namespace CGD.Tests
{
    public class GambleOddsTests
    {
        // Weights 4 / 5 / 1 out of 10: bust below 0.4, win below 0.9, jackpot above. Rolls stay off the exact boundaries so float rounding can't decide a test.
        private static GambleOdds Odds() => new(4f, 5f, 1f, 0.5f);

        [Test]
        public void RollsMapToOutcomesByWeight()
        {
            GambleOdds odds = Odds();

            Assert.AreEqual(GambleResult.Bust,    odds.Resolve(0f));
            Assert.AreEqual(GambleResult.Bust,    odds.Resolve(0.39f));
            Assert.AreEqual(GambleResult.Win,     odds.Resolve(0.41f));
            Assert.AreEqual(GambleResult.Win,     odds.Resolve(0.89f));
            Assert.AreEqual(GambleResult.Jackpot, odds.Resolve(0.91f));
            Assert.AreEqual(GambleResult.Jackpot, odds.Resolve(0.999f));
        }

        [Test]
        public void ZeroWeightsAlwaysBust()
        {
            Assert.AreEqual(GambleResult.Bust, new GambleOdds(0f, 0f, 0f, 0f).Resolve(0.7f));
        }

        [Test]
        public void PriceGrowsByShareOfFirstPricePerPull()
        {
            GambleOdds odds = Odds();

            Assert.AreEqual(100, odds.PriceFor(100, 0));
            Assert.AreEqual(150, odds.PriceFor(100, 1));
            Assert.AreEqual(200, odds.PriceFor(100, 2));
        }

        [Test]
        public void NegativePullsDoNotDiscount()
        {
            Assert.AreEqual(100, Odds().PriceFor(100, -3));
        }
    }
}
