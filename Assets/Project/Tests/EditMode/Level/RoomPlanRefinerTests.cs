using System;
using NUnit.Framework;
using CGD.Core;
using CGD.Level;

namespace CGD.Tests
{
    // The hill climb that moves a furnished room's loose props toward its priorities, on a plan
    // with no rooms in it: a row of numbers that should each reach a target.
    public class RoomPlanRefinerTests
    {
        private const int Target = 5;

        // Each "prop" is a number; a move sets it to a random one of 0–9, and the cost is how far they are from the target.
        private sealed class NumberPlan : IRefinablePlan
        {
            public readonly int[] Values;
            public int Moves;

            public NumberPlan(params int[] values) => Values = values;

            public int MovableCount => Values.Length;

            public float Cost()
            {
                float cost = 0f;
                foreach (int value in Values) cost += Math.Abs(value - Target);
                return cost;
            }

            public bool TryMove(int index, RandomStream random, out Action undo)
            {
                int before = Values[index];
                Values[index] = random.Range(0, 10);
                Moves++;
                undo = () => Values[index] = before;
                return true;
            }
        }

        private static RandomStream Stream(ulong seed = 1) => new(new Seed(seed));

        [Test]
        public void ItReachesAPerfectRoomGivenEnoughSteps()
        {
            var plan = new NumberPlan(0, 9, 1, 8);

            RefineResult result = RoomPlanRefiner.Refine(plan, plan.Cost, Stream(), 400);

            Assert.AreEqual(0f, result.Cost);
            Assert.AreEqual(0f, plan.Cost());
            Assert.Greater(result.Moves, 0);
        }

        [Test]
        public void ItNeverMakesTheRoomWorse()
        {
            var plan = new NumberPlan(0, 9, 1, 8, 4, 6);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                float before = plan.Cost();
                RefineResult result = RoomPlanRefiner.Refine(plan, plan.Cost, Stream(seed), 5);

                Assert.AreEqual(before, result.StartCost);
                Assert.LessOrEqual(result.Cost, before);
                Assert.AreEqual(result.Cost, plan.Cost(), "the plan reads what the result says");
            }
        }

        [Test]
        public void ARoomThatFitsIsLeftAlone()
        {
            var plan = new NumberPlan(Target, Target);

            RefineResult result = RoomPlanRefiner.Refine(plan, plan.Cost, Stream(), 50);

            Assert.AreEqual(0, plan.Moves, "no move is even tried");
            Assert.AreEqual(0, result.Moves);
            Assert.AreEqual(0f, result.Cost);
        }

        [Test]
        public void NoStepsMeansNoMoves()
        {
            var plan = new NumberPlan(0, 9);

            RefineResult result = RoomPlanRefiner.Refine(plan, plan.Cost, Stream(), 0);

            Assert.AreEqual(0, plan.Moves);
            Assert.AreEqual(result.StartCost, result.Cost);
        }

        [Test]
        public void ARoomWithNothingToMoveIsLeftAlone()
        {
            var plan = new NumberPlan();

            RefineResult result = RoomPlanRefiner.Refine(plan, () => 3f, Stream(), 50);

            Assert.AreEqual(3f, result.Cost);
            Assert.AreEqual(0, result.Moves);
        }

        [Test]
        public void AMoveThatFindsNoSpotCostsAStepButChangesNothing()
        {
            var plan = new StuckPlan();

            RefineResult result = RoomPlanRefiner.Refine(plan, () => 1f, Stream(), 10);

            Assert.AreEqual(10, plan.Asked);
            Assert.AreEqual(0, result.Moves);
            Assert.AreEqual(1f, result.Cost);
        }

        [Test]
        public void ItIsTheSameEveryTimeFromTheSameSeed()
        {
            var first = new NumberPlan(0, 9, 1, 8);
            var second = new NumberPlan(0, 9, 1, 8);

            RoomPlanRefiner.Refine(first, first.Cost, Stream(7), 12);
            RoomPlanRefiner.Refine(second, second.Cost, Stream(7), 12);

            CollectionAssert.AreEqual(first.Values, second.Values);
        }

        private sealed class StuckPlan : IRefinablePlan
        {
            public int Asked;
            public int MovableCount => 1;

            public bool TryMove(int index, RandomStream random, out Action undo)
            {
                Asked++;
                undo = null;
                return false;
            }
        }
    }
}
