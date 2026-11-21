using NUnit.Framework;
using UnityEngine;
using CGD.Abilities;
using CGD.Stealth;

namespace CGD.Tests
{
    // Smoke Screen: sized by its smoke cloud, it only runs with a cloud that hides and a caster
    // that can dash, and casting it doesn't give the caster away.
    public class SmokeDashAbilityTests
    {
        private SmokeDashAbility _ability;
        private GameObject       _cloud;

        [SetUp]
        public void SetUp()
        {
            _cloud = new GameObject("Cloud");
            _cloud.AddComponent<SphereCollider>().radius = 4f;
            _cloud.AddComponent<ConcealmentZone>();
            _ability = ScriptableObject.CreateInstance<SmokeDashAbility>();
            _ability.CloudPrefab = _cloud;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ability);
            Object.DestroyImmediate(_cloud);
        }

        [Test]
        public void TheCloudRadiusIsItsColliderOnTheGround()
        {
            Assert.AreEqual(4f, _ability.CloudRadius(), 0.001f);

            _cloud.transform.localScale = new Vector3(2f, 1f, 1.5f);
            Assert.AreEqual(6f, _ability.CloudRadius(), 0.001f);
        }

        [Test]
        public void NeedsACloudThatHidesAndACasterThatCanDash()
        {
            var caster = new GameObject("Caster");
            try
            {
                var ready = new AbilityContext { Dasher = new FakeDasher(), CasterTransform = caster.transform };
                Assert.IsTrue(_ability.CanExecute(ready));
                Assert.IsFalse(_ability.CanExecute(new AbilityContext { CasterTransform = caster.transform }));
                Assert.IsFalse(_ability.CanExecute(new AbilityContext { Dasher = new FakeDasher { IsDashing = true }, CasterTransform = caster.transform }));

                Object.DestroyImmediate(_cloud.GetComponent<ConcealmentZone>());
                Assert.IsFalse(_ability.CanExecute(ready));
            }
            finally
            {
                Object.DestroyImmediate(caster);
            }
        }

        [Test]
        public void CastingItDoesNotGiveTheCasterAway() => Assert.IsFalse(_ability.GivesAway);
    }
}
