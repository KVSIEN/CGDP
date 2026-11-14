using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Editor;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    // Behavior settings: node values a behavior names so each use (a Subtree) and each enemy
    // type (BehaviorTuning) can change them without copying the behavior.
    public class BehaviorSettingsTests
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

        // A behavior whose Root runs one probe, with its Range exposed as "Reach".
        private SettingProbe ProbeBehavior(float range = 2f)
        {
            var probe = _module.Add(new SettingProbe { Range = range }, _module.Root);
            _module.Asset.AddSetting(new BehaviorSetting("Reach", probe.Guid, "_range"));
            return probe;
        }

        private static SettingProbe RunningProbe(BehaviorTree tree, Subtree use) => (SettingProbe)Copy(tree, use).Branch;

        [Test]
        public void ASettingReadsItsDefaultFromTheNode()
        {
            ProbeBehavior(range: 2.5f);

            Assert.IsTrue(BehaviorSettings.TryGetDefault(_module.Asset, "Reach", out float value));
            Assert.AreEqual(2.5f, value);
            Assert.AreEqual(new List<string> { "Reach" }, _module.Asset.SettingNames());
        }

        [Test]
        public void EachUseRunsTheBehaviorWithItsOwnValue()
        {
            SettingProbe authored = ProbeBehavior(range: 2f);
            var list  = _main.Add(new Selector(), _main.Root);
            var near  = BehaviorEditing.AddBehavior(_main.Asset, list, _module.Asset);
            var far   = BehaviorEditing.AddBehavior(_main.Asset, list, _module.Asset);
            far.SetValue("Reach", 9f);

            BehaviorTree tree = _main.Build();

            Assert.AreEqual(2f, RunningProbe(tree, near).Range);
            Assert.AreEqual(9f, RunningProbe(tree, far).Range);
            Assert.AreEqual(2f, authored.Range, "the asset itself is never changed");
        }

        [Test]
        public void AnEnemyTypesTuningWinsOverTheTrees()
        {
            ProbeBehavior();
            var list = _main.Add(new Selector(), _main.Root);
            var use  = BehaviorEditing.AddBehavior(_main.Asset, list, _module.Asset);
            use.SetValue("Reach", 5f);

            var tuning = new BehaviorTuning(_module.Asset);
            tuning.SetValue("Reach", 7f);
            BehaviorTree tree = _main.Asset.CreateInstance(null, null, new List<BehaviorTuning> { tuning });

            Assert.AreEqual(7f, RunningProbe(tree, use).Range);
        }

        [Test]
        public void AnEnemyTypeCanTuneItsTreesOwnSettings()
        {
            var probe = _main.Add(new SettingProbe { Range = 1f }, _main.Root);
            _main.Asset.AddSetting(new BehaviorSetting("Reach", probe.Guid, "_range"));

            var tuning = new BehaviorTuning(_main.Asset);
            tuning.SetValue("Reach", 4f);
            BehaviorTree tree = _main.Asset.CreateInstance(null, null, new List<BehaviorTuning> { tuning });

            Assert.AreEqual(4f, Copy(tree, probe).Range);
        }

        [Test]
        public void ABehaviorSwitchedOffForAnEnemyTypeFails()
        {
            _module.Add(new ScriptedLeaf(), _module.Root);
            var list     = _main.Add(new Selector(), _main.Root);
            var use      = BehaviorEditing.AddBehavior(_main.Asset, list, _module.Asset);
            var fallback = _main.Add(new ScriptedLeaf { Result = BtStatus.Running }, list);

            var tuning = new BehaviorTuning(_module.Asset);
            tuning.SetOff(true);
            BehaviorTree tree = _main.Asset.CreateInstance(null, null, new List<BehaviorTuning> { tuning });
            tree.Tick(0.1f);

            Assert.IsNull(Copy(tree, use).Branch, "never built");
            Assert.IsTrue(Copy(tree, fallback).IsRunning, "the turn passes to the next behavior");
        }

        [Test]
        public void OneNameDrivesEverySettingCalledThat()
        {
            var sequence = _module.Add(new Sequence(), _module.Root);
            var first    = _module.Add(new SettingProbe(), sequence);
            var second   = _module.Add(new SettingProbe(), sequence);
            _module.Asset.AddSetting(new BehaviorSetting("Reach", first.Guid, "_range"));
            _module.Asset.AddSetting(new BehaviorSetting("Reach", second.Guid, "_range"));

            var use = BehaviorEditing.AddBehavior(_main.Asset, _main.Add(new Selector(), _main.Root), _module.Asset);
            use.SetValue("Reach", 6f);
            BehaviorTree tree = _main.Build();

            var running = (Sequence)Copy(tree, use).Branch;
            Assert.AreEqual(6f, ((SettingProbe)running.Children[0]).Range);
            Assert.AreEqual(6f, ((SettingProbe)running.Children[1]).Range);
        }

        [Test]
        public void WholeNumbersFlagsAndChoicesConvert()
        {
            var probe = new SettingProbe();

            Assert.IsTrue(BehaviorSettings.TrySet(probe, "_count", 2.6f));
            Assert.IsTrue(BehaviorSettings.TrySet(probe, "_loud", 1f));
            Assert.IsTrue(BehaviorSettings.TrySet(probe, "_mood", (float)ProbeMood.Angry));
            Assert.AreEqual(3, probe.Count);
            Assert.IsTrue(probe.Loud);
            Assert.AreEqual(ProbeMood.Angry, probe.Mood);

            Assert.IsNull(BehaviorSettings.FieldType(probe, "_label"), "text can't be a setting");
            Assert.IsFalse(BehaviorSettings.TrySet(probe, "_missing", 1f));
        }

        [Test]
        public void ValuesStayInsideTheFieldsLimits()
        {
            var chance = new Chance();

            BehaviorSettings.TrySet(chance, "_probability", 3f);
            Assert.IsTrue(BehaviorSettings.TryGet(chance, "_probability", out float value));
            Assert.AreEqual(1f, value, "Range(0, 1)");

            var cooldown = new Cooldown();
            BehaviorSettings.TrySet(cooldown, "_seconds", -5f);
            BehaviorSettings.TryGet(cooldown, "_seconds", out value);
            Assert.AreEqual(0f, value, "Min(0)");
        }

        [Test]
        public void SavingABranchAsABehaviorTakesItsSettingsAlong()
        {
            var list   = _main.Add(new Selector(), _main.Root);
            var branch = _main.Add(new Sequence(), list);
            var probe  = _main.Add(new SettingProbe(), branch);
            _main.Asset.AddSetting(new BehaviorSetting("Reach", probe.Guid, "_range"));

            BehaviorEditing.ExtractBranch(_main.Asset, branch, _module.Asset);

            Assert.AreEqual(0, _main.Asset.Settings.Count);
            Assert.AreEqual(1, _module.Asset.Settings.Count);
            Assert.AreEqual(probe.Guid, _module.Asset.Settings[0].NodeGuid);
        }

        [Test]
        public void TheChecksFlagValuesForSettingsTheBehaviorLacks()
        {
            ProbeBehavior();
            var use = BehaviorEditing.AddBehavior(_main.Asset, _main.Add(new Selector(), _main.Root), _module.Asset);
            use.SetValue("Rech", 3f);   // a typo, or a renamed setting

            List<BtIssue> issues = BtTreeValidator.Validate(_main.Asset);

            Assert.IsTrue(issues.Exists(issue => issue.NodeGuid == use.Guid && issue.Message.Contains("'Rech'")));
        }

        [Test]
        public void TuningDropsBackToEmptyWhenEverythingIsCleared()
        {
            var tuning = new BehaviorTuning(_module.Asset);
            tuning.SetValue("Reach", 3f);
            tuning.SetValue("Reach", 4f);
            Assert.AreEqual(1, tuning.Values.Count, "one value per setting");
            Assert.IsTrue(tuning.TryGetValue("Reach", out float value));
            Assert.AreEqual(4f, value);

            tuning.ClearValue("Reach");
            Assert.IsTrue(tuning.IsEmpty);
        }
    }

    internal enum ProbeMood { Calm, Angry }

    // A leaf with one setting of each kind a behavior can expose (plus one it can't).
    [Serializable]
    internal sealed class SettingProbe : BtAction
    {
        [SerializeField] private float     _range = 1f;
        [SerializeField] private int       _count;
        [SerializeField] private bool      _loud;
        [SerializeField] private ProbeMood _mood;
        [SerializeField] private string    _label = "";

        public float     Range { get => _range; set => _range = value; }
        public int       Count => _count;
        public bool      Loud  => _loud;
        public ProbeMood Mood  => _mood;
        public string    Label => _label;

        protected override BtStatus OnTick(BtContext context) => BtStatus.Success;
    }
}
