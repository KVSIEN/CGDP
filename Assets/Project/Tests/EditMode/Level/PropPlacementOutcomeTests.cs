using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // The planner says how each placement fared and which tiles each prop took, so room tools
    // can explain a bare room and draw what was placed.
    public class PropPlacementOutcomeTests
    {
        [Test]
        public void EachPlacementReportsWhatItAskedForAndGot()
        {
            RoomStructure room = Room(10, 10, new Vector2Int(9, 5));
            RoomPropPlanner planner = Planner(room, seed: 3);

            int placed = planner.Add(Placement(3, 3));

            Assert.AreEqual(1, planner.Outcomes.Count);
            PropPlacementOutcome outcome = planner.Outcomes[0];
            Assert.AreEqual(3, outcome.Wanted);
            Assert.AreEqual(placed, outcome.Placed);
            Assert.IsFalse(outcome.IsShort);
        }

        [Test]
        public void APlacementStoppedByTheFillShareSaysSo()
        {
            RoomStructure room = Room(10, 10, new Vector2Int(9, 5));
            RoomPropPlanner planner = Planner(room, seed: 3, fillShare: 0.03f);   // three tiles of floor

            planner.Add(Placement(20, 20));

            PropPlacementOutcome outcome = planner.Outcomes[0];
            Assert.IsTrue(outcome.IsShort);
            Assert.IsTrue(outcome.HitFillCap);
            StringAssert.Contains("share of floor", outcome.Explanation);
        }

        [Test]
        public void APropRemembersTheTilesItTook()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(8, 8, new Vector2Int(7, 4));
                RoomPropPlanner planner = Planner(room, seed);
                RoomPropPlacement wide = Placement(2, 2);
                wide.Footprint = new Vector2Int(2, 1);
                planner.Add(wide);

                foreach (PlannedProp prop in planner.Planned)
                {
                    Assert.AreEqual(2, prop.Tiles.width * prop.Tiles.height, $"seed {seed}");
                    foreach (Vector2Int tile in prop.Tiles.allPositionsWithin)
                        Assert.IsTrue(room.Has(tile, RoomTileTags.Prop), $"seed {seed}: {tile} not claimed");
                }
            }
        }
    }
}
