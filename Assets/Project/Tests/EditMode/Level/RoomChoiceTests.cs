using NUnit.Framework;
using UnityEditor;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The room generator scene's choices: a seed always makes the same room, one pinned choice
    // never changes what the others roll to, a room shared by two factions never has the same one
    // twice, and a recipe becomes a one-room graph with a way in.
    public class RoomChoiceTests
    {
        private const string ContentPath = "Assets/Project/Data/Map/Content/StandardMapContentSettings.asset";

        private static MapContentSettings Content => AssetDatabase.LoadAssetAtPath<MapContentSettings>(ContentPath);

        [Test]
        public void ASeedAlwaysRollsTheSameRoom()
        {
            var choice = new RoomChoice { SeedText = "41" };

            RoomRecipe first = choice.ToRecipe(choice.ResolveSeed(), null, Content);
            RoomRecipe second = choice.ToRecipe(choice.ResolveSeed(), null, Content);

            Assert.AreEqual(first.Type, second.Type);
            Assert.AreEqual(first.Faction, second.Faction);
            Assert.AreEqual(first.Section, second.Section);
            Assert.AreEqual(first.Tier, second.Tier);
        }

        [Test]
        public void PinningTheTypeLeavesTheOtherRollsAlone()
        {
            var open = new RoomChoice { SeedText = "7" };
            var pinned = new RoomChoice { SeedText = "7", AnyType = false, Type = MapNodeType.Stealth };

            RoomRecipe rolled = open.ToRecipe(open.ResolveSeed(), null, Content);
            RoomRecipe fixedType = pinned.ToRecipe(pinned.ResolveSeed(), null, Content);

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
                var choice = new RoomChoice { AnyType = false, Type = MapNodeType.Breach };

                RoomRecipe recipe = choice.ToRecipe(new Seed((ulong)seed), null, Content);

                Assert.AreNotEqual(recipe.Faction, recipe.BreachFaction, $"seed {seed}");
                Assert.That(recipe.BreachFaction, Is.InRange(0, Content.Factions.Count - 1));
            }
        }

        [Test]
        public void AMixedRoomIsHeldByTwoDifferentFactions()
        {
            Assume.That(Content.Factions.Count, Is.GreaterThan(1));
            for (int seed = 0; seed < 40; seed++)
            {
                var choice = new RoomChoice { AnyType = false, Type = MapNodeType.Combat, Faction = RoomChoice.Mixed };

                RoomRecipe recipe = choice.ToRecipe(new Seed((ulong)seed), null, Content);

                Assert.That(recipe.Faction, Is.InRange(0, Content.Factions.Count - 1), $"seed {seed}");
                Assert.That(recipe.BreachFaction, Is.InRange(0, Content.Factions.Count - 1), $"seed {seed}");
                Assert.AreNotEqual(recipe.Faction, recipe.BreachFaction, $"seed {seed}");
            }
        }

        [Test]
        public void APinnedSecondFactionIsKeptAndTheHolderRolledAroundIt()
        {
            Assume.That(Content.Factions.Count, Is.GreaterThan(1));
            for (int seed = 0; seed < 40; seed++)
            {
                var choice = new RoomChoice { Faction = RoomChoice.Mixed, SecondFaction = 0 };

                RoomRecipe recipe = choice.ToRecipe(new Seed((ulong)seed), null, Content);

                Assert.AreEqual(0, recipe.BreachFaction, $"seed {seed}");
                Assert.AreNotEqual(0, recipe.Faction, $"seed {seed}");
            }
        }

        [Test]
        public void AOneFactionRoomHasNoSecondAndANoneRoomHasNoFaction()
        {
            var one = new RoomChoice { AnyType = false, Type = MapNodeType.Combat, Faction = 0 };
            var none = new RoomChoice { AnyType = false, Type = MapNodeType.Combat, Faction = RoomChoice.None };

            RoomRecipe held = one.ToRecipe(new Seed(3UL), null, Content);
            RoomRecipe plain = none.ToRecipe(new Seed(3UL), null, Content);

            Assert.AreEqual(0, held.Faction);
            Assert.AreEqual(MapNode.NoFaction, held.BreachFaction);
            Assert.AreEqual(MapNode.NoFaction, plain.Faction);
            Assert.AreEqual(MapNode.NoFaction, plain.BreachFaction);
        }

        [Test]
        public void TheFloorLevelsAreKeptWithinWhatRoomsCanHave()
        {
            var choice = new RoomChoice { LowestLevel = -5, HighestLevel = 1 };

            RoomRecipe recipe = choice.ToRecipe(new Seed(1UL), null, Content);

            Assert.AreEqual(RoomElevation.Lowest, recipe.LowestLevel);
            Assert.AreEqual(1, recipe.HighestLevel);
        }

        [Test]
        public void AnEmptySeedRollsAFreshOneAndATypedSeedIsKept()
        {
            var typed = new RoomChoice { SeedText = " banana " };

            Assert.AreEqual(Seed.Parse("banana"), typed.ResolveSeed());
            Assert.AreNotEqual(new RoomChoice().ResolveSeed(), new RoomChoice().ResolveSeed());
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

        [Test]
        public void AMixedRoomKeepsItsSecondFactionWhateverItsType()
        {
            var recipe = new RoomRecipe { Type = MapNodeType.Combat, Faction = 0, BreachFaction = 1 };

            MapGraph graph = RoomRecipeGraph.Build(recipe);

            Assert.IsTrue(graph.TryGetNode(RoomRecipeGraph.RoomId, out MapNode room));
            Assert.AreEqual(1, room.BreachFaction);
        }
    }
}
