using System;
using NUnit.Framework;
using UnityEngine;
using CGD.Abilities;
using CGD.Combat;

namespace CGD.Tests
{
    // Thruster Dash: dashes through the caster's dasher, with its own distance, time and trail
    // spacing, and only when the caster can dash and isn't already.
    public class TrailDashAbilityTests
    {
        private sealed class FakeDasher : IDasher
        {
            public bool  IsDashing { get; set; }
            public int   Asked;
            public float Distance, Seconds, Spacing;

            public void Begin(float distance, float seconds, float spacing, Action<Vector3> passed)
            {
                Asked++;
                Distance = distance;
                Seconds  = seconds;
                Spacing  = spacing;
            }
        }

        private TrailDashAbility _ability;
        private GameObject       _zone;

        [SetUp]
        public void SetUp()
        {
            _zone = new GameObject("Zone", typeof(MeshRenderer), typeof(DamageZone));
            _ability = ScriptableObject.CreateInstance<TrailDashAbility>();
            _ability.ZonePrefab = _zone;
            _ability.Distance   = 7f;
            _ability.Duration   = 0.3f;
            _ability.Spacing    = 1.2f;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_ability);
            UnityEngine.Object.DestroyImmediate(_zone);
        }

        [Test]
        public void NeedsACasterThatCanDashAndIsNotAlready()
        {
            Assert.IsFalse(_ability.CanExecute(new AbilityContext()));
            Assert.IsTrue(_ability.CanExecute(new AbilityContext { Dasher = new FakeDasher() }));
            Assert.IsFalse(_ability.CanExecute(new AbilityContext { Dasher = new FakeDasher { IsDashing = true } }));
        }

        [Test]
        public void DashesItsOwnDistanceTimeAndSpacing()
        {
            var dasher = new FakeDasher();
            _ability.Execute(new AbilityContext { Dasher = dasher });

            Assert.AreEqual(1, dasher.Asked);
            Assert.AreEqual(7f, dasher.Distance);
            Assert.AreEqual(0.3f, dasher.Seconds);
            Assert.AreEqual(1.2f, dasher.Spacing);
        }
    }
}
