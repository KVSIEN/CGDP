using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // A station is loose: it holds its goal and stands still until things have really moved.
    public class StationKeeperTests
    {
        [Test]
        public void TakesTheFirstIdealSpotAsItsGoal()
        {
            var keeper = new StationKeeper();
            keeper.Update(new Vector3(10f, 0f, 0f), Vector3.zero);
            Assert.AreEqual(10f, keeper.Goal.x, 0.001f);
            Assert.IsTrue(keeper.Moving);
        }

        [Test]
        public void KeepsItsGoalWhileTheIdealSpotOnlyShuffles()
        {
            var keeper = new StationKeeper(retarget: 2.5f);
            keeper.Update(new Vector3(10f, 0f, 0f), Vector3.zero);
            keeper.Update(new Vector3(11.5f, 0f, 1f), Vector3.zero);
            Assert.AreEqual(10f, keeper.Goal.x, 0.001f);
        }

        [Test]
        public void ChangesItsGoalOnceTheIdealSpotHasMovedFar()
        {
            var keeper = new StationKeeper(retarget: 2.5f);
            keeper.Update(new Vector3(10f, 0f, 0f), Vector3.zero);
            keeper.Update(new Vector3(14f, 0f, 0f), Vector3.zero);
            Assert.AreEqual(14f, keeper.Goal.x, 0.001f);
        }

        [Test]
        public void StopsOnArrivalAndStaysStillUntilItDriftsWellClear()
        {
            var keeper = new StationKeeper(arrive: 1f, depart: 2.5f);
            Vector3 goal = new(10f, 0f, 0f);
            keeper.Update(goal, new Vector3(9.5f, 0f, 0f));
            keeper.Update(goal, new Vector3(9.5f, 0f, 0f));
            Assert.IsFalse(keeper.Moving, "arrived");

            keeper.Update(goal, new Vector3(7.8f, 0f, 0f));
            Assert.IsFalse(keeper.Moving, "2.2 m off is still close enough");

            keeper.Update(goal, new Vector3(7f, 0f, 0f));
            Assert.IsTrue(keeper.Moving, "3 m off: sets off again");

            keeper.Update(goal, new Vector3(8.5f, 0f, 0f));
            Assert.IsTrue(keeper.Moving, "keeps walking until it arrives");
        }
    }
}
