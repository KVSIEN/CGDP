using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using CGD.BehaviorTrees;
using CGD.Editor;
using CGD.Enemies;

namespace CGD.Tests
{
    // The AI Simulator faction agents' trees and the behaviors they are built from: each loads, has
    // nothing the validator calls broken, and still contains what its playstyle rests on.
    public class FactionTreeTests
    {
        private const string Trees = "Assets/Project/Data/Enemies/BehaviorTrees/";

        [TestCase("BioBehaviorTree")]
        [TestCase("TechBehaviorTree")]
        [TestCase("VoidBehaviorTree")]
        [TestCase("BioTankBehaviorTree")]
        [TestCase("TechSiegeBehaviorTree")]
        [TestCase("VoidTankBehaviorTree")]
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
        [TestCase("Behaviors/Fight/HiddenAmbush")]
        [TestCase("Behaviors/Fight/HiddenScan")]
        [TestCase("Behaviors/Fight/HiddenHaunt")]
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
                        LayTrap lay        => lay.Slot == AbilitySlot.Own && lay.Named != null,
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
            foreach (string name in new[] { "BioTestEnemyData", "TechTestEnemyData", "VoidTestEnemyData",
                                             "BioSupportEnemyData", "TechSupportEnemyData", "VoidSupportEnemyData",
                                             "BioTankEnemyData", "TechSiegeEnemyData", "VoidTankEnemyData" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/AITest/{name}.asset");
                foreach (EnemyDataReview.Issue issue in EnemyDataReview.Check(data))
                    Assert.IsFalse(issue.Message.Contains("kit"), $"{name}: {issue.Message}");
            }
        }

        [Test]
        public void EachFactionsSupportAgentPositionsToHelpItsAllies()
        {
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
            {
                HashSet<Type> nodes = EnemyTreeContents.NodeTypes(Load(faction + "SupportBehaviorTree"));
                Assert.IsTrue(nodes.Contains(typeof(TakeSupportPosition)), $"{faction}: holds the spot that reaches its allies");
                Assert.IsTrue(nodes.Contains(typeof(SupportAlly)), $"{faction}: goes to the ally that needs it");
            }
        }

        // Each support agent plays its own role: the Void enchanter keeps behind the line, the
        // Tech shield wall holds its front, the Bio pack mender runs with the ally leading the hunt.
        [Test]
        public void SupportAgentsKeepToTheirSideOfTheLine()
        {
            CollectionAssert.AreEquivalent(new[] { SupportLine.Behind }, Lines("VoidSupportBehaviorTree"), "Void: casts from behind its allies");
            CollectionAssert.Contains(Lines("TechSupportBehaviorTree"), SupportLine.Front, "Tech: holds the front of the line");
            Assert.IsTrue(EnemyTreeContents.NodeTypes(Load("BioSupportBehaviorTree")).Contains(typeof(GuardAlly)),
                          "Bio: runs at the shoulder of its predator");
        }

        // The Tech shield wall's tree is built round its Frontal Shield: the prefab must carry one
        // that stands a barrier, and the tree must know when it is broken.
        [Test]
        public void TechSupportCarriesAShieldWall()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Project/Prefabs/Characters/Enemies/AITest/TechSupportAgent.prefab");
            Assert.IsTrue(prefab.TryGetComponent(out FrontalShield shield), "TechSupportAgent has a Frontal Shield");
            Assert.IsTrue(shield.HasBarrier, "its Frontal Shield stands a barrier");
            Assert.IsTrue(EnemyTreeContents.NodeTypes(Load("TechSupportBehaviorTree")).Contains(typeof(FrontalShieldDown)),
                          "Tech: falls back while its shield is broken");
        }

        private static HashSet<SupportLine> Lines(string tree)
        {
            var lines = new HashSet<SupportLine>();
            foreach (BtNode node in EnemyTreeContents.Nodes(Load(tree)))
                if (node is TakeSupportPosition position) lines.Add(position.Line);
            return lines;
        }

