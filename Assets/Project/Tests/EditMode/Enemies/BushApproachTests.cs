using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;
using CGD.Stealth;

namespace CGD.Tests
{
    // How an enemy goes about a bush it knows: its whole patch, not a point.
    public class BushApproachTests
    {
        // A 6 × 2 m patch (x from 8 to 14, z from -1 to 1), long axis along x.
        private static readonly BushFootprint Patch = new(new Vector3(11f, 0f, 0f), new Vector2(3f, 1f), Vector3.right);

        [Test]
        public void AWalkAcrossThePatchComesOutTheFarSide()
        {
            Vector3 pass = BushApproach.PassPoint(new Vector3(0f, 0f, 0f), new Vector3(30f, 0f, 0f), Patch);

            Assert.IsTrue(Patch.Contains(pass));
            Assert.Greater(pass.x, 12.5f, "the far end of the patch, not its middle");
        }

        [Test]
        public void AWalkPastThePatchClipsItsNearestPart()
        {
            Vector3 pass = BushApproach.PassPoint(new Vector3(0f, 0f, 6f), new Vector3(30f, 0f, 6f), Patch);

            Assert.IsTrue(Patch.Contains(pass));
            Assert.Greater(pass.z, 0f, "the side facing the walk");
        }

        [Test]
        public void RestingSpotIsInTheBushOnTheSideItCameFrom()
        {
            Vector3 spot = BushApproach.RestSpot(new Vector3(0f, 0f, 0f), Patch);

            Assert.IsTrue(Patch.Contains(spot));
            Assert.Less(spot.x, 11f, "short of the middle, on the near side");
        }

        [Test]
        public void HidingSpotIsOnTheSideAwayFromTheThreat()
        {
            Vector3 spot = BushApproach.HideSpot(new Vector3(0f, 0f, 0f), Patch);

            Assert.IsTrue(Patch.Contains(spot));
            Assert.Greater(spot.x, 11f);
        }

        [Test]
        public void GoingInFromTheCoveredSideIsStraightThere()
        {
            // The threat is to the west; coming from the east goes in by the east side.
            BushRoute route = BushApproach.PlanHide(new Vector3(20f, 0f, 0f), new Vector3(-10f, 0f, 0f), Patch);

            Assert.IsFalse(route.HasVia);
        }

        [Test]
        public void GoingInFacingTheThreatGoesRoundToTheCoveredSide()
        {
            // The threat is to the west and so is the agent: straight in would face it.
            BushRoute route = BushApproach.PlanHide(new Vector3(5f, 0f, 0f), new Vector3(-10f, 0f, 0f), Patch);

            Assert.IsTrue(route.HasVia);
            Assert.IsFalse(Patch.Contains(route.Via), "round the outside of it");
            Assert.Greater(route.Via.x, 8f, "past the threat's side");
            Assert.IsTrue(Patch.Contains(route.Spot));
        }

        [Test]
        public void ThePatchKnowsWhereAWalkEntersAndLeaves()
        {
            Assert.IsTrue(Patch.TryChord(new Vector3(0f, 0f, 0f), new Vector3(22f, 0f, 0f), out float enters, out float leaves));
            Assert.AreEqual(8f / 22f, enters, 0.001f);
            Assert.AreEqual(14f / 22f, leaves, 0.001f);
            Assert.IsFalse(Patch.TryChord(new Vector3(0f, 0f, 5f), new Vector3(22f, 0f, 5f), out _, out _));
        }
    }
}
