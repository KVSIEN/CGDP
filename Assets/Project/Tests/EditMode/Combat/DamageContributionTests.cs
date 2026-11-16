using NUnit.Framework;
using CGD.Combat;

namespace CGD.Tests
{
    // Kill credit: each team's share of the effective health actually taken off a character.
    public class DamageContributionTests
    {
        [Test]
        public void Shares_SplitByWhatEachTeamRemoved()
        {
            var contribution = new DamageContribution();
            contribution.Record(Team.Player, 60f);
            contribution.Record(Team.Bio, 30f);
            contribution.Record(Team.None, 10f);

            Assert.That(contribution.ShareOf(Team.Player), Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(contribution.ShareOf(Team.Bio),    Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(contribution.ShareOf(Team.None),   Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(contribution.ShareOf(Team.Tech),   Is.Zero);
        }

        [Test]
        public void NothingRecorded_GivesNoShare()
        {
            Assert.That(new DamageContribution().ShareOf(Team.Player), Is.Zero);
        }

        [Test]
        public void NonPositiveAmounts_AreIgnored()
        {
            var contribution = new DamageContribution();
            contribution.Record(Team.Player, 50f);
            contribution.Record(Team.Bio, 0f);
            contribution.Record(Team.Bio, -20f);

            Assert.That(contribution.ShareOf(Team.Player), Is.EqualTo(1f));
        }

        [Test]
        public void Clear_StartsOver()
        {
            var contribution = new DamageContribution();
            contribution.Record(Team.Bio, 80f);
            contribution.Clear();
            contribution.Record(Team.Player, 20f);

            Assert.That(contribution.ShareOf(Team.Player), Is.EqualTo(1f));
            Assert.That(contribution.ShareOf(Team.Bio),    Is.Zero);
        }

        [Test]
        public void Credited_IsTheSource_OrElseTheInstigator()
        {
            var player = new DamageSource(null, Team.Player);
            var bio    = new DamageSource(null, Team.Bio);

            Assert.That(new DamageInfo(10f, source: bio, instigator: player).Credited.Team, Is.EqualTo(Team.Bio));
            Assert.That(new DamageInfo(10f, instigator: player).Credited.Team,              Is.EqualTo(Team.Player));
            Assert.That(new DamageInfo(10f).Credited.Team,                                  Is.EqualTo(Team.None));
        }

        [Test]
        public void Instigator_SurvivesRescalingAndReflection()
        {
            var info = new DamageInfo(10f, instigator: new DamageSource(null, Team.Player));

            Assert.That(info.WithDamageScale(0.5f).Instigator.Team, Is.EqualTo(Team.Player));
            Assert.That(info.AsReflected().Instigator.Team,         Is.EqualTo(Team.Player));
        }
    }
}
