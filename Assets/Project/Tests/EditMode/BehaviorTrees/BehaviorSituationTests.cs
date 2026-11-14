using System;
using NUnit.Framework;
using CGD.BehaviorTrees;
using CGD.Editor;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    // The Behavior Composer's what-if situation (which behavior would get a list's turn) and
    // its live check marks.
    public class BehaviorSituationTests
    {
        private BtTestTree _main;
        private Selector   _list;

        [SetUp]
        public void SetUp()
        {
            _main = new BtTestTree();
            _list = _main.Add(new Selector(), _main.Root);
        }

        [TearDown]
        public void TearDown() => _main.Dispose();

        // A behavior in the list: a Sequence of the given checks, then an action.
        private Sequence Behavior(params BtNode[] checks)
        {
            var sequence = _main.Add(new Sequence(), _list);
            foreach (BtNode check in checks) _main.Add(check, sequence);
            _main.Add(new ScriptedLeaf(), sequence);
            return sequence;
        }

        private int Turn(BehaviorSituation situation) => situation.Turn(_main.Asset, _list, _ => true);

        [Test]
        public void TheFirstBehaviorWhoseChecksHoldGetsTheTurn()
        {
            Behavior(new SeesFoe(), new IsHurt());   // flee when hurt and seen
            Behavior(new SeesFoe());                 // fight
            Behavior();                              // routine
            var situation = new BehaviorSituation();

            Assert.AreEqual(2, Turn(situation), "calm: the routine");
            situation.Set("Sees Foe", true);
            Assert.AreEqual(1, Turn(situation));
            situation.Set("Is Hurt", true);
            Assert.AreEqual(0, Turn(situation));
        }

        [Test]
        public void NegatedChecksAndGatesAreRespected()
        {
            var inverter = new Inverter();
            var sequence = Behavior(inverter);
            _main.Add(new SeesFoe(), inverter);
            var cooldown = _main.Add(new Cooldown(), _list);
            _main.Add(new ScriptedLeaf(), cooldown);
            var situation = new BehaviorSituation();

            Assert.AreEqual(0, Turn(situation), "not Sees Foe holds while nothing is seen");
            situation.Set("Sees Foe", true);
            Assert.AreEqual(1, Turn(situation), "the cooldown is assumed ready");
            situation.GatesReady = false;
            Assert.AreEqual(-1, Turn(situation));
            Assert.IsNotNull(sequence);
        }

        [Test]
        public void AListOfChoicesWouldActWhenOneChoiceWould()
        {
            var choices = _main.Add(new Selector(), _list);
            var hunt    = _main.Add(new Sequence(), choices);
            _main.Add(new IsHurt(), hunt);
            _main.Add(new ScriptedLeaf(), hunt);
            var situation = new BehaviorSituation();

            Assert.IsFalse(situation.WouldAct(_main.Asset, choices, 0));
            situation.Set("Is Hurt", true);
            Assert.IsTrue(situation.WouldAct(_main.Asset, choices, 0));
            CollectionAssert.AreEqual(new[] { "Is Hurt" }, BehaviorSituation.Phrases(_main.Asset));
        }

        [Test]
        public void SwitchedOffBehaviorsNeverGetTheTurn()
        {
            Behavior();
            Behavior();
            var situation = new BehaviorSituation();

            Assert.AreEqual(0, situation.Turn(_main.Asset, _list, _ => true));
            var module = new BtTestTree();
            try
            {
                module.Add(new ScriptedLeaf(), module.Root);
                Subtree off = BehaviorEditing.AddBehavior(_main.Asset, _list, module.Asset, 0);
                Assert.AreEqual(1, situation.Turn(_main.Asset, _list, subtree => subtree != off));
            }
            finally
            {
                module.Dispose();
            }
        }

        [Test]
        public void LiveChecksShowTheirLastAnswer()
        {
            var seen  = new SeesFoe { Holds = true };
            var hurt  = new IsHurt { Holds = false };
            var flee  = Behavior(seen, hurt);
            BehaviorTree tree = _main.Build();

            Assert.AreEqual("· Sees Foe · · Is Hurt", BehaviorTrigger.DescribeLive(_main.Asset, flee, tree));
            Copy(tree, seen).Holds = true;
            Copy(tree, hurt).Holds = false;
            tree.Tick(0.1f);
            Assert.AreEqual("✓ Sees Foe · ✗ Is Hurt", BehaviorTrigger.DescribeLive(_main.Asset, flee, tree));
        }
    }

    [Serializable]
    internal sealed class SeesFoe : BtCondition
    {
        public bool Holds;
        protected override bool Check(BtContext context) => Holds;
    }

    [Serializable]
    internal sealed class IsHurt : BtCondition
    {
        public bool Holds;
        protected override bool Check(BtContext context) => Holds;
    }
}
