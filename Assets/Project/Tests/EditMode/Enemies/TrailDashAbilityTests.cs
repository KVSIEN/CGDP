using NUnit.Framework;
using UnityEngine;
using CGD.Abilities;
using CGD.Combat;

namespace CGD.Tests
{
    // Thruster Dash: dashes through the caster's dasher the way the caster faces, with its own
    // distance, time and trail spacing, and only when the caster can dash and isn't already.
    public class TrailDashAbilityTests
    {
        private TrailDashAbility _ability;
        private GameObject       _zone;
        private GameObject       _caster;

        [SetUp]
        public void SetUp()
        {
            _zone = new GameObject("Zone", typeof(MeshRenderer), typeof(DamageZone));
            _ability = ScriptableObject.CreateInstance<TrailDashAbility>();
            _ability.ZonePrefab = _zone;
            _ability.Distance   = 7f;
            _ability.Duration   = 0.3f;
            _ability.Spacing    = 1.2f;
            _caster = new GameObject("Caster");
            _caster.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_ability);
            UnityEngine.Object.DestroyImmediate(_zone);
            UnityEngine.Object.DestroyImmediate(_caster);
        }

        [Test]
        public void NeedsACasterThatCanDashAndIsNotAlready()
        {
            Assert.IsFalse(_ability.CanExecute(new AbilityContext()));
            Assert.IsTrue(_ability.CanExecute(new AbilityContext { Dasher = new FakeDasher() }));
            Assert.IsFalse(_ability.CanExecute(new AbilityContext { Dasher = new FakeDasher { IsDashing = true } }));
        }

        [Test]
        public void DashesTheWayItFacesWithItsOwnDistanceTimeAndSpacing()
        {
            var dasher = new FakeDasher();
            _ability.Execute(new AbilityContext { Dasher = dasher, CasterTransform = _caster.transform });

            Assert.AreEqual(1, dasher.Asked);
            Assert.That(Vector3.Distance(Vector3.right, dasher.Direction), Is.LessThan(0.001f));
            Assert.AreEqual(7f, dasher.Distance);
            Assert.AreEqual(0.3f, dasher.Seconds);
            Assert.AreEqual(1.2f, dasher.Spacing);
        }
    }
}
