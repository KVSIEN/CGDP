using NUnit.Framework;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;
using CGD.Editor;
using CGD.Enemies;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    // What the Behavior Composer reads back from a behavior: when it acts (its leading checks),
    // whether it takes every turn, and which kit pieces it needs.
    public class BehaviorReadingTests
    {
        private BtTestTree _main;
        private BtTestTree _module;

        [SetUp]
        public void SetUp()
        {
            _main   = new BtTestTree();
            _module = new BtTestTree();
        }

        [TearDown]
        public void TearDown()
        {
            _main.Dispose();
            _module.Dispose();
        }

        [Test]
        public void ASequenceActsWhenItsLeadingConditionsHold()
        {
            var branch = _main.Add(new Sequence(), _main.Root);
            _main.Add(new ScriptedCondition(), branch);
            var inverter = _main.Add(new Inverter(), branch);
            _main.Add(new ScriptedCondition(), inverter);
            _main.Add(new ScriptedLeaf(), branch);
            _main.Add(new ScriptedCondition(), branch);   // after an action: part of the body, not a check

            Assert.AreEqual("Scripted Condition · not Scripted Condition", BehaviorTrigger.Describe(_main.Asset, branch));
            Assert.IsFalse(BehaviorTrigger.ActsAlways(_main.Asset, branch));
        }

        [Test]
        public void GatesAroundABranchCountAsChecks()
        {
            var cooldown = _main.Add(Set(new Cooldown(), "_seconds", 8f), _main.Root);
            var chance   = _main.Add(Set(new Chance(), "_probability", 0.25f), cooldown);
            _main.Add(new ScriptedLeaf(), chance);

            string when = BehaviorTrigger.Describe(_main.Asset, cooldown);
            StringAssert.StartsWith("Cooldown (8 s) · Chance (", when);   // the percentage follows the machine's culture
            Assert.IsFalse(BehaviorTrigger.ActsAlways(_main.Asset, cooldown));
        }

        [Test]
        public void ABranchWithNoChecksActsAlways()
        {
            var branch = _main.Add(new Sequence(), _main.Root);
            _main.Add(new ScriptedLeaf(), branch);

            Assert.IsTrue(BehaviorTrigger.ActsAlways(_main.Asset, branch));
            Assert.AreEqual("always, when its turn comes", BehaviorTrigger.Describe(_main.Asset, branch));
        }

        [Test]
        public void AListOfChoicesActsAlwaysOnlyIfOneOfThemDoes()
        {
            var choices  = _main.Add(new Selector(), _main.Root);
            var guarded  = _main.Add(new Sequence(), choices);
            _main.Add(new ScriptedCondition(), guarded);
            _main.Add(new ScriptedLeaf(), guarded);

            Assert.IsFalse(BehaviorTrigger.ActsAlways(_main.Asset, choices));
            Assert.AreEqual("when one of its choices can act", BehaviorTrigger.Describe(_main.Asset, choices));

            _main.Add(new ScriptedLeaf(), choices);   // a fallback like Patrol
            Assert.IsTrue(BehaviorTrigger.ActsAlways(_main.Asset, choices));
        }

        [Test]
        public void ABehaviorIsReadThroughItsSubtree()
        {
            var branch = _module.Add(new Sequence(), _module.Root);
            _module.Add(new ScriptedCondition(), branch);
            _module.Add(new ScriptedLeaf(), branch);

            var list    = _main.Add(new Selector(), _main.Root);
            var subtree = BehaviorEditing.AddBehavior(_main.Asset, list, _module.Asset);

            Assert.AreEqual("Scripted Condition", BehaviorTrigger.Describe(_main.Asset, subtree));
            Assert.AreEqual("Scripted Condition", BehaviorTrigger.Describe(_module.Asset, _module.Root));
        }

        [Test]
        public void KitNeedsNameTheSlotsAndGrenadeABranchCastsFrom()
        {
            var branch = _main.Add(new Sequence(), _main.Root);
            _main.Add(Set(new AbilityReady(), "_slot", AbilitySlot.Escape), branch);
            _main.Add(Set(new UseAbility(), "_slot", AbilitySlot.Escape), branch);
            _main.Add(new ThrowGrenade(), branch);
            _main.Add(Set(new UseAbility(), "_slot", AbilitySlot.Own), branch);

            Assert.AreEqual("Escape, Grenade", KitNeed.Names(KitNeed.Of(EnemyTreeContents.Nodes(_main.Asset, branch))));
        }

        [Test]
        public void MissingKitNeedsAreTheEmptyPieces()
        {
            var branch = _main.Add(new Sequence(), _main.Root);
            _main.Add(Set(new UseAbility(), "_slot", AbilitySlot.Summon), branch);
            _main.Add(Set(new UseAbility(), "_slot", AbilitySlot.Signature), branch);

            var data   = ScriptableObject.CreateInstance<EnemyData>();
            var summon = ScriptableObject.CreateInstance<SummonAbility>();
            try
            {
                data.Summon = summon;
                var needs = KitNeed.Of(EnemyTreeContents.Nodes(_main.Asset, branch));
                Assert.AreEqual("Signature", KitNeed.Names(KitNeed.Missing(data, needs)));
            }
            finally
            {
                Object.DestroyImmediate(summon);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void AnEmptyBehaviorStartsWithASequence()
        {
            BehaviorEditing.BuildEmptyBehavior(_module.Asset);

            Assert.AreEqual(1, _module.Root.ChildGuids.Count);
            Assert.IsInstanceOf<Sequence>(_module.Asset.FindNode(_module.Root.ChildGuids[0]));
        }
    }
}
