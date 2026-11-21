using NUnit.Framework;
using UnityEngine;
using CGD.Player;

namespace CGD.Tests
{
    // The rule table as it stands: what takes the hands off a reload, and what weaves into a
    // melee combo. Change these together with PlayerActionRules when a rule changes.
    public class PlayerActionRulesTests
    {
        [TestCase(PlayerActionKind.Melee,   true)]
        [TestCase(PlayerActionKind.Ability, true)]
        [TestCase(PlayerActionKind.Item,    true)]
        [TestCase(PlayerActionKind.Throw,   true)]
        [TestCase(PlayerActionKind.Fire,    false)]
        [TestCase(PlayerActionKind.Reload,  false)]
        [TestCase(PlayerActionKind.Offhand, false)]
        [TestCase(PlayerActionKind.Dodge,   false)]   // a dodge interrupts through PlayerInterrupts instead
        public void CancelsReload(PlayerActionKind kind, bool cancels) =>
            Assert.That(PlayerActionRules.CancelsReload(kind), Is.EqualTo(cancels));

        [TestCase(PlayerActionKind.Ability, true)]
        [TestCase(PlayerActionKind.Item,    true)]
        [TestCase(PlayerActionKind.Throw,   true)]
        [TestCase(PlayerActionKind.Offhand, true)]
        [TestCase(PlayerActionKind.Melee,   false)]
        [TestCase(PlayerActionKind.Fire,    false)]
        [TestCase(PlayerActionKind.Reload,  false)]
        [TestCase(PlayerActionKind.Dodge,   false)]
        public void WeavesIntoCombo(PlayerActionKind kind, bool weaves) =>
            Assert.That(PlayerActionRules.WeavesIntoCombo(kind), Is.EqualTo(weaves));

        [Test]
        public void On_AddsOneChannelAndReusesIt()
        {
            var player = new GameObject("Player");
            try
            {
                PlayerActionChannel first = PlayerActionChannel.On(player);
                Assert.That(PlayerActionChannel.On(player), Is.SameAs(first));
                Assert.That(player.GetComponents<PlayerActionChannel>(), Has.Length.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Reports_ReachListeners()
        {
            var player = new GameObject("Player");
            try
            {
                PlayerActionChannel channel = PlayerActionChannel.On(player);
                PlayerActionKind? started = null, completed = null;
                channel.Started   += kind => started   = kind;
                channel.Completed += kind => completed = kind;

                channel.ReportStarted(PlayerActionKind.Item);
                channel.ReportCompleted(PlayerActionKind.Ability);

                Assert.That(started,   Is.EqualTo(PlayerActionKind.Item));
                Assert.That(completed, Is.EqualTo(PlayerActionKind.Ability));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
