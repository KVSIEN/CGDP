using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using CGD.Editor;
using CGD.Level;

namespace CGD.Tests
{
    // Shared kits (RoomPropSet) and the ship-operations places built with them: a kit goes down
    // where it is listed, whatever a placement stands Near is always keyed before it, every
    // operations place keys its machine "Plant" for the kits to gather round, and the layout
    // check a designer runs agrees with what the level builds.
    public class PropSetTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";
        private const string SetsFolder = "Assets/Project/Data/Level/Functions/Sets";
        private const string Plant = "Plant";

        private static readonly string[] OperationsPlaces =
        {
            "Engine Room", "Power Distribution", "Waste Reclamation", "Fuel Depot", "Coolant Plant",
            "Maintenance Hub", "Comms Array", "Life Support", "Water Treatment", "Processing Plant",
        };

        private static LevelBuildSettings Settings
        {
            get
            {
                var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
                Assert.IsNotNull(settings, $"Missing {SettingsPath}");
                return settings;
            }
        }

        private static RoomFunction Place(string name)
        {
            foreach (RoomFunction function in Settings.Functions)
                if (function != null && function.DisplayName == name) return function;
            Assert.Fail($"The level build settings list no {name}");
            return null;
        }

        [Test]
        public void EverySetIsAKitOfRealPieces()
        {
            int sets = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:RoomPropSet", new[] { SetsFolder }))
            {
                var set = AssetDatabase.LoadAssetAtPath<RoomPropSet>(AssetDatabase.GUIDToAssetPath(guid));
                sets++;
                Assert.Greater(set.Props.Count, 0, $"{set.name} is empty");
                foreach (RoomPropPlacement placement in set.Props)
                    Assert.IsTrue(placement.HasPieces || placement.HasMembers, $"{set.name} has a placement with nothing to put down");
            }
            Assert.GreaterOrEqual(sets, 4);
        }

        [TestCaseSource(nameof(OperationsPlaces))]
        public void AnOperationsPlaceKeysItsMachineForItsKits(string name)
        {
            RoomFunction place = Place(name);
            Assert.Greater(place.Sets.Count, 0, $"{name} uses no shared kit");
            Assert.GreaterOrEqual(place.Layouts.Count, 3, $"{name} has too few layouts");

            bool keyed = false;
            foreach (RoomPropPlacement placement in place.Props)
                keyed |= placement.Required && placement.Key == Plant;
            Assert.IsTrue(keyed, $"{name}'s required machine is not keyed {Plant}");
        }

        // A placement Near a key nothing earlier put down never goes in: catch it in the data.
        [Test]
        public void WhatAPieceStandsNearIsAlwaysKeyedBeforeIt()
        {
            foreach (RoomFunction place in Settings.Functions)
            {
                if (place == null || place.Landmark != null) continue;
                var layouts = new List<RoomLayout>(place.Layouts);
                if (layouts.Count == 0) layouts.Add(null);
                foreach (RoomLayout layout in layouts)
                {
                    var keys = new HashSet<string>();
                    foreach (RoomPropPlacement placement in place.PropsWith(layout))
                    {
                        if (placement == null) continue;
                        if (placement.StandsNear)
                            Assert.IsTrue(keys.Contains(placement.Near),
                                $"{place.DisplayName}{(layout != null ? ": " + layout.DisplayName : "")} stands something Near {placement.Near} before anything is keyed so");
                        if (!string.IsNullOrEmpty(placement.Key)) keys.Add(placement.Key);
                    }
                }
            }
        }

        [Test]
        public void AKitGoesDownWhereItIsListed()
        {
            RoomFunction engine = Place("Engine Room");
            RoomLayout layout = null;
            foreach (RoomLayout candidate in engine.Layouts)
                if (candidate.Sets.Count > 0) layout = candidate;
            Assert.IsNotNull(layout, "an Engine Room layout lists a set");

            var order = new List<RoomPropPlacement>(engine.PropsWith(layout));
            int layoutSet = order.IndexOf(layout.Sets[0].Props[0]);
            int placeSet  = order.LastIndexOf(engine.Sets[engine.Sets.Count - 1].Props[0]);
            Assert.Greater(order.IndexOf(layout.Props[layout.Props.Count - 1]), order.IndexOf(engine.Props[0]), "set pieces follow the anchor");
            Assert.Greater(layoutSet, order.IndexOf(layout.Props[layout.Props.Count - 1]), "the layout's sets follow its set pieces");
            Assert.Greater(placeSet, layoutSet, "the place's sets come last");
        }

        [Test]
        public void TheLayoutCheckSeesWhatTheLevelBuilds()
        {
            RoomFunction power = Place("Power Distribution");
            RoomLayoutCheck check = RoomLayoutCheck.Run(Settings, power, power.Layouts[0], seeds: 4);

            Assert.AreEqual(4, check.Built, check.Summary);
            Assert.AreEqual(0, check.RoomsMissingRequired, check.Summary);
            Assert.IsTrue(check.Pieces.Count >= power.Props.Count + power.Layouts[0].Props.Count, "every piece is tracked");
            Assert.AreEqual("Anchor", check.Pieces[0].Source);
        }

        [Test]
        public void ANewLayoutIsNamedAndFiledLikeTheRest()
        {
            Assert.AreEqual("PumpRoomRoomLayout", RoomAuthoring.LayoutAssetName("pump room"));
            Assert.AreEqual("HighRollerSalonRoomLayout", RoomAuthoring.LayoutAssetName("High-Roller Salon"));
            Assert.AreEqual("Assets/Project/Data/Level/Functions/Layouts/EngineRoom", RoomAuthoring.FolderFor(Place("Engine Room")));
            foreach (RoomLayout layout in Place("Fuel Depot").Layouts)
                StringAssert.StartsWith(RoomAuthoring.FolderFor(Place("Fuel Depot")) + "/", AssetDatabase.GetAssetPath(layout));
        }
    }
}
