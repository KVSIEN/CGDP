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
        [TestCase("Behaviors/Fight/ShambleAndLunge")]
        [TestCase("Behaviors/Fight/HauntUnwatched")]
        [TestCase("Behaviors/Fight/TormentFromHiding")]
        [TestCase("Behaviors/Fight/RaiseMinions")]
        [TestCase("Behaviors/Fight/CurseAndVanish")]
        [TestCase("Behaviors/Fight/UseSignature")]
        [TestCase("Behaviors/Reactions/EscapeWhenHurt")]
        [TestCase("Behaviors/Reactions/PounceOnOpening")]
        [TestCase("Behaviors/Fight/Sentry")]
        [TestCase("Behaviors/Fight/Artillery")]
        [TestCase("Behaviors/Fight/Juggernaut")]
        [TestCase("Behaviors/Fight/Infiltrator")]
        [TestCase("Behaviors/Fight/Spotter")]
        [TestCase("Behaviors/Fight/Marksman")]
        [TestCase("Behaviors/Fight/Bulwark")]
        [TestCase("Behaviors/Fight/SwarmDive")]
        [TestCase("Behaviors/Fight/Engineer")]
        [TestCase("Behaviors/Fight/Hacker")]
        [TestCase("Behaviors/Reactions/FallBackToRange")]
        [TestCase("Behaviors/Reactions/CallForSupport")]
        [TestCase("Behaviors/Reactions/LockOn")]
        [TestCase("Behaviors/Fight/Swarm")]
        [TestCase("Behaviors/Fight/PackHunter")]
        [TestCase("Behaviors/Fight/Lurker")]
        [TestCase("Behaviors/Fight/Spitter")]
        [TestCase("Behaviors/Fight/Broodmother")]
        [TestCase("Behaviors/Fight/Regenerator")]
        [TestCase("Behaviors/Fight/Charger")]
        [TestCase("Behaviors/Fight/Burrower")]
        [TestCase("Behaviors/Fight/Infector")]
        [TestCase("Behaviors/Fight/Lure")]
        [TestCase("Behaviors/Fight/SporePod")]
        [TestCase("Behaviors/Fight/Alpha")]
        [TestCase("Behaviors/Reactions/Frenzy")]
        [TestCase("SwarmlingBehaviorTree")]
        [TestCase("Behaviors/Fight/Bodyguard")]
        [TestCase("Behaviors/Fight/DefendArea")]
        [TestCase("Behaviors/Fight/Opportunist")]
        [TestCase("Behaviors/Fight/Kiter")]
        [TestCase("Behaviors/Reactions/Avenger")]
        public void TreeHasNoErrors(string name)
        {
            BehaviorTreeAsset tree = Load(name);
            foreach (BtIssue issue in BtTreeValidator.Validate(tree))
                Assert.AreNotEqual(BtIssueSeverity.Error, issue.Severity, $"{name}: {issue.Message}");
        }

        // The library is for every faction: a behavior there never names one faction's ability or
        // grenade, it uses the kit slot each enemy type fills (EnemyData › Kit).
        [Test]
        public void LibraryBehaviorsAreFactionNeutral()
        {
            string[] guids = AssetDatabase.FindAssets("t:BehaviorTreeAsset", new[] { Trees + "Behaviors" });
            Assert.IsNotEmpty(guids);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
                foreach (BtNode node in tree.Nodes)
                {
                    bool named = node switch
                    {
                        UseAbility use     => use.Slot == AbilitySlot.Own && use.Named != null,
                        AbilityReady ready => ready.Slot == AbilitySlot.Own && ready.Named != null,
                        ThrowGrenade throwing => !throwing.UsesKit,
                        _ => false,
                    };
                    Assert.IsFalse(named, $"{path}: {node.GetType().Name} names its own asset; use a kit slot instead");
                }
            }
        }

        [Test]
        public void AITestAgentsCarryTheKitTheirTreesUse()
        {
            foreach (string name in new[] { "BioTestEnemyData", "TechTestEnemyData", "VoidTestEnemyData" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/AITest/{name}.asset");
                foreach (EnemyDataReview.Issue issue in EnemyDataReview.Check(data))
                    Assert.IsFalse(issue.Message.Contains("kit"), $"{name}: {issue.Message}");
            }
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
