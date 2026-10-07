using NUnit.Framework;
using CGD.Core;

namespace CGD.Tests
{
    public class RangeTests
    {
        [Test]
        public void FloatRangeInterpolatesBetweenItsEnds()
        {
            var range = new FloatRange(10f, 20f);

            Assert.AreEqual(10f, range.Lerp(0f), 0.0001f);
            Assert.AreEqual(20f, range.Lerp(1f), 0.0001f);
        }

        [Test]
        public void FloatRangeClampsThePosition()
        {
            var range = new FloatRange(10f, 20f);

            Assert.AreEqual(10f, range.Lerp(-5f), 0.0001f);
            Assert.AreEqual(20f, range.Lerp(5f), 0.0001f);
        }

        [Test]
        public void PositiveBiasSkewsTowardMaxAndNegativeTowardMin()
        {
            var low  = new FloatRange(0f, 100f, -1f);
            var mid  = new FloatRange(0f, 100f, 0f);
            var high = new FloatRange(0f, 100f, 1f);

            Assert.Less(low.Lerp(0.5f), mid.Lerp(0.5f));
            Assert.Less(mid.Lerp(0.5f), high.Lerp(0.5f));
        }

        [Test]
        public void SeededEvaluationIsReproducible()
        {
            var range = new FloatRange(0f, 100f);

            float a = range.Evaluate(Seed.From(7).Stream());
            float b = range.Evaluate(Seed.From(7).Stream());

            Assert.AreEqual(a, b);
        }

        [Test]
        public void EvaluateClampedStaysInsideTheRange()
        {
            var range  = new FloatRange(5f, 6f);
            var stream = Seed.From(1).Stream();

            for (int i = 0; i < 200; i++)
            {
                float value = range.EvaluateClamped(stream);
                Assert.That(value, Is.InRange(5f, 6f));
            }
        }

        [Test]
        public void IntRangeRoundsAndHitsBothEnds()
        {
            var range = new IntRange(3, 9);

            Assert.AreEqual(3, range.Lerp(0f));
            Assert.AreEqual(9, range.Lerp(1f));
        }
    }
}
