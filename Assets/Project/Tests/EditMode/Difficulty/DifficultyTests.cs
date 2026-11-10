using NUnit.Framework;
using UnityEngine;
using CGD.Difficulty;
using CGD.Settings;

namespace CGD.Tests
{
    public class DifficultyTests
    {
        private DifficultyTable _table;

        [SetUp]
        public void CreateTable() => _table = ScriptableObject.CreateInstance<DifficultyTable>();

        [TearDown]
        public void Clean()
        {
            GameDifficulty.Table = null;
            Object.DestroyImmediate(_table);
        }

        [Test]
        public void NormalPlaysAsBalanced()
        {
            DifficultyTuning normal = _table.For(DifficultyLevel.Normal);

            Assert.AreEqual(1f, normal.EnemyHealth);
            Assert.AreEqual(1f, normal.DamageTaken);
        }

        [Test]
        public void EasyIsGentlerAndHardIsHarsherThanNormal()
        {
            DifficultyTuning easy = _table.For(DifficultyLevel.Easy);
            DifficultyTuning hard = _table.For(DifficultyLevel.Hard);

            Assert.Less(easy.EnemyHealth, 1f);
            Assert.Less(easy.DamageTaken, 1f);
            Assert.Greater(hard.EnemyHealth, 1f);
            Assert.Greater(hard.DamageTaken, 1f);
        }

        [Test]
        public void WithoutATableEveryLevelPlaysAsBalanced()
        {
            GameDifficulty.Table = null;

            foreach (DifficultyLevel level in System.Enum.GetValues(typeof(DifficultyLevel)))
                Assert.AreSame(DifficultyTuning.None, GameDifficulty.For(level));
        }

        [Test]
        public void TheRegisteredTableAnswersForEveryLevel()
        {
            GameDifficulty.Table = _table;

            Assert.AreSame(_table.For(DifficultyLevel.Hard), GameDifficulty.For(DifficultyLevel.Hard));
        }

        [Test]
        public void TuningNeverDropsBelowItsFloor()
        {
            var tuning = new DifficultyTuning(0f, -3f);

            Assert.GreaterOrEqual(tuning.EnemyHealth, 0.1f);
            Assert.GreaterOrEqual(tuning.DamageTaken, 0.1f);
        }

        [Test]
        public void NewSettingsStartOnNormal() =>
            Assert.AreEqual(DifficultyLevel.Normal, new SettingsData().Difficulty);

        [Test]
        public void LevelsKeepTheirSerializedValues()
        {
            Assert.AreEqual(0, (int)DifficultyLevel.Easy);
            Assert.AreEqual(1, (int)DifficultyLevel.Normal);
            Assert.AreEqual(2, (int)DifficultyLevel.Hard);
        }
    }
}
