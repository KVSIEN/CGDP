using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // A hand-built room can face any way: the turn maths agrees with itself and with the
    // prefab's turn, the placer turns a landmark so its doorway faces the room it connects
    // to, and a landmark that can't turn never does.
    public class LandmarkTurnTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";

        [Test]
        public void OddTurnsSwapTheSizeAndTheCountWraps()
        {
            var size = new Vector2Int(12, 8);
            Assert.AreEqual(size, LandmarkTurn.Size(size, 0));
            Assert.AreEqual(new Vector2Int(8, 12), LandmarkTurn.Size(size, 1));
            Assert.AreEqual(size, LandmarkTurn.Size(size, 2));
            Assert.AreEqual(new Vector2Int(8, 12), LandmarkTurn.Size(size, -1));
            Assert.AreEqual(LandmarkTurn.Size(size, 1), LandmarkTurn.Size(size, 5));
        }

        [Test]
        public void TurnsGoClockwiseAndFourBringTheRoomBack()
        {
            Assert.AreEqual(RoomSide.East,  LandmarkTurn.Side(RoomSide.North, 1));
            Assert.AreEqual(RoomSide.South, LandmarkTurn.Side(RoomSide.East, 1));
            Assert.AreEqual(RoomSide.West,  LandmarkTurn.Side(RoomSide.North, 3));
            foreach (RoomSide side in new[] { RoomSide.North, RoomSide.East, RoomSide.South, RoomSide.West })
                Assert.AreEqual(side, LandmarkTurn.Side(side, 4));

            var size = new Vector2Int(5, 3);
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    Assert.AreEqual(new Vector2Int(x, y), LandmarkTurn.Tile(new Vector2Int(x, y), size, 4));
        }

        [Test]
        public void TurnedTilesStayInsideTheTurnedRoomOnePerTile()
        {
            var size = new Vector2Int(5, 3);
            for (int turns = 0; turns < 4; turns++)
            {
                Vector2Int turned = LandmarkTurn.Size(size, turns);
                var seen = new System.Collections.Generic.HashSet<Vector2Int>();
                for (int x = 0; x < size.x; x++)
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector2Int tile = LandmarkTurn.Tile(new Vector2Int(x, y), size, turns);
                        Assert.IsTrue(tile.x >= 0 && tile.y >= 0 && tile.x < turned.x && tile.y < turned.y, $"turns {turns}: {tile} outside {turned}");
                        Assert.IsTrue(seen.Add(tile), $"turns {turns}: two tiles land on {tile}");
                    }
            }
        }

        // The prefab is turned by Yaw and moved by PrefabOffset; a tile's centre must land on the
        // tile the tile maths gives it, or furniture and floor would disagree.
        [Test]
        public void ThePrefabTurnsTheSameWayAsTheTiles()
        {
            var size = new Vector2Int(5, 3);
            for (int turns = 0; turns < 4; turns++)
            {
                Quaternion yaw = Quaternion.Euler(0f, LandmarkTurn.Yaw(turns), 0f);
                Vector2 offset = LandmarkTurn.PrefabOffset(size, turns);
                for (int x = 0; x < size.x; x++)
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector3 centre = yaw * new Vector3(x + 0.5f, 0f, y + 0.5f) + new Vector3(offset.x, 0f, offset.y);
                        var landed = new Vector2Int(Mathf.FloorToInt(centre.x), Mathf.FloorToInt(centre.z));
                        Assert.AreEqual(LandmarkTurn.Tile(new Vector2Int(x, y), size, turns), landed, $"turns {turns}, tile ({x}, {y})");
                    }
            }
        }

        [Test]
        public void BigCurvedRoomsGetOneMorePassUpToTheMaximum()
        {
            Assert.AreEqual(2, RoomOutline.SmoothingFor(new RectInt(0, 0, 12, 12), 2));
            Assert.AreEqual(3, RoomOutline.SmoothingFor(new RectInt(0, 0, 30, 30), 2));
            Assert.AreEqual(3, RoomOutline.SmoothingFor(new RectInt(0, 0, RoomOutline.LargeRoomTiles, 8), 2), "a long room counts by its longer side");
            Assert.AreEqual(RoomOutline.MaxSmoothing, RoomOutline.SmoothingFor(new RectInt(0, 0, 30, 30), RoomOutline.MaxSmoothing));
            Assert.AreEqual(0, RoomOutline.SmoothingFor(new RectInt(0, 0, 30, 30), 0), "smoothing off stays off");
        }

        // --- Placement -------------------------------------------------------------------

        private static void Set(object target, string name, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            Assert.Fail($"no field {name}");
        }

        // A 12-tile room with one doorway, on its west wall; room A of a two-room map with B to its east.
        private static LevelRoom PlaceWestDoorLandmark(bool canTurn, int seed, out LandmarkRoomDefinition landmark, out RoomFunction function)
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
            Assert.IsNotNull(settings, $"Missing {SettingsPath}");

            landmark = ScriptableObject.CreateInstance<LandmarkRoomDefinition>();
            Set(landmark, "_sizeTiles", new Vector2Int(12, 12));
            Set(landmark, "_canTurn", canTurn);
            Set(landmark, "_doorways", new System.Collections.Generic.List<LandmarkDoorway> { new() { Tile = new Vector2Int(0, 6), Side = RoomSide.West } });
            function = ScriptableObject.CreateInstance<RoomFunction>();
            Set(function, "_landmark", landmark);

            var graph = new MapGraph();
            MapNode a = graph.AddNode(MapNodeType.Combat, Vector2.zero);
            MapNode b = graph.AddNode(MapNodeType.Combat, new Vector2(100f, 0f));
            graph.Connect(a.Id, b.Id);
            a.Place = function;

            LevelLayout layout = new LevelLayoutBuilder(settings).PlaceRooms(graph, new Vector2(100f, 100f), new Seed((ulong)seed));
            return layout.Rooms[a.Id];
        }

        [Test]
        public void ALandmarkIsTurnedSoItsDoorwayFacesTheRoomItConnectsTo()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                LevelRoom room = PlaceWestDoorLandmark(true, seed, out LandmarkRoomDefinition landmark, out RoomFunction function);
                try
                {
                    Assert.AreSame(landmark, room.Landmark, $"seed {seed}: generated instead");
                    Assert.AreEqual(2, room.LandmarkTurns, $"seed {seed}: a west doorway needs a half turn to face east");
                    Assert.AreEqual(Vector2Int.right, room.Footprint.Sockets[0].Outward);
                }
                finally
                {
                    Object.DestroyImmediate(function);
                    Object.DestroyImmediate(landmark);
                }
            }
        }

        [Test]
        public void ALandmarkThatCannotTurnStaysAsBuilt()
        {
            LevelRoom room = PlaceWestDoorLandmark(false, 1, out LandmarkRoomDefinition landmark, out RoomFunction function);
            try
            {
                Assert.AreSame(landmark, room.Landmark);
                Assert.AreEqual(0, room.LandmarkTurns);
                Assert.AreEqual(Vector2Int.left, room.Footprint.Sockets[0].Outward);
            }
            finally
            {
                Object.DestroyImmediate(function);
                Object.DestroyImmediate(landmark);
            }
        }
    }
}
