using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // A node can pin the place its room becomes; the validator flags pins that can't hold.
    public class MapPlacePinTests
    {
        private static RoomFunction CreatePlace(RoomCategory category, bool unique)
        {
            var place = ScriptableObject.CreateInstance<RoomFunction>();
            var serialized = new SerializedObject(place);
            serialized.FindProperty("_category").intValue = (int)category;
            serialized.FindProperty("_unique").boolValue = unique;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return place;
        }

        private static List<string> Validate(MapGraph graph) =>
            MapGraphValidator.Validate(graph, new MapGraphAnalysis(graph), null, null);

        [Test]
        public void PinnedPlaceIsKeptOnTheNodeAndItsClone()
        {
            RoomFunction bridge = CreatePlace(RoomCategory.Command, unique: true);
            MapNode node = new MapGraph().AddNode(MapNodeType.Combat, Vector2.zero);

            node.Place = bridge;

            Assert.AreSame(bridge, node.Place);
            Assert.AreSame(bridge, node.Clone().Place);
            Object.DestroyImmediate(bridge);
        }

        [Test]
        public void OneOfAKindPlacePinnedTwiceIsFlagged()
        {
            RoomFunction bridge = CreatePlace(RoomCategory.Command, unique: true);
            var graph = new MapGraph();
            graph.AddNode(MapNodeType.Combat, Vector2.zero).Place = bridge;
            MapNode second = graph.AddNode(MapNodeType.Shop, Vector2.right);
            second.Place = bridge;

            Assert.IsTrue(Validate(graph).Exists(i => i.StartsWith($"#{second.Id}:") && i.Contains("one of a kind")));
            Object.DestroyImmediate(bridge);
        }

        [Test]
        public void PlaceOfAnotherCategoryIsFlagged()
        {
            RoomFunction casino = CreatePlace(RoomCategory.Entertainment, unique: false);
            var graph = new MapGraph();
            MapNode node = graph.AddNode(MapNodeType.Combat, Vector2.zero);
            node.Category = RoomCategory.Residential;
            node.Place = casino;

            Assert.IsTrue(Validate(graph).Exists(i => i.StartsWith($"#{node.Id}: pinned place")));
            Object.DestroyImmediate(casino);
        }
    }
}
