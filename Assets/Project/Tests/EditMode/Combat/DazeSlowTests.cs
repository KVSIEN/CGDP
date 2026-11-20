using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    public class DazeSlowTests
    {
        private GameObject _go;
        private Stunnable  _stunnable;

        [SetUp]
        public void SetUp()
        {
            _go        = new GameObject("Dazed");
            _stunnable = _go.AddComponent<Stunnable>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void FadingSlowStartsAtItsMultiplier()
        {
            _stunnable.SetFadingSlow(this, 0.25f, 3f);

            Assert.AreEqual(0.25f, _stunnable.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void FadingSlowNeverWeakensAStrongerSlow()
        {
            _stunnable.SetSlow("ice", 0.1f);
            _stunnable.SetFadingSlow(this, 0.5f, 3f);

            Assert.AreEqual(0.1f, _stunnable.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void FadingSlowWithNoDurationIsIgnored()
        {
            _stunnable.SetFadingSlow(this, 0.25f, 0f);

            Assert.AreEqual(1f, _stunnable.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void ClearingEverythingEndsAFadingSlow()
        {
            _stunnable.SetFadingSlow(this, 0.25f, 3f);
            _stunnable.Clear();

            Assert.AreEqual(1f, _stunnable.SpeedMultiplier, 0.0001f);
        }
    }
}
