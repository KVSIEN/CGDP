using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    public class StunGraceTests
    {
        private GameObject _go;
        private Stunnable  _stunnable;

        [SetUp]
        public void SetUp()
        {
            _go        = new GameObject("Frozen");
            _stunnable = _go.AddComponent<Stunnable>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void ABuildupStunLandsTheFirstTime()
        {
            Assert.IsTrue(_stunnable.TryStunFromBuildup(2f, 4f));
            Assert.IsTrue(_stunnable.IsStunned);
        }

        [Test]
        public void AnotherBuildupStunIsRefusedDuringTheGrace()
        {
            _stunnable.TryStunFromBuildup(2f, 4f);

            Assert.IsFalse(_stunnable.TryStunFromBuildup(2f, 4f));
        }

        [Test]
        public void ClearingTheCharacterEndsTheGrace()
        {
            _stunnable.TryStunFromBuildup(2f, 4f);
            _stunnable.Clear();

            Assert.IsTrue(_stunnable.TryStunFromBuildup(2f, 4f));
        }

        [Test]
        public void NoGraceMeansItMayStunAgainOnceTheStunEnds()
        {
            _stunnable.TryStunFromBuildup(0f, 0f);

            Assert.IsTrue(_stunnable.TryStunFromBuildup(0f, 0f));
        }
    }
}
