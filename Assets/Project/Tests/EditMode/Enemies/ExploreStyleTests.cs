using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    // Each faction explores its own way: TECH a little at a time, VOID the outskirts and walls,
    // BIO the open middle and round concealment.
    public class ExploreStyleTests
    {
        private const float Far = float.PositiveInfinity;

        [Test]
        public void ExpandingPrefersTheNextStopCloseToItself()
        {
            float near = PatrolScoring.StyleBonus(ExploreStyle.Expand, 0.5f, 0.1f, Far, Far);
            float far  = PatrolScoring.StyleBonus(ExploreStyle.Expand, 0.5f, 0.9f, Far, Far);
            Assert.Greater(near, far);
        }

        [Test]
        public void ExpandingRangeGrowsWithWhatItHasCovered()
        {
            float fresh   = PatrolScoring.ExpandedRange(20f, 0);
            float learned = PatrolScoring.ExpandedRange(20f, 10);
            Assert.AreEqual(PatrolScoring.ExpandStart, fresh, 0.001f);
            Assert.Greater(learned, fresh);
            Assert.AreEqual(20f, PatrolScoring.ExpandedRange(20f, 1000), 0.001f, "never beyond its full range");
        }

        [Test]
        public void OutskirtsPreferTheEdgeOfItsRangeAndTheWalls()
        {
            float edgeByWall   = PatrolScoring.StyleBonus(ExploreStyle.Outskirts, 0.9f, 0.5f, 0.5f, Far);
            float edgeInOpen   = PatrolScoring.StyleBonus(ExploreStyle.Outskirts, 0.9f, 0.5f, 6f, Far);
            float middleInOpen = PatrolScoring.StyleBonus(ExploreStyle.Outskirts, 0.1f, 0.5f, 6f, Far);
            Assert.Greater(edgeByWall, edgeInOpen);
            Assert.Greater(edgeInOpen, middleInOpen);
        }

        [Test]
        public void InsidePrefersTheOpenMiddleAndConcealment()
        {
            float byBush      = PatrolScoring.StyleBonus(ExploreStyle.Inside, 0.3f, 0.5f, 5f, 1f);
            float openMiddle  = PatrolScoring.StyleBonus(ExploreStyle.Inside, 0.3f, 0.5f, 5f, Far);
            float edgeByWall  = PatrolScoring.StyleBonus(ExploreStyle.Inside, 0.9f, 0.5f, 0.5f, Far);
            Assert.Greater(byBush, openMiddle);
            Assert.Greater(openMiddle, edgeByWall);
        }

        [Test]
        public void RoamingHasNoTaste()
        {
            Assert.AreEqual(0f, PatrolScoring.StyleBonus(ExploreStyle.Roam, 0.9f, 0.9f, 0.5f, 1f));
        }
    }
}
