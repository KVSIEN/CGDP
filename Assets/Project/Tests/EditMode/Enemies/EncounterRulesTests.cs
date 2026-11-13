using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // The pure rules behind encounters and overseers: who gets which role, who leads and who
    // steps up, how menace rises and makes a faction back off, and how a situation moves an
    // enemy's temperament.
    public class EncounterRulesTests
    {
        private static RoleCandidate Member(float distance, bool ranged = true, float offAxis = 0.5f, float health = 1f,
                                            float shield = 0f, int tier = 1, bool cover = false, float seniority = 0f,
                                            bool lead = false, SquadRole current = SquadRole.None) =>
            new(ranged, distance, offAxis, health, shield, tier, cover, seniority, lead, current);

        private static SquadRole[] Assign(RoleCandidate[] members, RoleSlot[] slots, SquadRole fallback)
        {
            var roles = new SquadRole[members.Length];
            RoleAssignment.Assign(members, members.Length, slots, fallback, roles, new bool[members.Length]);
            return roles;
        }

        // --- Roles ---------------------------------------------------------------------

        [Test]
        public void PlainTableMatchesTheOldSquadRules()
        {
            // The plain doctrine: nearest ranged suppresses, two nearest melee go in, the rest flank.
            var slots = new[] { new RoleSlot(SquadRole.Suppress, 1, RoleFilter.Ranged), new RoleSlot(SquadRole.Assault, 2, RoleFilter.Melee) };
            var members = new[]
            {
                Member(20f), Member(8f), Member(14f),
                Member(5f, ranged: false), Member(9f, ranged: false), Member(3f, ranged: false),
            };

            CollectionAssert.AreEqual(
                new[] { SquadRole.Flank, SquadRole.Suppress, SquadRole.Flank, SquadRole.Assault, SquadRole.Flank, SquadRole.Assault },
                Assign(members, slots, SquadRole.Flank));
        }

        [Test]
        public void TheLeadTakesALeadFirstSlotEvenWhenOthersAreCloser()
        {
            var slots = new[] { new RoleSlot(SquadRole.Assault, 1, RoleFilter.Anyone, leadFirst: true) };
            var members = new[] { Member(3f), Member(20f, lead: true), Member(5f) };

            CollectionAssert.AreEqual(new[] { SquadRole.Flank, SquadRole.Assault, SquadRole.Flank },
                Assign(members, slots, SquadRole.Flank));
        }

        [Test]
        public void ACountOfZeroFillsEveryoneWhoQualifies()
        {
            var slots = new[] { new RoleSlot(SquadRole.Suppress, 0, RoleFilter.Ranged) };
            var members = new[] { Member(3f), Member(20f), Member(5f, ranged: false) };

            CollectionAssert.AreEqual(new[] { SquadRole.Suppress, SquadRole.Suppress, SquadRole.None },
                Assign(members, slots, SquadRole.None));
        }

        [Test]
        public void NoSlotsLeavesEveryoneOnTheFallback()
        {
            var members = new[] { Member(3f), Member(9f) };
            CollectionAssert.AreEqual(new[] { SquadRole.Flank, SquadRole.Flank }, Assign(members, null, SquadRole.Flank));
        }

        [Test]
        public void AFlankSlotWeightedOffAxisGoesToWhoeverIsAlreadyOnTheSide()
        {
            RoleSlot flank = System.Array.Find(Doctrine("Tech").Encounter.RolesFor(EncounterPosture.Steady), slot => slot.Role == SquadRole.Flank);
            Assume.That(flank, Is.Not.Null);

            // Tech's flank slot takes two: the two furthest round the target's side, not the one in front.
            var members = new[] { Member(10f, offAxis: 0.1f), Member(10f, offAxis: 0.9f), Member(10f, offAxis: 0.5f) };
            CollectionAssert.AreEqual(new[] { SquadRole.None, SquadRole.Flank, SquadRole.Flank },
                Assign(members, new[] { flank }, SquadRole.None));
        }

        [Test]
        public void HoldingARoleKeepsItAgainstAnEqualCandidate()
        {
            var slots = new[] { new RoleSlot(SquadRole.Suppress, 1, RoleFilter.Ranged) };
            var members = new[] { Member(10f), Member(10f, current: SquadRole.Suppress) };

            CollectionAssert.AreEqual(new[] { SquadRole.None, SquadRole.Suppress }, Assign(members, slots, SquadRole.None));
        }

        // --- Succession ---------------------------------------------------------------

        [Test]
        public void RankPicksTheHighestTierThenTheLongestServing()
        {
            var members = new[] { Member(5f, tier: 2, seniority: 3f), Member(5f, tier: 2, seniority: 9f), Member(5f, tier: 1, seniority: 30f) };
            Assert.AreEqual(1, Succession.PickLead(members, members.Length, SuccessionRule.Rank));
        }

        [Test]
        public void StrengthLetsAFreshMemberOutrankAWoundedOne()
        {
            var members = new[] { Member(5f, tier: 3, health: 0.3f), Member(5f, tier: 2, health: 1f) };
            Assert.AreEqual(1, Succession.PickLead(members, members.Length, SuccessionRule.Strength));
        }

        [Test]
        public void DepthPicksTheHighestTierThenTheMostWard()
        {
            var members = new[] { Member(5f, tier: 2, shield: 0.2f), Member(5f, tier: 2, shield: 0.9f), Member(5f, tier: 1, shield: 1f) };
            Assert.AreEqual(1, Succession.PickLead(members, members.Length, SuccessionRule.Depth));
        }

        [Test]
        public void NoMembersMeansNoLead()
        {
            Assert.AreEqual(-1, Succession.PickLead(new RoleCandidate[0], 0, SuccessionRule.Rank));
        }

        // --- Menace -------------------------------------------------------------------

        [Test]
        public void MenaceRisesWithContactAndDrainsWithout()
        {
            var gauge = new MenaceGauge(rise: 1f, decay: 0.5f, max: 30f, backsOff: false, backOffAt: 20f, backOffSeconds: 10f, resumeBelow: 8f);
            gauge.Tick(5f, inContact: 2, time: 5f);
            Assert.AreEqual(10f, gauge.Value, 0.001f);
            gauge.Tick(4f, inContact: 0, time: 9f);
            Assert.AreEqual(8f, gauge.Value, 0.001f);
        }

        [Test]
        public void MenaceCountsAtMostThreeInContactAndStopsAtItsMax()
        {
            var gauge = new MenaceGauge(1f, 0.5f, 30f, false, 20f, 10f, 8f);
            gauge.Tick(2f, inContact: 10, time: 2f);
            Assert.AreEqual(6f, gauge.Value, 0.001f);
            gauge.Tick(100f, inContact: 1, time: 102f);
            Assert.AreEqual(30f, gauge.Value, 0.001f);
        }

        [Test]
        public void PeakMenaceBacksOffForAWhileAndUntilItHasDrained()
        {
            var gauge = new MenaceGauge(1f, 1f, 30f, backsOff: true, backOffAt: 20f, backOffSeconds: 10f, resumeBelow: 8f);
            gauge.Tick(20f, inContact: 1, time: 20f);
            Assert.IsTrue(gauge.IsBackingOff);

            gauge.Tick(5f, inContact: 0, time: 25f);       // 15 left, still inside the hold
            Assert.IsTrue(gauge.IsBackingOff);
            gauge.Tick(5f, inContact: 0, time: 30f);       // hold over, but 10 is above the resume line
            Assert.IsTrue(gauge.IsBackingOff);
            gauge.Tick(3f, inContact: 0, time: 33f);       // drained to 7
            Assert.IsFalse(gauge.IsBackingOff);
        }

        [Test]
        public void AFactionThatDoesNotBackOffNeverDoes()
        {
            var gauge = new MenaceGauge(1f, 1f, 30f, backsOff: false, backOffAt: 5f, backOffSeconds: 10f, resumeBelow: 2f);
            gauge.Tick(30f, inContact: 3, time: 30f);
            Assert.IsFalse(gauge.IsBackingOff);
        }

        // --- Temperament --------------------------------------------------------------

        [Test]
        public void RegroupingMakesItWarierAndSteppingUpBolder()
        {
            var baseline = new TemperamentDoctrine();
            var calm      = EnemyTemperament.Of(baseline, regrouping: false, steppedUp: false);
            var regroup   = EnemyTemperament.Of(baseline, regrouping: true,  steppedUp: false);
            var steppedUp = EnemyTemperament.Of(baseline, regrouping: false, steppedUp: true);

            Assert.AreEqual(1f, calm.Aggression);
            Assert.AreEqual(1f, calm.Caution);
            Assert.Less(regroup.Aggression, calm.Aggression);
            Assert.Greater(regroup.Caution, calm.Caution);
            Assert.Greater(steppedUp.Aggression, calm.Aggression);
            Assert.Less(steppedUp.Caution, calm.Caution);
            Assert.AreEqual(calm.Cunning, regroup.Cunning);
        }

        private static FactionDoctrine Doctrine(string faction) =>
            UnityEditor.AssetDatabase.LoadAssetAtPath<FactionDoctrine>("Assets/Project/Data/Enemies/Doctrine/" + faction + "Doctrine.asset");
    }
}
