using NUnit.Framework;
using UnityEngine;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.Tests
{
    public class WallAndInterestTests
    {
        private static readonly Vector3[] Corners = { new(0f, 0f, 0f), new(10f, 0f, 0f), new(10f, 0f, 10f) };

        [Test]
        public void RouteLengthAndPointsAlongItFollowTheCorners()
        {
            Assert.AreEqual(20f, WallHug.Length(Corners, 3), 0.001f);
            Assert.AreEqual(new Vector3(6f, 0f, 0f),  WallHug.PointAlong(Corners, 3, 6f));
            Assert.AreEqual(new Vector3(10f, 0f, 4f), WallHug.PointAlong(Corners, 3, 14f));
            Assert.AreEqual(new Vector3(10f, 0f, 10f), WallHug.PointAlong(Corners, 3, 99f));
        }

        [Test]
        public void ALegIsPulledToJustInsideTheWallOnTheOpenSide()
        {
            // Wall edge at x = 0; the open floor (where the route is) is on the +x side.
            Vector3 beside = WallHug.BesideWall(new Vector3(0f, 0f, 5f), new Vector3(3f, 0f, 5f), 0.8f);
            Assert.AreEqual(new Vector3(0.8f, 0f, 5f), beside);
        }

        [Test]
        public void ThePointsUsersStandAtAreSpreadRoundTheCentre()
        {
            Vector3 centre = new(5f, 0f, 5f);
            Assert.AreEqual(centre, InterestRules.SpotAround(centre, 0, 1, 2f));
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(2f, Vector3.Distance(centre, InterestRules.SpotAround(centre, i, 3, 2f)), 0.001f);
            Assert.AreNotEqual(InterestRules.SpotAround(centre, 0, 3, 2f), InterestRules.SpotAround(centre, 1, 3, 2f));
        }

        [Test]
        public void WantingAPointDependsOnLikingNearnessAndSafety()
        {
            Assert.AreEqual(0f, InterestRules.Weight(0f, 5f, 30f, 0f));                       // does not like it
            Assert.AreEqual(0f, InterestRules.Weight(1f, 31f, 30f, 0f));                      // out of reach
            Assert.Greater(InterestRules.Weight(1f, 5f, 30f, 0f), InterestRules.Weight(1f, 25f, 30f, 0f));
            Assert.Greater(InterestRules.Weight(0.8f, 5f, 30f, 0f), InterestRules.Weight(0.4f, 5f, 30f, 0f));
            Assert.AreEqual(0f, InterestRules.Weight(1f, 5f, 30f, 1f));                       // blood spilt right there
        }

        [Test]
        public void AKindDrawsOnlyTheFactionsItListsAndTheyCanDiffer()
        {
            var kind = ScriptableObject.CreateInstance<InterestKind>();
            var field = typeof(InterestKind).GetField("_draws", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(kind, new[] { new InterestDraw { Team = Team.Bio, Appeal = 0.8f }, new InterestDraw { Team = Team.Tech, Appeal = 2f } });

            Assert.AreEqual(0.8f, kind.AppealTo(Team.Bio), 0.001f);
            Assert.AreEqual(1f,   kind.AppealTo(Team.Tech), 0.001f);   // clamped
            Assert.AreEqual(0f,   kind.AppealTo(Team.Void));
            Object.DestroyImmediate(kind);
        }
    }
}
