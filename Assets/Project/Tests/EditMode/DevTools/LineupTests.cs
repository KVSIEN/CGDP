using NUnit.Framework;
using UnityEngine;
using CGD.DevTools;

namespace CGD.Tests
{
    public class LineupTests
    {
        [Test]
        public void FirstOfAKindKeepsItsNameAndLaterOnesAreNumberedFromTwo()
        {
            var names = new LineupNames();
            Assert.AreEqual("Bio Agent",   names.Next("Bio Agent"));
            Assert.AreEqual("Bio Agent 2", names.Next("Bio Agent"));
            Assert.AreEqual("Tech Agent",  names.Next("Tech Agent"));
            Assert.AreEqual("Bio Agent 3", names.Next("Bio Agent"));
        }

        [Test]
        public void ALoneMemberStandsOnTheSpotAndSeveralRingIt()
        {
            var squad = new LineupSquad { Position = new Vector3(4f, 0f, 7f), Spread = 3f };
            Assert.AreEqual(squad.Position, LineupLayout.SpotOf(squad, 0, 1));

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(3f, Vector3.Distance(squad.Position, LineupLayout.SpotOf(squad, i, 4)), 0.001f);
            Assert.AreNotEqual(LineupLayout.SpotOf(squad, 0, 4), LineupLayout.SpotOf(squad, 1, 4));
        }

        [Test]
        public void SquadSizeCountsOnlyMembersWithAnEnemy()
        {
            var enemy = new GameObject("Enemy");
            var squad = new LineupSquad();
            squad.Members.Add(new LineupMember { Prefab = enemy, Count = 3 });
            squad.Members.Add(new LineupMember { Prefab = null,  Count = 5 });
            Assert.AreEqual(3, squad.Size);
            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void PatrolGroupsAreWaypointNamesWithoutTheirNumbers()
        {
            var root = new GameObject("Waypoints").transform;
            foreach (string name in new[] { "Bio 1", "Bio 2", "Tech 1", "Void 1", "Void 2" })
                new GameObject(name).transform.SetParent(root);

            CollectionAssert.AreEqual(new[] { "Bio", "Tech", "Void" }, LineupLayout.PatrolGroups(root));
            Assert.AreEqual(2, LineupLayout.PatrolRoute(root, "Void").Length);
            Assert.IsEmpty(LineupLayout.PatrolRoute(root, ""));
            Object.DestroyImmediate(root.gameObject);
        }
    }
}
