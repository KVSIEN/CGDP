using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Editor;
using CGD.Enemies;

namespace CGD.Tests
{
    // The numbers and checks the enemy design tools (EnemyData inspector, Enemy Roster) show.
    public class EnemyDataReviewTests
    {
        private EnemyData  _data;
        private BtTestTree _tree;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<EnemyData>();
            _tree = new BtTestTree();
            _data.BehaviorTree = _tree.Asset;
        }

        [TearDown]
        public void TearDown()
        {
            _tree.Dispose();
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void MeleeDamagePerSecondCountsWindupAndCooldown()
        {
            _data.AttackDamage   = 10f;
            _data.AttackWindup   = 0.5f;
            _data.AttackCooldown = 1.5f;

            Assert.AreEqual(5f, _data.MeleeDamagePerSecond, 0.001f);
        }

        [Test]
        public void RangedDamagePerSecondIsOneBurstPerCooldown()
        {
            _data.AttackDamage         = 9f;   // RangedAttackDamage 0 = the attack damage
            _data.BurstCount           = 3;
            _data.BurstInterval        = 0.1f;
            _data.RangedAttackCooldown = 1.5f;

            Assert.AreEqual(18f, _data.RangedDamagePerSecond, 0.001f);
        }

        [Test]
        public void ABurstLongerThanItsCooldownSetsThePace()
        {
            _data.RangedAttackDamage   = 10f;
            _data.BurstCount           = 5;
            _data.BurstInterval        = 1f;
            _data.RangedAttackCooldown = 2f;

            Assert.AreEqual(12.5f, _data.RangedDamagePerSecond, 0.001f);
        }

        [Test]
        public void NoTreeIsAnError()
        {
            _data.BehaviorTree = null;

            List<EnemyDataReview.Issue> issues = EnemyDataReview.Check(_data);
            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(BtIssueSeverity.Error, issues[0].Severity);
        }

        [Test]
        public void ARangedEnemyWhoseTreeNeverShootsIsFlagged()
        {
            _data.CombatType = EnemyCombatType.Ranged;
            _tree.Add(new MeleeAttack(), _tree.Root);

            Assert.IsTrue(HasWarning("never shoots"));
        }

        [Test]
        public void ShootingInsideASubtreeCounts()
        {
            _data.CombatType = EnemyCombatType.Ranged;
            using var shared = new BtTestTree();
            shared.Add(new ShootBurst(), shared.Root);
            _tree.Add(BtTestTree.Set(new Subtree(), "_tree", shared.Asset), _tree.Root);

            Assert.IsFalse(HasWarning("never shoots"));
        }

        [Test]
        public void ASwitchedOffShootingBehaviorDoesNotCount()
        {
            _data.CombatType = EnemyCombatType.Ranged;
            using var shooting = new BtTestTree();
            shooting.Add(new ShootBurst(), shooting.Root);
            Sequence fight = _tree.Add(new Sequence(), _tree.Root);
            _tree.Add(new MeleeAttack(), fight);
            _tree.Add(BtTestTree.Set(new Subtree(), "_tree", shooting.Asset), fight).SetOn(false);

            Assert.IsTrue(HasWarning("never shoots"));
        }

        [Test]
        public void NodesNotConnectedToTheRootDoNotCount()
        {
            _data.CombatType = EnemyCombatType.Ranged;
            _tree.Add(new MeleeAttack(), _tree.Root);
            _tree.Asset.AddNode(new ShootBurst());

            Assert.IsTrue(HasWarning("never shoots"));
        }

        [Test]
        public void RaisingAlarmsWithoutARaiseAlarmNodeIsFlagged()
        {
            _data.RaisesAlarm = true;
            _tree.Add(new MeleeAttack(), _tree.Root);

            Assert.IsTrue(HasWarning("Raise Alarm"));
        }

        [Test]
        public void RaisingAlarmsWithARaiseAlarmNodeIsFine()
        {
            _data.RaisesAlarm = true;
            Sequence sequence = _tree.Add(new Sequence(), _tree.Root);
            _tree.Add(new RaiseAlarm(), sequence);
            _tree.Add(new MeleeAttack(), sequence);

            Assert.IsFalse(HasWarning("Raise Alarm"));
        }

        [Test]
        public void AssetNamesMustBePascalCase()
        {
            Assert.IsTrue(EnemyTypeCreator.IsAssetName("Stalker2"));
            Assert.IsFalse(EnemyTypeCreator.IsAssetName("stalker"));
            Assert.IsFalse(EnemyTypeCreator.IsAssetName("Night Stalker"));
            Assert.IsFalse(EnemyTypeCreator.IsAssetName(""));
        }

        private bool HasWarning(string text)
        {
            foreach (EnemyDataReview.Issue issue in EnemyDataReview.Check(_data))
                if (issue.Severity == BtIssueSeverity.Warning && issue.Message.Contains(text)) return true;
            return false;
        }
    }
}
