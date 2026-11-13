using NUnit.Framework;
using UnityEngine;
using CGD.Abilities;

namespace CGD.Tests
{
    // Summon: calls up minions through the caster's summoner, never past its cap.
    public class SummonAbilityTests
    {
        private sealed class FakeSummoner : IMinionSummoner
        {
            public int Calls, LastCount;
            public int Alive { get; set; }

            public void Summon(GameObject prefab, int count, Vector3 around, float lifetime)
            {
                Calls++;
                LastCount = count;
                Alive += count;
            }
        }

        private SummonAbility _ability;
        private GameObject    _minion;
        private GameObject    _caster;

        [SetUp]
        public void SetUp()
        {
            _ability = ScriptableObject.CreateInstance<SummonAbility>();
            _minion  = new GameObject("Minion");
            _caster  = new GameObject("Caster");
            _ability.MinionPrefab = _minion;
            _ability.Count    = 3;
            _ability.MaxAlive = 4;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ability);
            Object.DestroyImmediate(_minion);
            Object.DestroyImmediate(_caster);
        }

        [Test]
        public void SummonsItsCountThroughTheSummoner()
        {
            var summoner = new FakeSummoner();
            var context  = new AbilityContext { CasterTransform = _caster.transform, Summoner = summoner };

            Assert.IsTrue(_ability.CanExecute(context));
            _ability.Execute(context);

            Assert.AreEqual(1, summoner.Calls);
            Assert.AreEqual(3, summoner.LastCount);
        }

        [Test]
        public void StopsAtItsCap()
        {
            var context = new AbilityContext { CasterTransform = _caster.transform, Summoner = new FakeSummoner { Alive = 4 } };

            Assert.IsFalse(_ability.CanExecute(context));
        }

        [Test]
        public void NeedsASummonerAndAMinion()
        {
            Assert.IsFalse(_ability.CanExecute(new AbilityContext { CasterTransform = _caster.transform }), "the player has no summoner");

            _ability.MinionPrefab = null;
            Assert.IsFalse(_ability.CanExecute(new AbilityContext { CasterTransform = _caster.transform, Summoner = new FakeSummoner() }));
        }
    }
}
