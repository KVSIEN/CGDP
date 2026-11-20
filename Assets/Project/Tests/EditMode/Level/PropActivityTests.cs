using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Combat;
using CGD.Enemies;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Furniture idle enemies use (RoomPropPlacement.Activity, PropActivity): users stand in a
    // row at the piece's front, the activity reaches the pieces a placement puts down but not
    // its piles or group members, and every faction has something of its own to do.
    public class PropActivityTests
    {
        private const string Kinds = "Assets/Project/Data/Enemies/Interests/";

        [Test]
        public void UsersStandInARowJustInFrontOfThePiece()
        {
            Vector3[] two = PropActivity.StandSpots(2, across: 2f, depth: 1f);
            Assert.That(two, Has.Length.EqualTo(2));
            foreach (Vector3 spot in two) Assert.That(spot.z, Is.EqualTo(1.1f).Within(1e-5f), "half the depth and a stand-off");
            Assert.That(two[0].x, Is.EqualTo(-two[1].x).Within(1e-5f));
            Assert.That(two[1].x - two[0].x, Is.LessThanOrEqualTo(0.9f + 1e-5f));

            Vector3[] one = PropActivity.StandSpots(1, across: 1f, depth: 2f);
            Assert.That(one[0], Is.EqualTo(new Vector3(0f, 0f, 1.6f)));
        }

        [Test]
        public void AnActivityGoesWithEachPiecePlacedButNotWithAPile()
        {
            var kind = ScriptableObject.CreateInstance<InterestKind>();
            try
            {
                RoomPropPlanner planner = Planner(Room(10, 8, new Vector2Int(0, 4)), seed: 3);
                RoomPropPlacement single = Placement(2, 2);
                single.Prefabs = new GameObject[1];
                single.Activity = kind;
                planner.Add(single);
                Assert.That(planner.Planned, Has.Count.EqualTo(2));
                foreach (PlannedProp prop in planner.Planned) Assert.That(prop.Activity, Is.SameAs(kind));

                RoomPropPlanner piles = Planner(Room(10, 8, new Vector2Int(0, 4)), seed: 3);
                RoomPropPlacement pile = Placement(1, 1);
                pile.Prefabs = new GameObject[1];
                pile.Pile = new CGD.Core.IntRange(3, 3);
                pile.Activity = kind;
                piles.Add(pile);
                Assert.That(piles.Planned, Has.Count.EqualTo(3));
                foreach (PlannedProp prop in piles.Planned) Assert.That(prop.Activity, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(kind);
            }
        }

        [TestCase(Team.Tech, "OperatingInterest")]
        [TestCase(Team.Tech, "ServicingInterest")]
        [TestCase(Team.Void, "RummagingInterest")]
        [TestCase(Team.Void, "LoungingInterest")]
        [TestCase(Team.Bio,  "FeedingInterest")]
        public void EachFactionHasSomethingOfItsOwnToDo(Team team, string kind)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InterestKind>(Kinds + kind + ".asset");
            Assert.IsNotNull(asset, kind);
            Assert.Greater(asset.AppealTo(team), 0.5f);
        }

        [Test]
        public void TheControlStationsConsolesAreOperated()
        {
            var set = AssetDatabase.LoadAssetAtPath<RoomPropSet>("Assets/Project/Data/Level/Functions/Sets/ControlStationPropSet.asset");
            var operating = AssetDatabase.LoadAssetAtPath<InterestKind>(Kinds + "OperatingInterest.asset");
            Assert.IsNotNull(set);
            Assert.That(set.Props[0].Activity, Is.SameAs(operating));
        }
    }
}
