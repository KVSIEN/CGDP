using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // How a lost target's likely whereabouts are weighed (BeliefModel).
    public class BeliefModelTests
    {
        private static readonly Vector3 Seen = Vector3.zero;

        [Test]
        public void SpotsAheadOfTheTargetBeatSpotsBehindIt()
        {
            Vector3 heading = Vector3.forward;
            float ahead  = BeliefModel.Weight(new Vector3(0f, 0f, 10f),  Seen, heading, 5f, 2.5f);
            float behind = BeliefModel.Weight(new Vector3(0f, 0f, -10f), Seen, heading, 5f, 2.5f);
            Assert.Greater(ahead, behind);
        }

        [Test]
        public void TheLikelyDistanceGrowsWithTimeSinceSighting()
        {
            Vector3 spot = new(0f, 0f, 20f);
            float soon  = BeliefModel.Weight(spot, Seen, Vector3.forward, 5f, 1f);
            float later = BeliefModel.Weight(spot, Seen, Vector3.forward, 5f, 5f);
            Assert.Greater(later, soon);
        }

        [Test]
        public void UnknownHeadingTreatsEverySideAlike()
        {
            float a = BeliefModel.Weight(new Vector3(8f, 0f, 0f),  Seen, Vector3.zero, 4f, 2f);
            float b = BeliefModel.Weight(new Vector3(-8f, 0f, 0f), Seen, Vector3.zero, 4f, 2f);
            Assert.AreEqual(a, b, 1e-4f);
        }

        [Test]
        public void WeightsStayWithinZeroAndOne()
        {
            for (float d = 0f; d <= 60f; d += 5f)
            for (float t = 0f; t <= 40f; t += 5f)
            {
                float w = BeliefModel.Weight(new Vector3(d, 0f, 0f), Seen, Vector3.right, 5f, t);
                Assert.That(w, Is.InRange(0f, 1f));
            }
        }
    }
}
