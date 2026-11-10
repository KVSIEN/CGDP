using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    public class TargetWeightsTests
    {
        private static TargetTraits Healthy(bool current = false, bool hurtIt = false, bool distracted = false) =>
            new(current, hurtIt, distracted, healthShare: 1f);

        [Test]
        public void DefaultOnlyFavoursTheCurrentTarget()
        {
            TargetWeights weights = TargetWeights.Default;

            Assert.AreEqual(1f, weights.Weigh(Healthy()), 1e-4f);
            Assert.AreEqual(1.25f, weights.Weigh(Healthy(current: true)), 1e-4f);
            Assert.AreEqual(1f, weights.Weigh(new TargetTraits(false, true, true, 0.1f)), 1e-4f);
        }

        [Test]
        public void WoundedScalesWithWhatIsMissing()
        {
            var weights = new TargetWeights(focus: 1f, retaliation: 0f, wounded: 1f, distracted: 0f);

            Assert.AreEqual(1f,   weights.Weigh(new TargetTraits(false, false, false, 1f)),   1e-4f);
            Assert.AreEqual(1.5f, weights.Weigh(new TargetTraits(false, false, false, 0.5f)), 1e-4f);
            Assert.AreEqual(2f,   weights.Weigh(new TargetTraits(false, false, false, 0f)),   1e-4f);
        }

        [Test]
        public void TraitsMultiply()
        {
            var weights = new TargetWeights(focus: 1.5f, retaliation: 1f, wounded: 0f, distracted: 2f);

            Assert.AreEqual(1.5f * 2f * 3f, weights.Weigh(Healthy(current: true, hurtIt: true, distracted: true)), 1e-4f);
        }

        [Test]
        public void ShootingBackBeatsStayingFocused()
        {
            // A counter-fire temperament turns on whoever is shooting it even if its current
            // target is sensed half again as strongly.
            var weights = new TargetWeights(focus: 1.5f, retaliation: 2.5f, wounded: 0.25f, distracted: 0f);

            float current  = 1.5f * weights.Weigh(Healthy(current: true));
            float attacker = 1f   * weights.Weigh(Healthy(hurtIt: true));
            Assert.Greater(attacker, current);
        }

        [Test]
        public void FinishOnlyWeighsTheCurrentTargetByWhatIsMissing()
        {
            var weights = new TargetWeights(focus: 1f, retaliation: 0f, wounded: 0f, distracted: 0f, finish: 2f);

            Assert.AreEqual(1f, weights.Weigh(new TargetTraits(true, false, false, 1f)),    1e-4f);
            Assert.AreEqual(2f, weights.Weigh(new TargetTraits(true, false, false, 0.5f)),  1e-4f);
            Assert.AreEqual(1f, weights.Weigh(new TargetTraits(false, false, false, 0.5f)), 1e-4f);
        }

        [Test]
        public void CounterFireSeesAWornDownTargetThroughButNotAFreshOne()
        {
            // Tech's temperament: a hostile shooting it still pulls it off a fresh target, but not
            // off one it has worn down to a third.
            var weights = new TargetWeights(focus: 1.5f, retaliation: 2.5f, wounded: 0.25f, distracted: 0f, finish: 3f);

            float attacker = weights.Weigh(Healthy(hurtIt: true));
            Assert.Greater(attacker, weights.Weigh(Healthy(current: true)));
            Assert.Greater(weights.Weigh(new TargetTraits(true, false, false, 0.33f)), attacker);
        }

        [Test]
        public void ClampsNonsenseValues()
        {
            var weights = new TargetWeights(focus: 0.2f, retaliation: -1f, wounded: -1f, distracted: -1f);

            Assert.AreEqual(1f, weights.Weigh(new TargetTraits(true, true, true, 0f)), 1e-4f);
        }
    }
}
