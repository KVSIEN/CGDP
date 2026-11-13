using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using CGD.BehaviorTrees;
using CGD.Editor;
using CGD.Enemies;

namespace CGD.Tests
{
    // The AITest faction agents' trees and the behaviors they are built from: each loads, has
    // nothing the validator calls broken, and still contains what its playstyle rests on.
    public class FactionTreeTests
    {
        private const string Trees = "Assets/Project/Data/Enemies/BehaviorTrees/";

        [TestCase("BioBehaviorTree")]
        [TestCase("TechBehaviorTree")]
        [TestCase("VoidBehaviorTree")]
        [TestCase("Behaviors/Idle/BushRoamRoutine")]
        [TestCase("Behaviors/Fight/StalkRushHide")]
        [TestCase("Behaviors/Fight/FlushOut")]
        [TestCase("Behaviors/Fight/PeekABoo")]
        [TestCase("Behaviors/Fight/CreepUnwatched")]
        public void TreeHasNoErrors(string name)
        {
            BehaviorTreeAsset tree = Load(name);
            foreach (BtIssue issue in BtTreeValidator.Validate(tree))
                Assert.AreNotEqual(BtIssueSeverity.Error, issue.Severity, $"{name}: {issue.Message}");
        }

        [Test]
        public void BioRoamsBushesAndHuntsFromThem()
        {
            HashSet<Type> nodes = EnemyTreeContents.NodeTypes(Load("BioBehaviorTree"));

            Assert.IsTrue(nodes.Contains(typeof(RoamBushes)), "roams bush to bush when calm");
            Assert.IsTrue(nodes.Contains(typeof(Prowl)), "stalks");
            Assert.IsTrue(nodes.Contains(typeof(HideInBush)), "breaks off to a bush");
        }

        [Test]
        public void TechFlushesOutHidingTargets()
        {
            HashSet<Type> nodes = EnemyTreeContents.NodeTypes(Load("TechBehaviorTree"));

            Assert.IsTrue(nodes.Contains(typeof(TargetHiding)), "notices a target gone to ground");
            Assert.IsTrue(nodes.Contains(typeof(ThrowGrenade)), "grenades it out");
            Assert.IsTrue(nodes.Contains(typeof(Suppress)), "pins it down");
        }

        [Test]
        public void VoidPeeksAndCreepsWhileUnwatched()
        {
            HashSet<Type> nodes = EnemyTreeContents.NodeTypes(Load("VoidBehaviorTree"));

            Assert.IsTrue(nodes.Contains(typeof(Peek)), "pops out of cover to strike");
            Assert.IsTrue(nodes.Contains(typeof(CreepUnwatched)), "only closes in while unwatched");
        }

        [Test]
        public void BioEnemiesTravelThroughBushes()
        {
            foreach (string name in new[] { "ScavengerEnemyData", "BioTier2EnemyData", "BioTier3EnemyData", "AITest/BioTestEnemyData" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/{name}.asset");
                Assert.IsNotNull(data, name);
                Assert.Greater(data.BushDetour, 0f, name);
            }
        }

        private static BehaviorTreeAsset Load(string name)
        {
            var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(Trees + name + ".asset");
            Assert.IsNotNull(tree, $"{name} not found");
            return tree;
        }
    }
}
