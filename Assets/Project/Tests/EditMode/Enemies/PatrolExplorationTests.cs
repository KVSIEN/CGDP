using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    public class PatrolExplorationTests
    {
        private static readonly PatrolDoctrine Patrol = new(true, 0.25f, 20f, 0.5f, 100f);
        private static readonly GroundDoctrine Ground = new(1f, 1f, 1f, 1f, false, true);

        [Test]
        public void GroundNeverWalkedIsStaleAndGroundJustWalkedIsNot()
        {
            var coverage = new PatrolCoverage();
            var spot = new Vector3(10f, 0f, 10f);
            Assert.AreEqual(1f, coverage.Staleness(spot, 5f, 100f));

            coverage.Visit(spot, 5f);
            Assert.AreEqual(0f, coverage.Staleness(spot, 5f, 100f), 0.001f);
            Assert.AreEqual(0.5f, coverage.Staleness(spot, 55f, 100f), 0.001f);
            Assert.AreEqual(1f, coverage.Staleness(spot, 500f, 100f));
        }

        [Test]
        public void SharingKeepsTheLaterVisit()
        {
            var a = new PatrolCoverage();
            var b = new PatrolCoverage();
            var spot = new Vector3(0f, 0f, 0f);
            a.Visit(spot, 10f);
            b.Visit(spot, 50f);

            a.ShareWith(b);
            Assert.AreEqual(0f, b.Staleness(spot, 50f, 100f), 0.001f);   // b keeps its own, later visit

            b.ShareWith(a);
            Assert.AreEqual(0f, a.Staleness(spot, 50f, 100f), 0.001f);   // a learns b's
        }

        [Test]
        public void ItForgetsTheOldestCellsOnceFull()
        {
            var coverage = new PatrolCoverage();
            for (int i = 0; i < PatrolCoverage.Capacity + 10; i++)
                coverage.Visit(new Vector3(i * PatrolCoverage.CellSize, 0f, 0f), i);
            Assert.AreEqual(PatrolCoverage.Capacity, coverage.Count);
            Assert.AreEqual(1f, coverage.Staleness(Vector3.zero, 0f, 5000f));   // the oldest was the first to go
        }

        [Test]
        public void UnseenGroundBeatsGroundJustWalked()
        {
            float unseen = PatrolScoring.Score(1f, 0f, 0f, 0.5f, Patrol, Ground);
            float walked = PatrolScoring.Score(0.05f, 0f, 0f, 0.5f, Patrol, Ground);
            Assert.Greater(unseen, walked);
        }

        [Test]
        public void AStaleFavouritePlaceBeatsEqualUnexploredGround()
        {
            float favourite = PatrolScoring.Score(1f, 1f, 0f, 0.5f, Patrol, Ground);
            float unknown   = PatrolScoring.Score(1f, 0f, 0f, 0.5f, Patrol, Ground);
            Assert.Greater(favourite, unknown);
        }

        [Test]
        public void FreshDangerPutsItOffAndNearerIsPreferred()
        {
            float safe     = PatrolScoring.Score(1f, 0f, 0f, 0.5f, Patrol, Ground);
            float bloodied = PatrolScoring.Score(1f, 0f, 1f, 0.5f, Patrol, Ground);
            Assert.Greater(safe, bloodied);
            Assert.Greater(PatrolScoring.Score(1f, 0f, 0f, 0.1f, Patrol, Ground), PatrolScoring.Score(1f, 0f, 0f, 0.9f, Patrol, Ground));
        }

        [Test]
        public void DangerFadesTowardsTheEdgeOfItsReach()
        {
            var places = new List<GroundPlace> { new(Vector3.zero, PlaceTraits.None, 0f, 1f) };
            Assert.AreEqual(1f, PatrolScoring.DangerNear(Vector3.zero, places), 0.001f);
            Assert.Less(PatrolScoring.DangerNear(new Vector3(PatrolScoring.DangerReach * 0.5f, 0f, 0f), places), 1f);
            Assert.AreEqual(0f, PatrolScoring.DangerNear(new Vector3(PatrolScoring.DangerReach + 1f, 0f, 0f), places));
        }
    }
}
