using NUnit.Framework;
using UnityEditor;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The room generator scene's choices: a seed always makes the same room, one pinned choice
    // never changes what the others roll to, and a recipe becomes a one-room graph with a way in.
    public class RoomChoiceTests
    {
        private const string ContentPath = "Assets/Project/Data/Map/Content/StandardMapContentSettings.asset";

        private static MapContentSettings Content => AssetDatabase.LoadAssetAtPath<MapContentSettings>(ContentPath);

        [Test]
        public void ASeedAlwaysRollsTheSameRoom()
        {
            var choice = new RoomChoice { SeedText = "41" };

            RoomRecipe first = choice.ToRecipe(null, Content);
            RoomRecipe second = choice.ToRecipe(null, Content);

            Assert.AreEqual(first.Type, second.Type);
            Assert.AreEqual(first.Faction, second.Faction);
            Assert.AreEqual(first.Section, second.Section);
            Assert.AreEqual(first.Tier, second.Tier);
        }

        [Test]
        public void PinningTheTypeLeavesTheOtherRollsAlone()
        {
            var open = new RoomChoice { SeedText = "7" };
            var pinned = new RoomChoice { SeedText = "7", RandomType = false, Type = MapNodeType.Stealth };

            RoomRecipe rolled = open.ToRecipe(null, Content);
            RoomRecipe fixedType = pinned.ToRecipe(null, Content);

            Assert.AreEqual(MapNodeType.Stealth, fixedType.Type);
            Assert.AreEqual(rolled.Faction, fixedType.Faction);
            Assert.AreEqual(rolled.Section, fixedType.Section);
            Assert.AreEqual(rolled.Tier, fixedType.Tier);
        }

        [Test]
        public void ABreachComesFromAnotherFactionThanTheHolder()
        {
            Assume.That(Content.Factions.Count, Is.GreaterThan(1));
            for (int seed = 0; seed < 40; seed++)
            {
                var choice = new RoomChoice { SeedText = seed.ToString(), RandomType = false, Type = MapNodeType.Breach };

                RoomRecipe recipe = choice.ToRecipe(null, Content);

                Assert.AreNotEqual(recipe.Faction, recipe.BreachFaction, $"seed {seed}");
                Assert.That(recipe.BreachFaction, Is.InRange(0, Content.Factions.Count - 1));
            }
        }

        [Test]
        public void TheGraphHasTheRoomAJunctionPerDoorwayAndAStart()
        {
            var recipe = new RoomRecipe { Type = MapNodeType.Combat, DoorNorth = true, DoorEast = true, DoorSouth = false, DoorWest = true };

            MapGraph graph = RoomRecipeGraph.Build(recipe);

            Assert.IsTrue(graph.TryGetNode(RoomRecipeGraph.RoomId, out MapNode room));
            Assert.AreEqual(MapNodeType.Combat, room.Type);
            Assert.AreEqual(1 + recipe.DoorCount + 1, graph.Nodes.Count);
            Assert.IsNotNull(graph.FindFirst(MapNodeType.Start));
        }
    }
}
