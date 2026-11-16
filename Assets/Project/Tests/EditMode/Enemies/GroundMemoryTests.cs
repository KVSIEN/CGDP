using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // Enemies learning their ground: what eight rays say about a spot (GroundReading), and
    // what is remembered (PlaceMemory) — familiarity from visits, a cap, danger that fades,
    // the best place to fall back to, what is passed on to kin, and a known vantage's lane.
    public class GroundMemoryTests
    {
        private static readonly GroundDoctrine Ambusher = new(hidden: 3f, vantage: 0.5f, tight: 1f, dangerAversion: 1f, fallBack: true, share: true);
        private static readonly Vector3 Threat = new(0f, 0f, -20f);

        private static float[] Rays(params float[] reach) => reach;

        [Test]
        public void TheFirstRayRunsAlongWorldForward()
        {
            Assert.That(Vector3.Distance(Vector3.forward, GroundReading.Direction(0)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(Vector3.right, GroundReading.Direction(2)), Is.LessThan(0.001f));
        }

        [Test]
        public void OpenFloorIsGoodForNothing()
        {
            Assert.AreEqual(PlaceTraits.None, GroundReading.Classify(Rays(8, 8, 8, 8, 8, 8, 8, 8), false, out _));
        }

        [Test]
        public void WallsCloseOnBothSidesMakeATightSpot()
        {
            // Two metres to either side (rays 2 and 6), long ways along.
            PlaceTraits traits = GroundReading.Classify(Rays(10, 4, 2, 4, 10, 4, 2, 4), false, out _);
            Assert.IsTrue((traits & PlaceTraits.Tight) != 0);
        }

        [Test]
        public void ALongLaneWithCoverBesideIsAVantageLookingDownIt()
        {
            PlaceTraits traits = GroundReading.Classify(Rays(6, 6, 1f, 6, 20, 6, 6, 6), false, out int lane);
            Assert.IsTrue((traits & PlaceTraits.Vantage) != 0);
            Assert.AreEqual(4, lane);
        }

        [Test]
        public void ALongLaneInTheOpenIsNoVantage()
        {
            PlaceTraits traits = GroundReading.Classify(Rays(6, 6, 6, 6, 20, 6, 6, 6), false, out _);
            Assert.IsTrue((traits & PlaceTraits.Vantage) == 0);
        }

        [Test]
        public void StandingInConcealmentIsHidden()
        {
            Assert.AreEqual(PlaceTraits.Hidden, GroundReading.Classify(Rays(8, 8, 8, 8, 8, 8, 8, 8), true, out _));
        }

        [Test]
        public void VisitsMakeAPlaceFamiliar()
        {
            var memory = new PlaceMemory();
            var spot = new Vector3(5f, 0f, 5f);
            memory.Visit(spot, PlaceTraits.Hidden, Vector3.zero);
            memory.Visit(spot + new Vector3(0.3f, 0f, 0.3f), PlaceTraits.Hidden, Vector3.zero);   // the same cell

            Assert.AreEqual(1, memory.Count);
            Assert.AreEqual(2 * PlaceMemory.VisitGain, Familiarity(memory), 0.0001f);

            for (int i = 0; i < 10; i++) memory.Visit(spot, PlaceTraits.Hidden, Vector3.zero);
            Assert.AreEqual(1f, Familiarity(memory), 0.0001f);
        }

        [Test]
        public void ASightingIsKnownButLessTrusted()
        {
            var memory = new PlaceMemory();
            memory.Sight(Vector3.zero, PlaceTraits.Hidden);
            Assert.AreEqual(PlaceMemory.SightingGain, Familiarity(memory), 0.0001f);
        }

        [Test]
        public void AFullMemoryForgetsItsLeastFamiliarPlace()
        {
            var memory = new PlaceMemory();
            memory.Sight(new Vector3(-100f, 0f, 0f), PlaceTraits.Hidden);   // the weakest
            for (int i = 1; i < PlaceMemory.Capacity; i++)
                memory.Visit(new Vector3(i * 10f, 0f, 0f), PlaceTraits.Tight, Vector3.zero);
            Assert.AreEqual(PlaceMemory.Capacity, memory.Count);

            memory.Visit(new Vector3(0f, 0f, 500f), PlaceTraits.Tight, Vector3.zero);
            Assert.AreEqual(PlaceMemory.Capacity, memory.Count);
            var places = new List<GroundPlace>();
            memory.CopyTo(places, 0f);
            Assert.IsFalse(places.Exists(p => p.Position.x < -50f), "the least familiar place was kept");
        }

        [Test]
        public void ItFallsBackToTheMostValuableFamiliarPlaceAwayFromTheThreat()
        {
            var memory = new PlaceMemory();
            var hidden = new Vector3(0f, 0f, 10f);
            var tight  = new Vector3(5f, 0f, 10f);
            for (int i = 0; i < 4; i++) memory.Visit(hidden, PlaceTraits.Hidden, Vector3.zero);
            for (int i = 0; i < 4; i++) memory.Visit(tight, PlaceTraits.Tight, Vector3.zero);

            Assert.IsTrue(memory.TryBest(Ambusher, Vector3.zero, Threat, 0f, out Vector3 point));
            Assert.AreEqual(hidden, point);
        }

        [Test]
        public void ItNeverFallsBackTowardTheThreatOrOutOfReach()
        {
            var memory = new PlaceMemory();
            for (int i = 0; i < 4; i++)
            {
                memory.Visit(new Vector3(0f, 0f, -10f), PlaceTraits.Hidden, Vector3.zero);   // toward the threat
                memory.Visit(new Vector3(0f, 0f, 60f), PlaceTraits.Hidden, Vector3.zero);    // too far
            }
            Assert.IsFalse(memory.TryBest(Ambusher, Vector3.zero, Threat, 0f, out _));
        }

        [Test]
        public void DangerPutsItOffAndFades()
        {
            var memory = new PlaceMemory();
            var spot = new Vector3(0f, 0f, 10f);
            for (int i = 0; i < 4; i++) memory.Visit(spot, PlaceTraits.Hidden, Vector3.zero);
            memory.MarkDanger(spot, 0f);

            Assert.IsFalse(memory.TryBest(Ambusher, Vector3.zero, Threat, 0f, out _), "fresh danger");
            Assert.IsTrue(memory.TryBest(Ambusher, Vector3.zero, Threat, 10f * PlaceMemory.DangerFade, out _), "danger long faded");
        }

        [Test]
        public void KinAreToldOnlyWhatItKnowsWellAtHalfTheFamiliarity()
        {
            var scout = new PlaceMemory();
            var told  = new PlaceMemory();
            for (int i = 0; i < 4; i++) scout.Visit(new Vector3(0f, 0f, 10f), PlaceTraits.Hidden, Vector3.zero);   // well known
            scout.Sight(new Vector3(30f, 0f, 0f), PlaceTraits.Hidden);                                              // barely known

            scout.ShareWith(told, 0f);
            Assert.AreEqual(1, told.Count);
            Assert.AreEqual(1f * PlaceMemory.SharedShare, Familiarity(told), 0.0001f);
        }

        [Test]
        public void AKnownVantageGivesItsLane()
        {
            var memory = new PlaceMemory();
            memory.Visit(Vector3.zero, PlaceTraits.Vantage, Vector3.right);
            memory.Visit(new Vector3(20f, 0f, 0f), PlaceTraits.Tight, Vector3.forward);

            Assert.IsTrue(memory.TryLaneAt(new Vector3(0.4f, 0f, 0.4f), out Vector3 lane));
            Assert.AreEqual(Vector3.right, lane);
            Assert.IsFalse(memory.TryLaneAt(new Vector3(20f, 0f, 0f), out _), "a tight spot has no lane to watch");
        }

        [Test]
        public void AFactionValuesAPlaceByItsBestTrait()
        {
            Assert.AreEqual(3f, Ambusher.ValueOf(PlaceTraits.Hidden | PlaceTraits.Tight));
            Assert.AreEqual(0f, Ambusher.ValueOf(PlaceTraits.None));
        }

        private static float Familiarity(PlaceMemory memory)
        {
            var places = new List<GroundPlace>();
            memory.CopyTo(places, 0f);
            return places[0].Familiarity;
        }
    }
}
