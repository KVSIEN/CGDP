using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // Where a supporter wants to stand: reaching its allies, behind their line (or holding its
    // front), in cover.
    public class SupportSpotTests
    {
        // Two allies side by side at z = 0, the threat 10 m in front of them at z = 10.
        private static readonly List<Vector3> Line = new() { new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f) };
        private static readonly Vector3 Threat = new(0f, 0f, 10f);

        private static SupportSituation Fight(float reach = 8f) => new(Line, Threat, true, reach);
        private static SupportSituation HoldingTheLine(float reach = 8f) => new(Line, Threat, true, reach, SupportLine.Front);

        [Test]
        public void TheFrontIsTheAllyNearestTheThreat()
        {
            var situation = new SupportSituation(new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 5f) }, Threat, true, 8f);
            Assert.AreEqual(5f, situation.FrontDistance, 0.001f);
        }

        [Test]
        public void TheIdealSpotIsBehindTheLine()
        {
            Vector3 ideal = SupportSpot.Ideal(Fight(), 4f);
            // 4 m further from the threat than the front ally, who is √101 m from it.
            float expected = 10f - (Mathf.Sqrt(101f) + 4f);
            Assert.AreEqual(expected, ideal.z, 0.001f);
            Assert.AreEqual(0f, ideal.x, 0.001f);
        }

        [Test]
        public void ItNeverStandsBackFurtherThanMostOfItsReach()
        {
            Vector3 ideal = SupportSpot.Ideal(Fight(reach: 5f), 20f);
            Assert.AreEqual(-4f, ideal.z, 0.001f, "0.8 of a 5 m reach");
        }

        [Test]
        public void WithoutAThreatItGoesToItsAlliesCentre()
        {
            var calm = new SupportSituation(Line, Vector3.zero, false, 8f);
            Assert.AreEqual(Vector3.zero, SupportSpot.Ideal(calm, 4f));
        }

        [Test]
        public void BehindTheLineBeatsInFrontOfIt()
        {
            float behind  = SupportSpot.Score(new Vector3(0f, 0f, -3f), false, false, 0f, Fight());
            float inFront = SupportSpot.Score(new Vector3(0f, 0f, 3f), false, false, 0f, Fight());
            Assert.Greater(behind, inFront);
        }

        [Test]
        public void ReachingMoreAlliesBeatsReachingFewer()
        {
            float both = SupportSpot.Score(new Vector3(0f, 0f, -3f), false, false, 0f, Fight());
            float one  = SupportSpot.Score(new Vector3(8f, 0f, -2f), false, false, 0f, Fight());
            Assert.Greater(both, one);
        }

        [Test]
        public void CoverFacingTheThreatBeatsOpenGround()
        {
            Vector3 spot = new(0f, 0f, -3f);
            Assert.Greater(SupportSpot.Score(spot, true, false, 0f, Fight()), SupportSpot.Score(spot, false, false, 0f, Fight()));
            Assert.Greater(SupportSpot.Score(spot, true, true, 0f, Fight()), SupportSpot.Score(spot, true, false, 0f, Fight()));
        }

        [Test]
        public void StandingOnAnAllyIsWorseThanBesideIt()
        {
            float onTop  = SupportSpot.Score(new Vector3(1f, 0f, -0.5f), false, false, 0f, Fight());
            float beside = SupportSpot.Score(new Vector3(0f, 0f, -3f), false, false, 0f, Fight());
            Assert.Greater(beside, onTop);
        }

        [Test]
        public void AFarWalkCostsSomething()
        {
            Vector3 spot = new(0f, 0f, -3f);
            Assert.Greater(SupportSpot.Score(spot, false, false, 0f, Fight()), SupportSpot.Score(spot, false, false, 10f, Fight()));
        }

        [Test]
        public void HoldingTheLineItsIdealSpotIsInFrontOfIt()
        {
            Vector3 ideal = SupportSpot.Ideal(HoldingTheLine(), 2f);
            // 2 m nearer the threat than the front ally, who is √101 m from it.
            float expected = 10f - (Mathf.Sqrt(101f) - 2f);
            Assert.AreEqual(expected, ideal.z, 0.001f);
            Assert.AreEqual(0f, ideal.x, 0.001f);
        }

        [Test]
        public void HoldingTheLineItNeverStandsFurtherOutThanMostOfItsReach()
        {
            Vector3 ideal = SupportSpot.Ideal(HoldingTheLine(reach: 5f), 20f);
            Assert.AreEqual(4f, ideal.z, 0.001f, "0.8 of a 5 m reach");
        }

        [Test]
        public void HoldingTheLineInFrontOfItBeatsBehindIt()
        {
            float inFront = SupportSpot.Score(new Vector3(0f, 0f, 2f), false, false, 0f, HoldingTheLine());
            float behind  = SupportSpot.Score(new Vector3(0f, 0f, -3f), false, false, 0f, HoldingTheLine());
            Assert.Greater(inFront, behind);
        }
    }
}
