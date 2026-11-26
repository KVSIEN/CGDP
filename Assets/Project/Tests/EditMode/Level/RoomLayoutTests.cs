using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using CGD.Core;
using CGD.Editor;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // Every generated place has layouts: every layout comes round, a pinned one is built, and
    // whichever is built the place keeps what makes it that place — its Required pieces always
    // go down, its layout's set pieces too, and no floor is cut off.
    public class RoomLayoutTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";
        private const int Seeds = 8;

        private static LevelBuildSettings Settings
        {
            get
            {
                var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
                Assert.IsNotNull(settings, $"Missing {SettingsPath}");
                return settings;
            }
        }

        private static IEnumerable<RoomFunction> PlacesWithLayouts()
        {
            foreach (RoomFunction function in Settings.Functions)
                if (function != null && function.Layouts.Count > 0) yield return function;
        }

        [Test]
        public void EveryGeneratedPlaceHasSeveralLayouts()
        {
            foreach (RoomFunction place in Settings.Functions)
            {
                if (place == null || place.Landmark != null || place.IsTown || place.DisplayName == "Junction") continue;
                Assert.GreaterOrEqual(place.Layouts.Count, 2, place.DisplayName);
                var layoutNames = new HashSet<string>();
                foreach (RoomLayout layout in place.Layouts)
                {
                    Assert.IsNotNull(layout, $"{place.DisplayName} lists an empty layout");
                    Assert.IsTrue(layoutNames.Add(layout.DisplayName), $"{place.DisplayName} has two layouts called {layout.DisplayName}");
                    if (!layout.IsBuilding) Assert.Greater(layout.Props.Count, 0, $"{place.DisplayName}: {layout.DisplayName} has no set pieces");
                }
            }
        }

        [Test]
        public void EveryLayoutComesRound()
        {
            foreach (RoomFunction place in PlacesWithLayouts())
            {
                var seen = new HashSet<RoomLayout>();
                for (int seed = 0; seed < 200; seed++) seen.Add(place.PickLayout(new RandomStream(new Seed((ulong)seed))));
                Assert.AreEqual(place.Layouts.Count, seen.Count, place.DisplayName);
            }
        }

        [Test]
        public void APinnedLayoutWinsOnlyForItsOwnPlace()
        {
            var random = new RandomStream(new Seed(3UL));
            RoomFunction[] places = new List<RoomFunction>(PlacesWithLayouts()).ToArray();
            Assert.GreaterOrEqual(places.Length, 2);

            RoomLayout own = places[0].Layouts[places[0].Layouts.Count - 1];
            Assert.AreSame(own, places[0].PickLayout(random, own));
            Assert.AreNotSame(own, places[1].PickLayout(random, own), "another place's layout is ignored");
        }

        [Test]
        public void EveryLayoutBuildsWithItsPlaceIntact()
        {
            foreach (RoomFunction place in PlacesWithLayouts())
                foreach (RoomLayout layout in place.Layouts)
                {
                    var recipe = new RoomLabRecipe { BuildSettings = Settings, Room = new RoomRecipe { Type = MapNodeType.Quiet, Place = place, Layout = layout } };
                    int setPieces = 0;
                    for (int seed = 0; seed < Seeds; seed++)
                    {
                        string what = $"{place.DisplayName}: {layout.DisplayName}, seed {seed}";
                        RoomSample sample = RoomSampleBuilder.Build(recipe, seed.ToString());
                        Assert.IsTrue(sample.IsValid, $"{what}: {sample.Message}");
                        Assert.AreSame(layout, sample.Room.RoomLayout, what);
                        Assert.AreEqual($"{place.DisplayName}: {layout.DisplayName}", sample.Room.LayoutName, what);

                        foreach (PropPlacementOutcome outcome in sample.Furnishing.Outcomes)
                        {
                            // A building's rooms furnish themselves and one may be too small for a piece:
                            // the Room Lab reports such shortfalls, the check here is for the layouts of one room.
                            if (layout.IsBuilding) continue;
                            if (outcome.Placement.Required) Assert.Greater(outcome.Placed, 0, $"{what}: {outcome.Name} is missing");
                            if (outcome.Placement == layout.Props[0] && outcome.Placed > 0) setPieces++;
                        }
                        foreach (RoomIssue issue in sample.Issues)
                            StringAssert.DoesNotContain("can't be walked to", issue.Text, what);
                    }
                    if (!layout.IsBuilding) Assert.Greater(setPieces, Seeds / 2, $"{place.DisplayName}: {layout.DisplayName}'s first set piece seldom fits");
                }
        }

        // Junction is kept bare on purpose: one arrangement, its own.
        [Test]
        public void APlaceWithoutLayoutsBuildsNone()
        {
            foreach (RoomFunction function in Settings.Functions)
            {
                if (function == null || function.Layouts.Count > 0 || function.Landmark != null) continue;
                var recipe = new RoomLabRecipe { BuildSettings = Settings, Room = new RoomRecipe { Type = MapNodeType.Quiet, Place = function } };
                RoomSample sample = RoomSampleBuilder.Build(recipe, "5");
                Assert.IsTrue(sample.IsValid, sample.Message);
                Assert.IsNull(sample.Room.RoomLayout, function.DisplayName);
                Assert.AreEqual(function.DisplayName, sample.Room.LayoutName);
                return;
            }
        }
    }
}
