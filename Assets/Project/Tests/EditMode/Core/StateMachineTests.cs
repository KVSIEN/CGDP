using System.Collections.Generic;
using NUnit.Framework;
using CGD.Core;

namespace CGD.Tests
{
    public class StateMachineTests
    {
        private enum S { Idle, Chase, Dead }

        private static StateMachine<S> Build(List<string> log = null)
        {
            DelegateState Logged(string name) => new(
                enter: () => log?.Add("enter " + name),
                exit:  () => log?.Add("exit " + name));

            return new StateMachine<S>()
                .Add(S.Idle, Logged("Idle"))
                .Add(S.Chase, Logged("Chase"))
                .Add(S.Dead, Logged("Dead"));
        }

        [Test]
        public void StartEntersTheInitialState()
        {
            var log = new List<string>();
            var machine = Build(log);

            machine.Start(S.Idle);

            Assert.IsTrue(machine.IsIn(S.Idle));
            CollectionAssert.AreEqual(new[] { "enter Idle" }, log);
        }

        [Test]
        public void ChangingStatesExitsThenEnters()
        {
            var log = new List<string>();
            var machine = Build(log);
            machine.Start(S.Idle);
            log.Clear();

            Assert.IsTrue(machine.TryChangeTo(S.Chase));

            CollectionAssert.AreEqual(new[] { "exit Idle", "enter Chase" }, log);
            Assert.AreEqual(S.Idle, machine.Previous);
        }

        [Test]
        public void ChangingToTheCurrentStateIsRefused()
        {
            var machine = Build();
            machine.Start(S.Idle);

            Assert.IsFalse(machine.TryChangeTo(S.Idle));
        }

        [Test]
        public void RuleBlocksTryChangeButNotForce()
        {
            var machine = Build().SetRule((from, to) => from != S.Dead);
            machine.Start(S.Dead);

            Assert.IsFalse(machine.TryChangeTo(S.Idle));

            machine.ForceChangeTo(S.Idle);
            Assert.IsTrue(machine.IsIn(S.Idle));
        }

        [Test]
        public void TransitionFiresOnTickWhenItsConditionHolds()
        {
            bool seen = false;
            var machine = Build().AddTransition(S.Idle, S.Chase, () => seen);
            machine.Start(S.Idle);

            machine.Tick(0.1f);
            Assert.IsTrue(machine.IsIn(S.Idle));

            seen = true;
            machine.Tick(0.1f);
            Assert.IsTrue(machine.IsIn(S.Chase));
        }

        [Test]
        public void AnyTransitionWinsOverTheStatesOwn()
        {
            var machine = Build()
                .AddTransition(S.Idle, S.Chase, () => true)
                .AddAnyTransition(S.Dead, () => true);
            machine.Start(S.Idle);

            machine.Tick(0.1f);

            Assert.IsTrue(machine.IsIn(S.Dead));
        }

        [Test]
        public void TimeInStateResetsOnEntry()
        {
            var machine = Build();
            machine.Start(S.Idle);
            machine.Tick(1.5f);
            Assert.AreEqual(1.5f, machine.TimeInState, 0.0001f);

            machine.TryChangeTo(S.Chase);
            Assert.AreEqual(0f, machine.TimeInState);
        }

        [Test]
        public void ForcingAnUnknownStateThrows()
        {
            var machine = new StateMachine<S>().Add(S.Idle);
            machine.Start(S.Idle);

            Assert.Throws<System.ArgumentException>(() => machine.ForceChangeTo(S.Chase));
        }

        [Test]
        public void ChangedReportsPreviousAndNext()
        {
            var machine = Build();
            S prev = S.Dead, next = S.Dead;
            machine.Start(S.Idle);
            machine.Changed += (p, n) => { prev = p; next = n; };

            machine.TryChangeTo(S.Chase);

            Assert.AreEqual(S.Idle, prev);
            Assert.AreEqual(S.Chase, next);
        }
    }
}
