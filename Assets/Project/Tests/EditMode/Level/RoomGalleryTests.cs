using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The RoomGallery scene's graph: every place once per ship side it can sit on and per layout
    // it has, every faction
    // and section in turn, each faction's wing from frontier to core with its showcase fights,
    // one walk from Start to Exit, and a layout that fits.
    public class RoomGalleryTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";
        private const string GalleryPath  = "Assets/Project/Data/Level/Gallery/RoomGallerySettings.asset";

        private static LevelBuildSettings Settings => AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
        private static RoomGallerySettings Gallery => AssetDatabase.LoadAssetAtPath<RoomGallerySettings>(GalleryPath);

        [Test]
        public void EveryPlaceGetsARoomOnEachOfItsSides()
        {
            Dictionary<IRoomPlace, List<MapNode>> rooms = RoomsByPlace(RoomGalleryGraph.Build(Gallery, Settings));

            foreach (RoomFunction place in Settings.Functions)
            {
                if (place == null) continue;
                Assert.IsTrue(rooms.TryGetValue(place, out List<MapNode> shown), $"{place.DisplayName} missing");
                int expected = Mathf.Max(place.Sides.Count < 2 ? 1 : place.Sides.Count, Layouts(place).Count);
                Assert.AreEqual(expected, shown.Count, $"{place.DisplayName}: one room per side or per layout, whichever is more");

                var sides = new HashSet<ShipSide>();
                foreach (MapNode node in shown) sides.Add(place.SideIn(Gallery.Content.GetSection(node.Section)));
                if (place.Sides.Count >= 2) CollectionAssert.IsSubsetOf(place.Sides, sides, $"{place.DisplayName}'s sides");
            }
        }

        [Test]
        public void EveryLayoutOfAPlaceGetsARoom()
        {
            Dictionary<IRoomPlace, List<MapNode>> rooms = RoomsByPlace(RoomGalleryGraph.Build(Gallery, Settings));

            foreach (RoomFunction place in Settings.Functions)
            {
                if (place == null || Layouts(place).Count == 0) continue;
                var shown = new List<RoomLayout>();
                foreach (MapNode node in rooms[place]) shown.Add(node.Layout as RoomLayout);
                CollectionAssert.IsSubsetOf(Layouts(place), shown, $"{place.DisplayName}'s layouts");
            }
        }

        private static Dictionary<IRoomPlace, List<MapNode>> RoomsByPlace(MapGraph graph)
        {
            var rooms = new Dictionary<IRoomPlace, List<MapNode>>();
            foreach (MapNode node in graph.Nodes)
            {
                if (node.Place == null) continue;
                if (!rooms.TryGetValue(node.Place, out List<MapNode> nodes)) rooms[node.Place] = nodes = new List<MapNode>();
                nodes.Add(node);
            }
            return rooms;
        }

        private static List<RoomLayout> Layouts(RoomFunction place)
        {
            var layouts = new List<RoomLayout>();
            foreach (RoomLayout layout in place.Layouts)
                if (layout != null) layouts.Add(layout);
            return layouts;
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
                    if (node.Type != type || node.Place != null || node.FactionInfluence < 1f) continue;
                    held.Add(node.Faction);
                    if (type.HasSecondFaction()) Assert.AreNotEqual(node.Faction, node.BreachFaction, $"{type} breached by its own faction");
                }
                Assert.AreEqual(factions, held.Count, $"{type}");
            }
        }

        [Test]
        public void EachWingWalksFromTheFrontierToTheCore()
        {
            MapGraph graph = RoomGalleryGraph.Build(Gallery, Settings);
            TerritoryDepth depth = Settings.TerritoryDepth;
            for (int faction = 0; faction < Gallery.Content.Factions.Count; faction++)
            {
                var strengths = new List<float>();
                foreach (MapNode node in graph.Nodes)
                    if (node.Place == null && node.Faction == faction && node.Type == MapNodeType.Combat)
                        strengths.Add(depth.StrengthOf(node.FactionInfluence));

                Assert.AreEqual(Gallery.DepthSteps.Count + 1, strengths.Count, $"faction {faction}'s Combat rooms");
                Assert.AreEqual("frontier", depth.NameOf(strengths[0]), $"faction {faction}'s wing opens on its frontier");
                Assert.AreEqual("core", depth.NameOf(strengths[strengths.Count - 1]), $"faction {faction}'s wing ends in its core");
                for (int i = 1; i < strengths.Count; i++)
                    Assert.Greater(strengths[i], strengths[i - 1], $"faction {faction}'s wing thickens toward its core");
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
