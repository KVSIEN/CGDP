using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    public class TacticsLogTests
    {
        [Test]
        public void ASearchRecordsWhatItWeighedAndWhatItPicked()
        {
            var search = new TacticsLog().Cover;
            Assert.IsFalse(search.HasRun);

            search.Begin(new Vector3(5f, 0f, 0f));
            search.Add(Vector3.left, 2f, CandidateVerdict.Exposed);
            int best = search.Add(Vector3.right, 1f, CandidateVerdict.Passed);
            search.Mark(best, CandidateVerdict.Picked);
            search.Pick(Vector3.right);

            Assert.IsTrue(search.HasRun);
            Assert.IsTrue(search.Succeeded);
            Assert.AreEqual(Vector3.right, search.Chosen);
            Assert.AreEqual(new Vector3(5f, 0f, 0f), search.Threat);
            Assert.AreEqual(2, search.Candidates.Count);
            Assert.AreEqual(CandidateVerdict.Picked, search.Candidates[1].Verdict);
            Assert.AreEqual(1f, search.Candidates[1].Score);
        }

        [Test]
        public void TheNextSearchReplacesTheLast()
        {
            var search = new TacticsLog().Flank;
            search.Begin(Vector3.zero);
            search.Add(Vector3.one, 1f, CandidateVerdict.Picked);
            search.Pick(Vector3.one);

            search.Begin(Vector3.zero);

            Assert.IsFalse(search.Succeeded);
            Assert.AreEqual(0, search.Candidates.Count);
        }

        [Test]
        public void ItKeepsAFixedNumberOfSpots()
        {
            var search = new TacticsLog().Vanish;
            search.Begin(Vector3.zero);
            for (int i = 0; i < TacticalDecision.MaxCandidates; i++)
                Assert.AreEqual(i, search.Add(Vector3.zero, 0f, CandidateVerdict.Seen));

            Assert.AreEqual(-1, search.Add(Vector3.zero, 0f, CandidateVerdict.Seen));
            search.Mark(-1, CandidateVerdict.Picked);   // ignored
            Assert.AreEqual(TacticalDecision.MaxCandidates, search.Candidates.Count);
        }

        [Test]
        public void EachPlanHasItsOwnLog()
        {
            var log = new TacticsLog();
            Assert.AreSame(log.Cover,  log.For(TacticalPlan.Cover));
            Assert.AreSame(log.Flank,  log.For(TacticalPlan.Flank));
            Assert.AreSame(log.Vanish, log.For(TacticalPlan.Vanish));
        }
    }
}
