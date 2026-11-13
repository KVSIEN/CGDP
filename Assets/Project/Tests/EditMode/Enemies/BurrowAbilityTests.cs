using NUnit.Framework;
using UnityEngine;
using CGD.Abilities;

namespace CGD.Tests
{
    // Burrow: surfaces beside the target through the caster's relocator, or not at all.
    public class BurrowAbilityTests
    {
        private sealed class FakeRelocator : IUnseenRelocator
        {
            public bool  Allows = true;
            public float AskedMax, AskedWithin;
            public int   Asked;

            public bool CanRelocateUnseen(float maxDistance) => true;
            public bool TryRelocateUnseen(float maxDistance) => true;

            public bool TryRelocateNearTarget(float maxDistance, float within)
            {
                Asked++;
                AskedMax    = maxDistance;
                AskedWithin = within;
                return Allows;
            }
        }

        private BurrowAbility _ability;
        private GameObject    _caster;

        [SetUp]
        public void SetUp()
        {
            _ability = ScriptableObject.CreateInstance<BurrowAbility>();
            _ability.MaxDistance   = 25f;
            _ability.SurfaceWithin = 2f;
            _caster = new GameObject("Caster");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ability);
            Object.DestroyImmediate(_caster);
        }

        [Test]
        public void NeedsACasterThatCanRelocate()
        {
            Assert.IsFalse(_ability.CanExecute(new AbilityContext { CasterTransform = _caster.transform }));
            Assert.IsTrue(_ability.CanExecute(new AbilityContext { CasterTransform = _caster.transform, Relocator = new FakeRelocator() }));
        }

        [Test]
        public void SurfacesBesideTheTargetWithinItsReach()
        {
            var relocator = new FakeRelocator();
            _ability.Execute(new AbilityContext { CasterTransform = _caster.transform, Relocator = relocator });

            Assert.AreEqual(1, relocator.Asked);
            Assert.AreEqual(25f, relocator.AskedMax);
            Assert.AreEqual(2f, relocator.AskedWithin);
        }
    }
}
