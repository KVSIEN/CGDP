using NUnit.Framework;
using UnityEngine;
using CGD.Abilities;

namespace CGD.Tests
{
    // Venom Bite: lunges at the aim point, stopping short of it and never further than its
    // reach, and strikes once it lands.
    public class LungeStrikeAbilityTests
    {
        private LungeStrikeAbility _ability;
        private TargetedAbility    _strike;
        private GameObject         _caster;

        [SetUp]
        public void SetUp()
        {
            _strike  = ScriptableObject.CreateInstance<TargetedAbility>();
            _ability = ScriptableObject.CreateInstance<LungeStrikeAbility>();
            _ability.MaxDistance = 6f;
            _ability.StopShort   = 1.2f;
            _ability.Duration    = 0.25f;
            _ability.Strike      = _strike;
            _caster = new GameObject("Caster");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ability);
            Object.DestroyImmediate(_strike);
            Object.DestroyImmediate(_caster);
        }

        private AbilityContext Aimed(Vector3 at, FakeDasher dasher) =>
            new() { Dasher = dasher, CasterTransform = _caster.transform, HasAimPoint = true, AimPoint = at };

        [Test]
        public void NeedsAStrikeADasherAndAnAimPoint()
        {
            Assert.IsTrue(_ability.CanExecute(Aimed(Vector3.forward * 5f, new FakeDasher())));
            Assert.IsFalse(_ability.CanExecute(new AbilityContext { Dasher = new FakeDasher(), CasterTransform = _caster.transform }));
            Assert.IsFalse(_ability.CanExecute(Aimed(Vector3.forward * 5f, new FakeDasher { IsDashing = true })));

            _ability.Strike = null;
            Assert.IsFalse(_ability.CanExecute(Aimed(Vector3.forward * 5f, new FakeDasher())));
        }

        [Test]
        public void LungesAtTheAimPointStoppingShortOfIt()
        {
            var dasher = new FakeDasher();
            _ability.Execute(Aimed(new Vector3(0f, 1f, 5f), dasher));

            Assert.AreEqual(1, dasher.Asked);
            Assert.AreEqual(3.8f, dasher.Distance, 0.001f);
            Assert.AreEqual(0f, dasher.Direction.y, 0.001f, "lunges along the ground, not up at the chest it aims at");
            Assert.Greater(dasher.Direction.z, 0f);
            Assert.IsNotNull(dasher.Arrived, "strikes on arrival");
        }

        [Test]
        public void NeverLungesFurtherThanItsReach()
        {
            var dasher = new FakeDasher();
            _ability.Execute(Aimed(Vector3.forward * 20f, dasher));
            Assert.AreEqual(6f, dasher.Distance, 0.001f);
        }

        [Test]
        public void AlreadyInReachItStrikesWithoutLunging()
        {
            var dasher = new FakeDasher();
            _ability.Execute(Aimed(Vector3.forward, dasher));
            Assert.AreEqual(0, dasher.Asked);
        }
    }
}
