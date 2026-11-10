using System.IO;
using NUnit.Framework;
using CGD.Expedition;
using CGD.Saving;
using CGD.Telemetry;

namespace CGD.Tests
{
    public class TelemetryTests
    {
        private string _directory;

        [SetUp]
        public void PickDirectory() =>
            _directory = Path.Combine(Path.GetTempPath(), "cgd-telemetry-tests-" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void Clean()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }

        private TelemetryStore NewStore() => new(new SaveFile(Path.Combine(_directory, "telemetry.json")));

        [Test]
        public void HitsAddUpDamageAndCriticals()
        {
            var run = new RunStats();
            run.RecordDealt(40f, critical: false, killed: false, tier: 1);
            run.RecordDealt(60f, critical: true,  killed: false, tier: 1);

            Assert.AreEqual(100f, run.DamageDealt);
            Assert.AreEqual(2, run.Hits);
            Assert.AreEqual(0.5f, run.CriticalRate);
        }

        [Test]
        public void KillsAreSplitByTier()
        {
            var run = new RunStats();
            run.RecordDealt(10f, false, killed: true, tier: 1);
            run.RecordDealt(10f, false, killed: true, tier: 3);
            run.RecordDealt(10f, false, killed: true, tier: 3);
            run.RecordDealt(10f, false, killed: true, tier: 0);   // not an enemy

            Assert.AreEqual(4, run.Kills);
            Assert.AreEqual(1, run.KillsOfTier(1));
            Assert.AreEqual(0, run.KillsOfTier(2));
            Assert.AreEqual(2, run.KillsOfTier(3));
            Assert.AreEqual(1, run.KillsOther);
        }

        [Test]
        public void NothingNegativeOrEmptyIsCounted()
        {
            var run = new RunStats();
            run.RecordDealt(0f, true, killed: false, tier: 1);
            run.RecordTaken(-5f);
            run.RecordCollected(0);
            run.AddTime(-1f);

            Assert.AreEqual(0, run.Hits);
            Assert.AreEqual(0, run.CriticalHits);
            Assert.AreEqual(0f, run.DamageTaken);
            Assert.AreEqual(0, run.ItemsCollected);
            Assert.AreEqual(0f, run.Seconds);
        }

        [TestCase(0f,    "0:00")]
        [TestCase(75f,   "1:15")]
        [TestCase(3725f, "1:02:05")]
        public void DurationReadsAsClockTime(float seconds, string expected) =>
            Assert.AreEqual(expected, RunStats.FormatDuration(seconds));

        [Test]
        public void LifetimeCountsRunsByOutcomeAndKeepsBests()
        {
            var lifetime = new LifetimeStats();

            var good = new RunStats();
            good.RecordDealt(50f, false, killed: true, tier: 2);
            good.RecordDealt(50f, false, killed: true, tier: 2);
            good.AddTime(300f);
            lifetime.Add(good, RunOutcome.Extracted);

            var bad = new RunStats();
            bad.RecordDealt(10f, false, killed: true, tier: 1);
            bad.RecordTaken(100f);
            bad.AddTime(60f);
            lifetime.Add(bad, RunOutcome.Died);
            lifetime.Add(new RunStats(), RunOutcome.EmergencyExtracted);

            Assert.AreEqual(3, lifetime.runs);
            Assert.AreEqual(1, lifetime.extractions);
            Assert.AreEqual(1, lifetime.emergencyExtractions);
            Assert.AreEqual(1, lifetime.deaths);
            Assert.AreEqual(3, lifetime.kills);
            Assert.AreEqual(110f, lifetime.damageDealt);
            Assert.AreEqual(2, lifetime.bestKillsInRun);
            Assert.AreEqual(300f, lifetime.longestRunSeconds);
            Assert.AreEqual(2f / 3f, lifetime.SurvivalRate, 0.0001f);
        }

        [Test]
        public void StoreKeepsTotalsBetweenSessions()
        {
            var first = NewStore();
            first.Load();
            var run = new RunStats();
            run.RecordDealt(25f, false, killed: true, tier: 1);
            Assert.IsTrue(first.Record(run, RunOutcome.Extracted));

            var second = NewStore();
            second.Load();
            Assert.AreEqual(1, second.Lifetime.runs);
            Assert.AreEqual(1, second.Lifetime.kills);

            second.Record(run, RunOutcome.Died);
            var third = NewStore();
            third.Load();
            Assert.AreEqual(2, third.Lifetime.runs);
            Assert.AreEqual(1, third.Lifetime.deaths);
        }

        [Test]
        public void UnreadableSaveStartsFreshAndIsSetAside()
        {
            Directory.CreateDirectory(_directory);
            string path = Path.Combine(_directory, "telemetry.json");
            File.WriteAllText(path, "{ not json");

            var store = NewStore();
            store.Load();

            Assert.AreEqual(0, store.Lifetime.runs);
            Assert.IsFalse(File.Exists(path));
            Assert.IsTrue(File.Exists(path + ".unreadable"));
        }

        [Test]
        public void SummaryNamesDurationKillsAndDamage()
        {
            var run = new RunStats();
            run.RecordDealt(120.4f, false, killed: true, tier: 2);
            run.RecordTaken(30f);
            run.AddTime(95f);

            Assert.AreEqual("1:35 · 1 kill (T1 0 T2 1 T3 0) · 120 dealt · 30 taken", RunStatsText.Summary(run));
        }
    }
}
