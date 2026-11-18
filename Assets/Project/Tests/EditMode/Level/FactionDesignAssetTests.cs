using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Factions;
using CGD.Level;

namespace CGD.Tests
{
    // Checks the shipped faction data: every faction is built by a complete design, and every
    // prop a dressing places is a real prefab of the right kind. Guards against broken references
    // and a decoration that would block the way (or furniture you could walk through).
    public class FactionDesignAssetTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";

        private static LevelBuildSettings Settings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
            Assert.IsNotNull(settings, $"Missing {SettingsPath}");
            return settings;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"Missing {path}");
            return asset;
        }

        [TestCase("Tech")]
        [TestCase("Bio")]
        [TestCase("Void")]
        public void EveryFactionHasACompleteDesign(string name)
        {
            var faction = Load<FactionDefinition>($"Assets/Project/Data/Factions/{name}Faction.asset");
            FactionDesign design = Settings().DesignFor(faction);

            Assert.IsNotNull(design, $"{name} has no design in the level build settings");
            Assert.Greater(design.Structure.Count, 0, $"{name} builds no structure");
            foreach (RoomStructureRule rule in design.Structure) Assert.IsNotNull(rule, $"{name} lists a missing structure rule");
            Assert.IsNotNull(design.Dressing, $"{name} has no dressing of its own");

            var cover = Load<RoomDressing>("Assets/Project/Data/Level/Dressing/CombatCoverRoomDressing.asset");
            var loot = Load<RoomDressing>("Assets/Project/Data/Level/Dressing/LootRoomDressing.asset");
            Assert.AreNotSame(cover, design.Dress(cover), $"{name} has no version of the combat cover");
            Assert.AreNotSame(loot, design.Dress(loot), $"{name} has no version of the loot dressing");
        }

        [Test]
        public void EveryPlacedPropIsARealPrefabOfTheRightKind()
        {
            var checkedPrefabs = new HashSet<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:RoomDressing"))
            {
                var dressing = AssetDatabase.LoadAssetAtPath<RoomDressing>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (RoomPropPlacement placement in dressing.Props) AssertRightKind(placement, dressing.name, checkedPrefabs);
            }
            Assert.Greater(checkedPrefabs.Count, 10);
        }

        // Every place is furnished (ROOM_GENERATION.md, Furnishing rules), with real prefabs, and
        // its anchor — when it has one — goes down before its furniture.
        [Test]
        public void EveryPlaceIsFurnishedAnchorFirst()
        {
            var checkedPrefabs = new HashSet<GameObject>();
            foreach (RoomFunction function in Settings().Functions)
            {
                // A building or town is furnished by its rooms' places, not by props of its own.
                if (function.Landmark != null || function.IsComposite || function.name == "JunctionRoomFunction") continue;
                Assert.Greater(function.Props.Count, 0, $"{function.name} has no furniture");
                for (int i = 0; i < function.Props.Count; i++)
                {
                    AssertRightKind(function.Props[i], function.name, checkedPrefabs);
                    if (i > 0 && function.Props[i].Required && !function.Props[i - 1].Required)
                        Assert.Fail($"{function.name}: a required piece comes after optional furniture");
                }
            }
        }

        private static void AssertRightKind(RoomPropPlacement placement, string owner, HashSet<GameObject> checkedPrefabs)
        {
            Assert.Greater(placement.Prefabs.Length, 0, $"{owner} has a placement with no prefab");
            foreach (GameObject prefab in placement.Prefabs)
            {
                Assert.IsNotNull(prefab, $"{owner} places a missing prefab");
                if (!checkedPrefabs.Add(prefab)) continue;

                bool solid = prefab.GetComponentInChildren<Collider>() != null;
                if (placement.Decor) Assert.IsFalse(solid, $"{prefab.name} is ground cover but has a collider");
                else Assert.IsTrue(solid, $"{prefab.name} is furniture but has no collider");
            }
        }

        // Each faction leans on its own trait: BIO hides, VOID breaks sight with walls, TECH
        // holds lines of fire — and each is low on another's strength.
        [TestCase("Bio",  TraitPriority.High,   TraitPriority.Medium, TraitPriority.Low)]
        [TestCase("Void", TraitPriority.Medium, TraitPriority.High,   TraitPriority.Low)]
        [TestCase("Tech", TraitPriority.Low,    TraitPriority.Medium, TraitPriority.High)]
        public void EachFactionHasItsPriorities(string faction, TraitPriority concealment, TraitPriority walls, TraitPriority linesOfFire)
        {
            var design = Load<FactionDesign>($"Assets/Project/Data/Level/Designs/{faction}FactionDesign.asset");
            Assert.AreEqual(concealment, design.Priorities.Of(RoomTrait.Concealment));
            Assert.AreEqual(walls,       design.Priorities.Of(RoomTrait.Walls));
            Assert.AreEqual(linesOfFire, design.Priorities.Of(RoomTrait.LinesOfFire));
        }

        // What still marks a room with its own layout as this faction's: BIO's wall stubs,
        // VOID's folds. TECH adds nothing, to keep its lanes open.
        [TestCase("Bio", 1)]
        [TestCase("Void", 1)]
        [TestCase("Tech", 0)]
        public void EachFactionAddsItsOwnStructure(string faction, int additions)
        {
            var design = Load<FactionDesign>($"Assets/Project/Data/Level/Designs/{faction}FactionDesign.asset");
            Assert.AreEqual(additions, design.Additions.Count);
            foreach (RoomStructureRule rule in design.Additions) Assert.IsNotNull(rule, $"{faction} adds a missing structure rule");
        }

        // Every place has its layout pattern's structure (ROOM_GENERATION.md, section 2), except
        // those left to the holding faction and the hand-built landmarks.
        [Test]
        public void EveryPlaceBuildsItsPattern()
        {
            var leftToTheFaction = new HashSet<string> { "Park", "Farm", "Junction" };
            foreach (RoomFunction function in Settings().Functions)
            {
                Assert.IsNotNull(function, "the level build settings list a missing room function");
                foreach (RoomStructureRule rule in function.Structure) Assert.IsNotNull(rule, $"{function.name} lists a missing structure rule");
                if (function.Landmark != null || leftToTheFaction.Contains(function.name.Replace("RoomFunction", ""))) continue;
                Assert.Greater(function.Structure.Count, 0, $"{function.name} has no structure of its own");
            }
        }
    }
}
