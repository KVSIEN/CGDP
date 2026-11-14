using NUnit.Framework;
using UnityEditor;
using CGD.Editor;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The Room Lab builds one room through the level's own pipeline: the doorways asked for,
    // the same room from the same seed, and never floor that can't be walked to.
    public class RoomLabTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";
        private const int Seeds = 20;

        private static RoomLabRecipe Recipe(MapNodeType type = MapNodeType.Combat)
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
            Assert.IsNotNull(settings, $"Missing {SettingsPath}");
            return new RoomLabRecipe { BuildSettings = settings, Type = type };
        }

        [Test]
        public void TheRoomGetsADoorwayOnEveryWallAsked()
        {
            RoomLabRecipe recipe = Recipe();   // north, east and south

            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomSample sample = RoomSampleBuilder.Build(recipe, seed.ToString());
                Assert.IsTrue(sample.IsValid, sample.Message);
                if (sample.Room.Landmark != null) continue;   // a landmark has its own doorways
                Assert.AreEqual(recipe.DoorCount, sample.DoorwayCount, $"seed {seed}: {sample.Room.DisplayName}");
            }
        }

        [Test]
        public void TheSameSeedBuildsTheSameRoom()
        {
            RoomLabRecipe recipe = Recipe();
            RoomSample a = RoomSampleBuilder.Build(recipe, "banana");
            RoomSample b = RoomSampleBuilder.Build(recipe, "banana");

            Assert.AreEqual(a.Room.Function, b.Room.Function);
            Assert.AreEqual(a.Room.Footprint.Count, b.Room.Footprint.Count);
            Assert.AreEqual(a.Furnishing.Props.Count, b.Furnishing.Props.Count);
            for (int i = 0; i < a.Furnishing.Props.Count; i++)
                Assert.AreEqual(a.Furnishing.Props[i].Position, b.Furnishing.Props[i].Position);
        }

        [Test]
        public void NoFloorIsCutOffFromTheDoorways()
        {
            foreach (MapNodeType type in new[] { MapNodeType.Combat, MapNodeType.Treasure, MapNodeType.Shop, MapNodeType.Boss })
            {
                RoomLabRecipe recipe = Recipe(type);
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomSample sample = RoomSampleBuilder.Build(recipe, seed.ToString());
                    Assert.IsTrue(sample.IsValid, sample.Message);
                    foreach (RoomIssue issue in sample.Issues)
                        StringAssert.DoesNotContain("can't be walked to", issue.Text, $"{type} seed {seed}: {sample.Room.DisplayName}");
                }
            }
        }

        [Test]
        public void APinnedPlaceIsBuilt()
        {
            RoomLabRecipe recipe = Recipe();
            foreach (RoomFunction function in recipe.BuildSettings.Functions)
            {
                if (function == null || function.Landmark != null) continue;
                recipe.Place = function;
                RoomSample sample = RoomSampleBuilder.Build(recipe, "7");
                Assert.IsTrue(sample.IsValid, sample.Message);
                Assert.AreEqual(function, sample.Room.Function);
            }
        }

        [Test]
        public void WithoutBuildSettingsItSaysWhatIsMissing()
        {
            RoomSample sample = RoomSampleBuilder.Build(new RoomLabRecipe());
            Assert.IsFalse(sample.IsValid);
            StringAssert.Contains("Build Settings", sample.Message);
        }
    }
}
