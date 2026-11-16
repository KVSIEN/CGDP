using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The RoomGallery scene's graph: every place once, every faction and section in turn, the
    // showcase fights for each faction, one walk from Start to Exit, and a layout that fits.
    public class RoomGalleryTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";
        private const string GalleryPath  = "Assets/Project/Data/Level/Gallery/RoomGallerySettings.asset";

        private static LevelBuildSettings Settings => AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
        private static RoomGallerySettings Gallery => AssetDatabase.LoadAssetAtPath<RoomGallerySettings>(GalleryPath);

        [Test]
        public void EveryPlaceGetsOneRoom()
        {
            MapGraph graph = RoomGalleryGraph.Build(Gallery, Settings);
            var placed = new HashSet<IRoomPlace>();
            foreach (MapNode node in graph.Nodes)
                if (node.Place != null) Assert.IsTrue(placed.Add(node.Place), $"{node.Place} twice");

            foreach (RoomFunction place in Settings.Functions)
                if (place != null) Assert.IsTrue(placed.Contains(place), $"{place.DisplayName} missing");
        }

        [Test]
        public void EveryFactionAndSectionShowsUp()
        {
            MapGraph graph = RoomGalleryGraph.Build(Gallery, Settings);
            var factions = new HashSet<int>();
            var sections = new HashSet<int>();
            foreach (MapNode node in graph.Nodes)
            {
                if (node.Place == null) continue;
                factions.Add(node.Faction);
                sections.Add(node.Section);
            }
            Assert.AreEqual(Gallery.Content.Factions.Count, factions.Count);
            Assert.AreEqual(Gallery.Content.Sections.Count, sections.Count);
        }

        [Test]
        public void EachShowcaseFightIsThereForEachFaction()
        {
            MapGraph graph = RoomGalleryGraph.Build(Gallery, Settings);
            int factions = Gallery.Content.Factions.Count;
            foreach (MapNodeType type in Gallery.ShowcaseTypes)
            {
                var held = new HashSet<int>();
                foreach (MapNode node in graph.Nodes)
                {
                    if (node.Type != type || node.Place != null) continue;
                    held.Add(node.Faction);
                    if (type.HasSecondFaction()) Assert.AreNotEqual(node.Faction, node.BreachFaction, $"{type} breached by its own faction");
                }
                Assert.AreEqual(factions, held.Count, $"{type}");
            }
        }

        [Test]
        public void TheRoomsMakeOneWalkFromStartToExit()
        {
            MapGraph graph = RoomGalleryGraph.Build(Gallery, Settings);
            Assert.AreEqual(MapNodeType.Start, graph.Nodes[0].Type);
            Assert.AreEqual(MapNodeType.Exit, graph.Nodes[graph.Nodes.Count - 1].Type);
            Assert.AreEqual(graph.Nodes.Count - 1, graph.Connections.Count);

            var spots = new HashSet<Vector2>();
            foreach (MapNode node in graph.Nodes) Assert.IsTrue(spots.Add(node.Position), $"two rooms at {node.Position}");
        }

        [Test]
        public void TheGalleryLaysOutEveryRoom()
        {
            MapGraph graph = RoomGalleryGraph.Build(Gallery, Settings);
            LevelLayout layout = new LevelLayoutBuilder(Settings).Build(graph, Vector2.one, new Seed(1UL), Gallery.Content);

            Assert.AreEqual(graph.Nodes.Count, layout.Rooms.Count, string.Join("\n", layout.Warnings));
            foreach (LevelRoom room in layout.Rooms.Values)
                Assert.IsNotNull(room.Structure, $"{room.DisplayName} has no structure");
        }
    }
}