        // Tech has no tank: its siege sentinel holds a post far behind its squad and denies the
        // enemy cover — mortaring where it hides, firing into the spot, watching for it to break cover.
        [Test]
        public void TechSiegeIsASentinelThatDeniesCover()
        {
            HashSet<Type> nodes = EnemyTreeContents.NodeTypes(Load("TechSiegeBehaviorTree"));
            Assert.IsTrue(nodes.Contains(typeof(TargetHiding)), "notices a target going to ground");
            Assert.IsTrue(nodes.Contains(typeof(ThrowGrenade)) && nodes.Contains(typeof(Suppress)) && nodes.Contains(typeof(Overwatch)),
                          "shells the spot, fires into it and watches it");
            CollectionAssert.AreEquivalent(new[] { SupportLine.Behind }, Lines("TechSiegeBehaviorTree"), "posts itself behind its squad");

            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Project/Prefabs/Characters/Enemies/AITest/TechSiegeAgent.prefab");
            Assert.IsFalse(prefab.TryGetComponent(out CGD.Combat.SoakAura _), "it is no tank: it soaks nothing for its squad");
        }

        // A tank is the frontline sponge: far harder to kill and slower than its faction's agent,
        // and it takes part of the hits on the allies round it.
        [Test]
        public void EachFactionsTankIsBuiltToSoak()
        {
            foreach (string faction in new[] { "Bio", "Void" })
            {
                var tank  = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/AITest/{faction}TankEnemyData.asset");
                var agent = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/AITest/{faction}TestEnemyData.asset");
                Assert.Greater(tank.EffectiveHealth, agent.EffectiveHealth * 2f, $"{faction}: at least twice as hard to kill as its agent");
                Assert.Less(tank.ChaseSpeed, agent.ChaseSpeed, $"{faction}: slower than its agent");
                Assert.AreSame(tank.BehaviorTree, Load(faction + "TankBehaviorTree"), $"{faction}: runs its tank tree");

                var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>($"Assets/Project/Prefabs/Characters/Enemies/AITest/{faction}TankAgent.prefab");
                Assert.IsTrue(prefab.TryGetComponent(out CGD.Combat.SoakAura soak) && soak.Share > 0f, $"{faction}: its prefab soaks for its allies");
                Assert.IsTrue(prefab.TryGetComponent(out EnemyAI ai) && ai.Data == tank, $"{faction}: its prefab uses the tank data");
            }
        }

        // The faction agents are where every behavior gets tried out; support agents stay focused.
        [Test]
        public void FactionAgentTreesStayTheFullestPerFaction()
        {
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
            {
                int agent   = EnemyTreeContents.NodeTypes(Load(faction + "BehaviorTree")).Count;
                int support = EnemyTreeContents.NodeTypes(Load(faction + "SupportBehaviorTree")).Count;
                Assert.Greater(agent, support, $"{faction}: the agent tree should use more kinds of node than the support tree");
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
        public void EachFactionHasItsOwnWayOfLyingInWaitWhenHidden()
        {
            Assert.IsTrue(EnemyTreeContents.NodeTypes(Load("BioBehaviorTree")).IsSupersetOf(new[] { typeof(IsHidden), typeof(AmbushFromCover) }), "Bio ambushes from cover");
            Assert.IsTrue(EnemyTreeContents.NodeTypes(Load("TechBehaviorTree")).IsSupersetOf(new[] { typeof(IsHidden), typeof(ScanFromCover) }), "Tech scans from cover");
            Assert.IsTrue(EnemyTreeContents.NodeTypes(Load("VoidBehaviorTree")).IsSupersetOf(new[] { typeof(IsHidden), typeof(HauntFromCover) }), "Void haunts from cover");
        }

        [Test]
        public void EachFactionAgentCarriesItsSpecial()
        {
            foreach (string name in new[] { "BioTestEnemyData", "TechTestEnemyData", "VoidTestEnemyData" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/AITest/{name}.asset");
                Assert.IsNotNull(data.Special, $"{name} has no Special ability");
            }
        }

        [Test]
        public void BioEnemiesTravelThroughBushes()
        {
            foreach (string name in new[] { "ScavengerEnemyData", "AITest/BioTestEnemyData" })
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
