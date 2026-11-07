using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    public class SightingMemoryTests
    {
        private const float Step = 0.15f;   // the default sense interval

        // Sees a target facing +z three times, moving by `move` per sighting.
        private static SightingMemory Watch(Vector3 move, Vector3 facing)
        {
            var memory = new SightingMemory();
            for (int i = 0; i < 3; i++) memory.Record(move * i, facing, i * Step);
            return memory;
        }

        [Test]
        public void OneSightingCannotTellHowItMoves()
        {
            var memory = new SightingMemory();
            memory.Record(Vector3.zero, Vector3.forward, 0f);

            Assert.AreEqual(TargetMotion.Unknown, memory.Motion);
            Assert.AreEqual(Vector3.forward, memory.Facing);
        }

        [Test]
        public void ReadsStandingStill()
        {
            Assert.AreEqual(TargetMotion.Stationary, Watch(Vector3.zero, Vector3.forward).Motion);
        }

        [Test]
        public void ReadsMovementAgainstTheFacing()
        {
            Vector3 run = Vector3.forward * 0.6f;   // 4 m/s
            Assert.AreEqual(TargetMotion.Forward,  Watch(run, Vector3.forward).Motion);
            Assert.AreEqual(TargetMotion.Sideways, Watch(run, Vector3.right).Motion);
            Assert.AreEqual(TargetMotion.Backward, Watch(run, Vector3.back).Motion);
        }

        [Test]
        public void SightingsFarApartGiveNoVelocity()
        {
            var memory = new SightingMemory();
            memory.Record(Vector3.zero, Vector3.forward, 0f);
            memory.Record(Vector3.forward * 10f, Vector3.forward, 5f);

            Assert.AreEqual(TargetMotion.Unknown, memory.Motion);
            Assert.AreEqual(Vector3.zero, memory.Velocity);
        }

        [Test]
        public void GuessesFurtherForACommittedRunner()
        {
            Vector3 run = Vector3.forward * 0.6f;
            Vector3 runnerGuess  = Watch(run, Vector3.forward).Predict(1f, 0.5f, 0.25f);
            Vector3 strafeGuess  = Watch(run, Vector3.right).Predict(1f, 0.5f, 0.25f);
            Vector3 backingGuess = Watch(run, Vector3.back).Predict(1f, 0.5f, 0.25f);
            Vector3 lastSeen     = run * 2f;

            Assert.AreEqual(4f, (runnerGuess  - lastSeen).magnitude, 0.01f);
            Assert.AreEqual(2f, (strafeGuess  - lastSeen).magnitude, 0.01f);
            Assert.AreEqual(1f, (backingGuess - lastSeen).magnitude, 0.01f);
        }

        [Test]
        public void ExpectsAStillTargetToStayPut()
        {
            SightingMemory memory = Watch(Vector3.zero, Vector3.forward);
            Assert.AreEqual(Vector3.zero, memory.Predict(1f, 1f, 1f));
            Assert.AreEqual(Vector3.zero, memory.Heading);
        }

        [Test]
        public void ForgetClearsEverything()
        {
            SightingMemory memory = Watch(Vector3.forward * 0.6f, Vector3.forward);
            memory.Forget();

            Assert.AreEqual(TargetMotion.Unknown, memory.Motion);
            Assert.AreEqual(Vector3.zero, memory.Facing);
            Assert.AreEqual(Vector3.zero, memory.Velocity);
        }
    }
}
