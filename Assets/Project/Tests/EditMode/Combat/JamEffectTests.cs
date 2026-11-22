using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    // A Jam is found by its kind, whichever asset it is, and a cleanse ends it.
    public class JamEffectTests
    {
        private GameObject _go;
        private StatusEffectController _status;
        private JamEffect _jam;

        [SetUp]
        public void SetUp()
        {
            _go        = new GameObject("Target");
            _status    = _go.AddComponent<StatusEffectController>();
            _jam       = ScriptableObject.CreateInstance<JamEffect>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_jam);
        }

        [Test]
        public void AJamIsFoundByItsKind()
        {
            Assert.IsFalse(_status.Any<JamEffect>());
            _status.Apply(_jam, new DamageInfo(0f));
            Assert.IsTrue(_status.Any<JamEffect>());
            Assert.IsFalse(_status.Any<StunEffect>());
        }

        [Test]
        public void ClearingEndsAJam()
        {
            _status.Apply(_jam, new DamageInfo(0f));
            _status.Clear();
            Assert.IsFalse(_status.Any<JamEffect>());
            Assert.IsFalse(_status.HasAny);
        }
    }
}
