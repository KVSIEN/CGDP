using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    public class BodyLanguageTests
    {
        // The observer stands at +z from the target, so "toObserver" is +z throughout.
        private static readonly Vector3 ToObserver = Vector3.forward * 10f;

        [Test]
        public void FacingTheObserverIsNotAnOpening()
        {
            var body = new BodyLanguage();
            body.Read(Vector3.forward, ToObserver, reloading: false, now: 0f);

            Assert.IsFalse(body.IsExposed);
        }

        [Test]
        public void ABackTurnedOrAReloadIsAnOpening()
        {
            var body = new BodyLanguage();
            body.Read(Vector3.back, ToObserver, reloading: false, now: 0f);
            Assert.IsTrue(body.IsExposed, "back turned");

            body.Read(Vector3.forward, ToObserver, reloading: true, now: 0.1f);
            Assert.IsTrue(body.IsExposed, "reloading while facing it");
        }

        [Test]
        public void AStareBuildsWhileHeldAndBreaksWhenItLooksAway()
        {
            var body = new BodyLanguage();
            for (int i = 0; i <= 10; i++) body.Read(Vector3.forward, ToObserver, false, i * 0.2f);
            Assert.AreEqual(2f, body.Watching, 0.001f);

            body.Read(Vector3.right, ToObserver, false, 2.2f);
            Assert.AreEqual(0f, body.Watching);
        }

        [Test]
        public void LosingTheTargetBreaksTheStare()
        {
            var body = new BodyLanguage();
            body.Read(Vector3.forward, ToObserver, false, 0f);
            body.Read(Vector3.forward, ToObserver, false, 1f);
            body.Lose();
            body.Read(Vector3.forward, ToObserver, false, 5f);

            Assert.AreEqual(0f, body.Watching, "the unseen seconds must not count as staring");
        }

        [Test]
        public void WhippingRoundIsAFlinchThatFades()
        {
            var body = new BodyLanguage();
            body.Read(Vector3.right, ToObserver, false, 0f);
            body.Read(Vector3.left, ToObserver, false, 0.5f);

            Assert.IsTrue(body.FlinchedAt(0.6f));
            Assert.IsFalse(body.FlinchedAt(0.5f + BodyLanguage.FlinchWindow + 0.01f));
        }

        [Test]
        public void ASteadyTurnIsNotAFlinch()
        {
            var body = new BodyLanguage();
            Vector3 facing = Vector3.right;
            for (int i = 0; i <= 8; i++)
            {
                body.Read(facing, ToObserver, false, i * 0.5f);
                facing = Quaternion.Euler(0f, 30f, 0f) * facing;   // 60°/s
            }

            Assert.IsFalse(body.FlinchedAt(4f));
        }

        [Test]
        public void ForgettingClearsAFlinch()
        {
            var body = new BodyLanguage();
            body.NoteFlinch(0f);
            body.Forget();

            Assert.IsFalse(body.FlinchedAt(0.1f));
        }
    }
}
