using NUnit.Framework;
using CGD.Enemies;
using State = CGD.Enemies.EnemyAI.AiState;

namespace CGD.Tests
{
    public class EnemyVoiceTests
    {
        [TestCase(State.Patrol,      State.Chase,       BarkKind.Spotted)]
        [TestCase(State.Investigate, State.Chase,       BarkKind.Spotted)]
        [TestCase(State.Patrol,      State.Investigate, BarkKind.Investigating)]
        [TestCase(State.Chase,       State.Hunt,        BarkKind.LostTarget)]
        [TestCase(State.Investigate, State.Hunt,        BarkKind.Hunting)]
        [TestCase(State.Chase,       State.Retreat,     BarkKind.FallingBack)]
        [TestCase(State.Chase,       State.RaiseAlarm,  BarkKind.RaisingAlarm)]
        [TestCase(State.Hunt,        State.Patrol,      BarkKind.None)]
        [TestCase(State.Chase,       State.Chase,       BarkKind.None)]
        [TestCase(State.Return,      State.Return,      BarkKind.None)]
        [TestCase(State.Chase,       State.Investigate, BarkKind.None)]
        public void SaysWhatFitsTheChange(State from, State to, BarkKind expected)
        {
            Assert.AreEqual(expected, EnemyVoice.ForChange(from, to, returnIsVictory: false));
        }

        [Test]
        public void ReturningAfterAKillIsAVictory()
        {
            Assert.AreEqual(BarkKind.Victory,      EnemyVoice.ForChange(State.Chase, State.Return, returnIsVictory: true));
            Assert.AreEqual(BarkKind.NothingFound, EnemyVoice.ForChange(State.Hunt,  State.Return, returnIsVictory: false));
        }
    }
}
